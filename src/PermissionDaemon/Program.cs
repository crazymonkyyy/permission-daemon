using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Extensions.FileSystemGlobbing;

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
        public string AllowedUser { get; set; } = "";
    }

    public class SimplePermissionDaemon
    {
        private FileSystemWatcher? _configWatcher;
        private FileSystemWatcher? _folderWatcher;
        private DaemonConfig _config = new();
        private readonly object _lock = new();
        private readonly string _configPath;
        private readonly string _rootDirectory;

        public SimplePermissionDaemon()
        {
            _rootDirectory = Environment.CurrentDirectory;
            _configPath = Path.Combine(_rootDirectory, "permissions.config");
        }

        public void Start()
        {
            Console.WriteLine("Simple Permission Daemon starting...");
            Console.WriteLine($"Monitoring directory: {_rootDirectory}");
            
            // Ensure config exists
            EnsureConfigExists();
            
            // Load initial config
            LoadConfig();
            
            // Setup watchers
            SetupWatchers();
            
            Console.WriteLine("Simple Permission Daemon running. Press 'q' to quit.");
            
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

        private void EnsureConfigExists()
        {
            if (!File.Exists(_configPath))
            {
                Console.WriteLine("Config file not found. Creating default config...");
                CreateDefaultConfig();
            }
        }

        private void SetupWatchers()
        {
            // Watch for config file changes
            var configDir = Path.GetDirectoryName(_configPath) ?? _rootDirectory;
            var configFileName = Path.GetFileName(_configPath);
            
            try
            {
                _configWatcher = new FileSystemWatcher(configDir)
                {
                    Filter = configFileName,
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                _configWatcher.Changed += OnConfigChanged;
                _configWatcher.Created += OnConfigChanged;
                _configWatcher.Deleted += OnConfigChanged;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to setup config watcher: {ex.Message}");
            }

            // Watch the current directory for file operations
            try
            {
                _folderWatcher = new FileSystemWatcher(_rootDirectory)
                {
                    IncludeSubdirectories = true,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                _folderWatcher.Created += OnFileEvent;
                _folderWatcher.Changed += OnFileEvent;
                _folderWatcher.Deleted += OnFileEvent;
                _folderWatcher.Renamed += OnFileRenamed;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to setup folder watcher: {ex.Message}");
            }
            
            Console.WriteLine($"Watching config file: {_configPath}");
            Console.WriteLine($"Watching directory: {_rootDirectory}");
        }

        private void OnConfigChanged(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Add a small delay to ensure the file is fully written
                System.Threading.Thread.Sleep(300);
                LoadConfig();
                Console.WriteLine($"Configuration reloaded at {DateTime.Now:HH:mm:ss}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reloading config: {ex.Message}");
            }
        }

        private void OnFileEvent(object sender, FileSystemEventArgs e)
        {
            try
            {
                lock (_lock)
                {
                    ProcessFileEvent(e.FullPath, e.ChangeType);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in file event handling: {ex.Message}");
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            try
            {
                lock (_lock)
                {
                    ProcessFileEvent(e.OldFullPath, WatcherChangeTypes.Deleted);
                    ProcessFileEvent(e.FullPath, WatcherChangeTypes.Created);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in file rename handling: {ex.Message}");
            }
        }

        private void ProcessFileEvent(string fullPath, WatcherChangeTypes changeType)
        {
            var relativePath = Path.GetRelativePath(_rootDirectory, fullPath);
            
            // Skip if the file itself is the config file
            if (string.Equals(fullPath, _configPath, StringComparison.OrdinalIgnoreCase))
                return;
            
            // Log the operation with timestamp
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {changeType}: {relativePath}");
            
            // Only prevent deletion for now (as per spec requirement about preventing AI's from deleting files)
            if (changeType == WatcherChangeTypes.Deleted)
            {
                // Check against each rule in the configuration
                foreach (var rule in _config.Rules)
                {
                    try
                    {
                        // Use glob pattern matching
                        var matcher = new Matcher();
                        matcher.AddInclude(rule.Pattern);
                        
                        // Check if the file path matches the rule's pattern
                        if (IsPathMatchingPattern(fullPath, matcher))
                        {
                            // Check if the current user is allowed to delete
                            var currentUser = GetCurrentUser();
                            if (!string.Equals(currentUser, rule.AllowedUser, StringComparison.OrdinalIgnoreCase))
                            {
                                // Access denied - this is a violation
                                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ACCESS VIOLATION: User '{currentUser}' not allowed to delete '{relativePath}' (matches pattern '{rule.Pattern}', allowed user: '{rule.AllowedUser}')");
                                
                                // Log the violation
                                LogAccessViolation(currentUser, relativePath, rule.Pattern, "delete");
                                
                                return; // Stop checking other rules once we find a match
                            }
                            else
                            {
                                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Delete allowed for user '{currentUser}': {relativePath}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking rule for pattern '{rule.Pattern}': {ex.Message}");
                    }
                }
            }
            else
            {
                // For non-delete operations, we could still check permissions
                // But for now, focus on preventing deletions as per spec
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Operation allowed: {changeType} - {relativePath}");
            }
        }

        private bool IsPathMatchingPattern(string fullPath, Matcher matcher)
        {
            // Get directory info for the matcher to execute against
            var directory = new DirectoryInfo(Path.GetDirectoryName(fullPath) ?? _rootDirectory);
            var directoryInfoWrapper = new DirectoryInfoWrapper(directory);
            
            var matchResult = matcher.Execute(directoryInfoWrapper);
            
            // Extract the relative path from the root directory to check for matches
            var relativePath = Path.GetRelativePath(_rootDirectory, fullPath).Replace('\\', '/');
            
            // Check if any of the matched files match our target path
            foreach (var file in matchResult.Files)
            {
                var matchedPath = file.Path.Replace('\\', '/');
                if (string.Equals(matchedPath, relativePath, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }

        private string GetCurrentUser()
        {
            try
            {
                // In a real implementation, you might want to get the effective user more reliably
                // For this implementation, we'll use the environment username
                return Environment.UserName;
            }
            catch
            {
                // Fallback if environment variable is not available
                return "unknown";
            }
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
                        PropertyNameCaseInsensitive = true
                    });

                    if (newConfig != null)
                    {
                        _config = newConfig;
                        Console.WriteLine($"Configuration loaded with {_config.Rules.Count} rules");
                        
                        // Log the loaded rules for transparency
                        foreach (var rule in _config.Rules)
                        {
                            Console.WriteLine($"  Rule: Pattern='{rule.Pattern}', AllowedUser='{rule.AllowedUser}'");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Config file is empty or invalid, using default config");
                        CreateDefaultConfig();
                    }
                }
                else
                {
                    Console.WriteLine("Config file does not exist, using default config");
                    CreateDefaultConfig();
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
            
            // Example rule: only tester can delete test files
            _config.Rules.Add(new Rule 
            { 
                Pattern = "**/*test*.cs", 
                AllowedUser = "tester"
            });
            
            // Example rule: only editor can delete source code files
            _config.Rules.Add(new Rule 
            { 
                Pattern = "**/*.cs", 
                AllowedUser = "editor"
            });
            
            // Example rule: only admin can delete config files
            _config.Rules.Add(new Rule 
            { 
                Pattern = "**/*.config", 
                AllowedUser = "admin"
            });
            
            // Save the default config to file
            try
            {
                var jsonString = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_configPath, jsonString);
                Console.WriteLine($"Default configuration saved to: {_configPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save default config: {ex.Message}");
            }
        }

        private void LogAccessViolation(string user, string filePath, string pattern, string operation)
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] VIOLATION: User '{user}' attempted {operation} on '{filePath}' (matched pattern '{pattern}')\n";
            
            Console.Write(logEntry);
            
            // Write to a log file
            var logPath = Path.Combine(_rootDirectory, "access_violations.log");
            try
            {
                File.AppendAllText(logPath, logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }

        private void Cleanup()
        {
            try
            {
                _configWatcher?.Dispose();
                _folderWatcher?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during cleanup: {ex.Message}");
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var daemon = new SimplePermissionDaemon();
            daemon.Start();
        }
    }
}