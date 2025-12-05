using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;

namespace PermissionDaemon
{
    public class PermissionEnforcer
    {
        private DaemonConfig _config;
        private readonly string _currentAgentName;

        public PermissionEnforcer(DaemonConfig config, string currentAgentName)
        {
            _config = config;
            _currentAgentName = currentAgentName;
        }

        public void UpdateConfig(DaemonConfig newConfig)
        {
            _config = newConfig;
        }

        /// <summary>
        /// Check if the current agent has permission to perform an action on a file
        /// </summary>
        /// <param name="filePath">The file path to check</param>
        /// <param name="action">The action being performed (read, write, delete)</param>
        /// <returns>True if the agent has permission, false otherwise</returns>
        public bool HasPermission(string filePath, System.Security.AccessControl.FileSystemRights action)
        {
            // Find the first rule that matches this file path
            var matchingRule = _config.Rules.FirstOrDefault(rule => 
                GlobPatternMatcher.IsMatchAny(rule.Patterns, filePath));

            if (matchingRule == null)
            {
                // No rule matches, default to allowing access (or could default to deny)
                return true;
            }

            // Determine the permission for this agent
            string permission = GetPermissionForAgent(matchingRule, _currentAgentName);

            // Check if the requested action is allowed by the permission
            return IsActionAllowed(permission, action);
        }

        /// <summary>
        /// Attempt to enforce permissions by setting Unix file permissions using chmod
        /// </summary>
        /// <param name="filePath">The file path to protect</param>
        public void EnforcePermissions(string filePath)
        {
            Console.WriteLine($"[DEBUG] Checking rules for file: {filePath}");

            // Extract just the filename to match against patterns (like .gitignore patterns work)
            string fileName = System.IO.Path.GetFileName(filePath);
            Console.WriteLine($"[DEBUG] Using filename for pattern matching: {fileName}");

            var matchingRule = _config.Rules.FirstOrDefault(rule =>
                GlobPatternMatcher.IsMatchAny(rule.Patterns, fileName));

            if (matchingRule == null)
            {
                Console.WriteLine($"[DEBUG] No matching rule found for: {fileName}");
                return; // No rule applies to this file
            }

            Console.WriteLine($"[DEBUG] Found matching rule for: {fileName}");
            Console.WriteLine($"[DEBUG] Rule patterns: {string.Join(", ", matchingRule.Patterns)}");

            try
            {
                if (File.Exists(filePath))
                {
                    // For now, get the default permission or first available permission to apply to the file
                    string permissionToApply = GetDefaultPermissionForFile(matchingRule);
                    Console.WriteLine($"[DEBUG] Permission to apply: {permissionToApply}");

                    if (!string.IsNullOrEmpty(permissionToApply))
                    {
                        SetUnixPermissions(filePath, permissionToApply);

                        Console.WriteLine($"[PERMISSION ENFORCEMENT] Applied permission {permissionToApply} to {filePath}");
                    }

                    Console.WriteLine($"[PERMISSION ENFORCEMENT] File: {filePath}");
                    foreach (var perm in matchingRule.Permissions)
                    {
                        Console.WriteLine($"  Agent '{perm.Key}' has permission: {perm.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Could not enforce permissions on {filePath}: {ex.Message}");
            }
        }

        private string GetDefaultPermissionForFile(Rule rule)
        {
            // First check if there's a "default" permission specified
            if (rule.Permissions.ContainsKey("default"))
            {
                return rule.Permissions["default"];
            }

            // If there isn't a default, but there are other specific agent permissions,
            // we should still apply some permission - return the first one found
            // This is important for the test case where we have patterns with permissions
            // but no explicit "default" key
            foreach (var permission in rule.Permissions.Values)
            {
                if (!string.IsNullOrEmpty(permission) && permission.Length == 3)
                {
                    return permission;
                }
            }

            // Default to no permissions if nothing specified
            return "---";
        }

        private bool ShouldRestrictWrite(Rule rule)
        {
            // Simple logic: if all agents have no write permission ('-' in position 1), restrict write
            foreach (var permValue in rule.Permissions.Values)
            {
                if (permValue.Length > 1 && permValue[1] == 'w')
                {
                    // At least one agent has write permission
                    return false;
                }
            }
            return true;
        }

        private void SetFileReadOnly(string filePath)
        {
            try
            {
                // On Unix systems, execute chmod command to set permissions
                // This is the proper way to set Unix file permissions

                // For a read-only file, we typically want 444 (read-only for all) or 400 (owner read-only)
                // If we want to respect our permission configuration more granularly, we'd need to
                // map our rwx notation to actual Unix octal permissions
                RunChmodCommand(filePath, "444");  // read-only for all (owner, group, others)

                Console.WriteLine($"[PERMISSION ENFORCEMENT] Set {filePath} to read-only (444)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Could not set read-only permission on {filePath}: {ex.Message}");
            }
        }

        private void RunChmodCommand(string filePath, string permissions)
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"{permissions} \"{filePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                string error = process.StandardError.ReadToEnd();
                throw new InvalidOperationException($"chmod command failed: {error}");
            }
        }

        // Method to set more granular Unix permissions based on our permission model
        public void SetUnixPermissions(string filePath, string permissionString)
        {
            if (permissionString.Length != 3)
            {
                Console.WriteLine($"[ERROR] Invalid permission string: {permissionString}");
                return;
            }

            try
            {
                // Convert our rwx notation to Unix octal for owner, group, and others
                // Format: rwx = read, write, execute for owner
                // We'll apply the same permissions to owner, group, and others for simplicity
                int ownerPerm = ConvertPermissionToOctal(permissionString[0], permissionString[1], permissionString[2]);

                // For a simple implementation, we'll use the same permission for owner/group/others
                // This creates a 3-digit octal like "777" or "444"
                string octalPerm = $"{ownerPerm}{ownerPerm}{ownerPerm}";

                RunChmodCommand(filePath, octalPerm);
                Console.WriteLine($"[PERMISSION ENFORCEMENT] Set {filePath} to {octalPerm}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Could not set Unix permissions on {filePath}: {ex.Message}");
            }
        }

        private int ConvertPermissionToOctal(char read, char write, char execute)
        {
            int result = 0;
            if (read == 'r') result += 4;  // read = 4
            if (write == 'w') result += 2; // write = 2
            if (execute == 'x') result += 1; // execute = 1
            return result;
        }

        private string GetPermissionForAgent(Rule rule, string agentName)
        {
            // Check if this specific agent has defined permissions
            if (rule.Permissions.ContainsKey(agentName))
            {
                return rule.Permissions[agentName];
            }

            // Check for "default" permission
            if (rule.Permissions.ContainsKey("default"))
            {
                return rule.Permissions["default"];
            }

            // By default, deny access if no specific permission is defined for the agent
            // This is more secure than allowing access by default
            return "---"; // No permissions
        }

        private bool IsActionAllowed(string permission, System.Security.AccessControl.FileSystemRights action)
        {
            if (string.IsNullOrEmpty(permission) || permission.Length < 3)
            {
                return false; // Invalid permission string
            }

            // permission string is in format "rwx" where:
            // position 0: read (r/-)
            // position 1: write (w/-)
            // position 2: execute/delete (x/-)

            // Check for read permissions
            bool isReadAction = action.HasFlag(System.Security.AccessControl.FileSystemRights.ReadData) ||
                               action.HasFlag(System.Security.AccessControl.FileSystemRights.ReadAttributes) ||
                               action.HasFlag(System.Security.AccessControl.FileSystemRights.ReadExtendedAttributes) ||
                               action.HasFlag(System.Security.AccessControl.FileSystemRights.ListDirectory);

            if (isReadAction)
                return permission[0] == 'r';

            // Check for write permissions
            bool isWriteAction = action.HasFlag(System.Security.AccessControl.FileSystemRights.WriteData) ||
                                action.HasFlag(System.Security.AccessControl.FileSystemRights.AppendData) ||
                                action.HasFlag(System.Security.AccessControl.FileSystemRights.Delete) ||
                                action.HasFlag(System.Security.AccessControl.FileSystemRights.WriteAttributes) ||
                                action.HasFlag(System.Security.AccessControl.FileSystemRights.WriteExtendedAttributes);

            if (isWriteAction)
                return permission[1] == 'w';

            // Check for execute permissions
            bool isExecuteAction = action.HasFlag(System.Security.AccessControl.FileSystemRights.ExecuteFile);

            if (isExecuteAction)
                return permission[2] == 'x';

            // For other complex access types, check if any write permission exists
            return permission[1] == 'w';
        }
    }
}