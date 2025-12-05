using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PermissionDaemon
{
    public class PermissionDaemon
    {
        private readonly string _configPath;
        private readonly string _watchDirectory;
        private readonly string _agentName;
        private FileWatcher _fileWatcher;
        private PermissionEnforcer _permissionEnforcer;
        private ILogger _logger;
        private DaemonConfig _config;

        public PermissionDaemon(string configPath, string watchDirectory, string agentName)
        {
            _configPath = configPath;
            _watchDirectory = watchDirectory;
            _agentName = agentName;
        }

        public void Start()
        {
            LoadConfiguration();

            // Initialize logger based on config
            _logger = new ConsoleLogger(
                ParseLogLevel(_config.Logging.Level), 
                _config.Logging.Audit
            );

            _logger.LogInfo($"Permission Daemon starting for agent: {_agentName}");
            _logger.LogInfo($"Watching config file: {_configPath}");
            _logger.LogInfo($"Watching directory: {_watchDirectory}");

            // Create permission enforcer
            _permissionEnforcer = new PermissionEnforcer(_config, _agentName);

            // Create file watcher
            _fileWatcher = new FileWatcher(
                _configPath,
                _watchDirectory,
                OnConfigFileChanged,
                OnMonitoredFileChanged
            );

            _fileWatcher.StartWatching();

            _logger.LogInfo("Permission Daemon started successfully. Press Ctrl+C to stop.");

            // Keep the daemon running
            Console.CancelKeyPress += (sender, e) => {
                e.Cancel = true; // Prevent termination
                Stop();
            };

            // Keep the main thread alive
            while (true)
            {
                System.Threading.Thread.Sleep(1000);
            }
        }

        public void Stop()
        {
            _logger?.LogInfo("Stopping Permission Daemon...");
            _fileWatcher?.StopWatching();
            _logger?.LogInfo("Permission Daemon stopped.");
        }

        private void LoadConfiguration()
        {
            try
            {
                var yaml = File.ReadAllText(_configPath);
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention.Instance)
                    .Build();
                
                _config = deserializer.Deserialize<DaemonConfig>(yaml);
                _logger?.LogDebug($"Configuration loaded from {_configPath}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Failed to load configuration from {_configPath}: {ex.Message}");
                // Use default config if loading fails
                _config = new DaemonConfig();
            }
        }

        private void OnConfigFileChanged()
        {
            _logger?.LogInfo("Configuration file changed, reloading...");
            LoadConfiguration();
            _permissionEnforcer?.UpdateConfig(_config);
            _logger?.LogInfo("Configuration reloaded successfully.");
        }

        private void OnMonitoredFileChanged(string eventType, FileSystemEventArgs e)
        {
            _logger?.LogDebug($"File {eventType}: {e.FullPath}");
            
            // Check if this file is protected based on current rules
            if (_permissionEnforcer != null)
            {
                // This is where we would intercept and potentially block operations
                // For now, we'll just log access attempts
                var accessType = eventType switch
                {
                    "Deleted" => System.Security.AccessControl.FileSystemRights.Delete,
                    "Changed" => System.Security.AccessControl.FileSystemRights.WriteData,
                    "Created" => System.Security.AccessControl.FileSystemRights.WriteData,
                    "Renamed" => System.Security.AccessControl.FileSystemRights.WriteData,
                    _ => System.Security.AccessControl.FileSystemRights.ReadData
                };

                var hasPermission = _permissionEnforcer.HasPermission(e.FullPath, accessType);
                
                _logger?.LogAudit($"Access attempt - File: {e.FullPath}, Type: {eventType}, Agent: {_agentName}, Allowed: {hasPermission}");
                
                if (!hasPermission)
                {
                    _logger?.LogWarning($"ACCESS BLOCKED - Agent '{_agentName}' does not have permission to {eventType.ToLower()} file: {e.FullPath}");
                    // In a real implementation, we would block the operation here
                }
                
                // Enforce permissions (apply file attributes, etc.)
                _permissionEnforcer.EnforcePermissions(e.FullPath);
            }
        }

        private LogLevel ParseLogLevel(string level)
        {
            return level.ToLower() switch
            {
                "minimal" => LogLevel.Minimal,
                "verbose" => LogLevel.Verbose,
                "debug" => LogLevel.Debug,
                _ => LogLevel.Verbose
            };
        }
    }
}