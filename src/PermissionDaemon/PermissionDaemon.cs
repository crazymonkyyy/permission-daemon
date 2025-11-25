using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace PermissionDaemon
{
    // Configuration class for the daemon
    public class DaemonConfig
    {
        public List<Rule> Rules { get; set; } = new();
    }

    public class Rule
    {
        public string Pattern { get; set; } = "";
        public string User { get; set; } = "";
        public List<string> AllowedOperations { get; set; } = new();
    }

    public class PermissionDaemon
    {
        private FileSystemWatcher? _configWatcher;
        private FileSystemWatcher? _folderWatcher;
        private DaemonConfig _config = new();
        private readonly object _lock = new();
        private readonly string _configPath;
        private readonly string _rootDirectory;

        public PermissionDaemon()
        {
            _rootDirectory = Environment.CurrentDirectory;
            _configPath = Path.Combine(_rootDirectory, "permissions.config");
        }

        public void Start()
        {
            Console.WriteLine("Permission Daemon starting...");
            Console.WriteLine($"Monitoring directory: {_rootDirectory}");
            
            LoadConfig();
            SetupWatchers();
            
            Console.WriteLine("Permission Daemon running. Press 'q' to quit.");
            
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                {
                    Console.WriteLine("\nShutting down daemon...");
                    break;
                }
                
                System.Threading.Thread.Sleep(100);
            }
            
            Cleanup();
        }

        private void SetupWatchers()
        {
            // Watch for config file changes
            var configDir = Path.GetDirectoryName(_configPath) ?? _rootDirectory;
            var configFileName = Path.GetFileName(_configPath);
            
            _configWatcher = new FileSystemWatcher(configDir)
            {
                Filter = configFileName,
                EnableRaisingEvents = true
            };
            _configWatcher.Changed += (s, e) => HandleConfigChange();
            _configWatcher.Created += (s, e) => HandleConfigChange();
            _configWatcher.Deleted += (s, e) => HandleConfigChange();

            // Watch the current directory for all file operations
            _folderWatcher = new FileSystemWatcher(_rootDirectory)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            _folderWatcher.Created += (s, e) => HandleFileOperation(e.FullPath, "create");
            _folderWatcher.Changed += (s, e) => HandleFileOperation(e.FullPath, "change");
            _folderWatcher.Deleted += (s, e) => HandleFileOperation(e.FullPath, "delete");
            _folderWatcher.Renamed += (s, e) => HandleFileRename(e);
            
            Console.WriteLine($"Watching config file: {_configPath}");
            Console.WriteLine($"Watching directory: {_rootDirectory}");
        }

        private void HandleConfigChange()
        {
            try
            {
                // Add a small delay to ensure the file is fully written
                System.Threading.Thread.Sleep(200);
                LoadConfig();
                Console.WriteLine("Configuration reloaded.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading config: {ex.Message}");
            }
        }

        private void HandleFileOperation(string fullPath, string operation)
        {
            try
            {
                lock (_lock)
                {
                    CheckAndHandleOperation(fullPath, operation);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in file operation handling: {ex.Message}");
            }
        }

        private void HandleFileRename(RenamedEventArgs e)
        {
            try
            {
                lock (_lock)
                {
                    // Check both old and new paths
                    CheckAndHandleOperation(e.OldFullPath, "rename_from");
                    CheckAndHandleOperation(e.FullPath, "rename_to");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in file rename handling: {ex.Message}");
            }
        }

        private void CheckAndHandleOperation(string fullPath, string operation)
        {
            var relativePath = Path.GetRelativePath(_rootDirectory, fullPath);
            
            // Log the operation
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Operation: {operation.ToUpper()} - {relativePath}");
            
            // Skip if the file itself is the config file
            if (fullPath == _configPath)
                return;

            // Check each rule against the file path
            foreach (var rule in _config.Rules)
            {
                try
                {
                    var matcher = new Matcher();
                    matcher.AddInclude(rule.Pattern);
                    
                    var directoryInfo = new DirectoryInfo(_rootDirectory);
                    var directoryInfoWrapper = new DirectoryInfoWrapper(directoryInfo);
                    
                    var result = matcher.Execute(directoryInfoWrapper);
                    
                    // Check if the file path matches the pattern
                    bool isMatch = false;
                    foreach (var file in result.Files)
                    {
                        var matchedPath = Path.Combine(_rootDirectory, file.Path);
                        if (matchedPath == fullPath)
                        {
                            isMatch = true;
                            break;
                        }
                    }
                    
                    if (isMatch)
                    {
                        // Check if the current user is allowed to perform this operation
                        var currentUser = GetCurrentUser();
                        
                        if (!IsOperationAllowed(currentUser, rule, operation))
                        {
                            // Access denied - log and potentially revert
                            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ACCESS DENIED: User '{currentUser}' not allowed to {operation} '{relativePath}' (matches pattern '{rule.Pattern}')");
                            
                            LogAccessViolation(currentUser, relativePath, rule.Pattern, operation);
                            
                            // Try to revert the operation if it was a delete
                            if (operation == "delete" && File.Exists(fullPath + ".backup"))
                            {
                                try
                                {
                                    File.Move(fullPath + ".backup", fullPath);
                                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Restored file: {relativePath}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Failed to restore file: {ex.Message}");
                                }
                            }
                            
                            return; // Stop checking other rules once we find a match
                        }
                        else
                        {
                            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Operation allowed for user '{currentUser}': {operation} - {relativePath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error checking rule for pattern '{rule.Pattern}': {ex.Message}");
                }
            }
        }

        private bool IsOperationAllowed(string currentUser, Rule rule, string operation)
        {
            // Check if the user matches the rule's allowed user
            if (!string.Equals(currentUser, rule.User, StringComparison.OrdinalIgnoreCase))
                return false;
                
            // If allowed operations is empty, allow all operations
            if (rule.AllowedOperations.Count == 0)
                return true;
                
            // Check if the operation is in the allowed list
            return rule.AllowedOperations.Contains(operation, StringComparer.OrdinalIgnoreCase);
        }

        private string GetCurrentUser()
        {
            // In a real implementation, you might want to get the effective user more reliably
            // For this implementation, we'll use the environment username
            return Environment.UserName;
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    var jsonContent = File.ReadAllText(_configPath);
                    var newConfig = JsonSerializer.Deserialize<DaemonConfig>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    });

                    if (newConfig != null)
                    {
                        _config = newConfig;
                    }
                    else
                    {
                        CreateDefaultConfig();
                    }
                }
                else
                {
                    Console.WriteLine("Config file not found. Creating default config...");
                    CreateDefaultConfig();
                    File.WriteAllText(_configPath, JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}. Using default config.");
                CreateDefaultConfig();
            }
        }

        private void CreateDefaultConfig()
        {
            _config = new DaemonConfig();
            
            // Example rule: prevent non-testers from deleting test files
            _config.Rules.Add(new Rule 
            { 
                Pattern = "**/*test*.cs", 
                User = "tester",
                AllowedOperations = new List<string> { "read", "create", "change", "delete" }
            });
            
            // Example rule: prevent non-editors from modifying .cs files
            _config.Rules.Add(new Rule 
            { 
                Pattern = "**/*.cs", 
                User = "editor",
                AllowedOperations = new List<string> { "read", "create", "change" }
            });
            
            // Example rule: allow admins to do anything
            _config.Rules.Add(new Rule 
            { 
                Pattern = "**/*", 
                User = "admin",
                AllowedOperations = new List<string> { "read", "create", "change", "delete" }
            });
        }

        private void LogAccessViolation(string user, string filePath, string pattern, string operation)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] VIOLATION: User '{user}' attempted {operation} on '{filePath}' (matched pattern '{pattern}')\n";
            
            Console.Write(logEntry);
            
            // Write to a log file
            var logPath = Path.Combine(_rootDirectory, "access_violations.log");
            File.AppendAllText(logPath, logEntry);
        }

        private void Cleanup()
        {
            _configWatcher?.Dispose();
            _folderWatcher?.Dispose();
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var daemon = new PermissionDaemon();
            daemon.Start();
        }
    }
}