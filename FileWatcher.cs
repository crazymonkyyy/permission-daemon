using System;
using System.IO;

namespace PermissionDaemon
{
    public class FileWatcher
    {
        private FileSystemWatcher _configWatcher;
        private FileSystemWatcher _directoryWatcher;
        private readonly string _configPath;
        private readonly string _watchDirectory;
        private readonly Action _onConfigChanged;
        private readonly Action<string, FileSystemEventArgs> _onFileChanged;

        public FileWatcher(string configPath, string watchDirectory, Action onConfigChanged, Action<string, FileSystemEventArgs> onFileChanged)
        {
            _configPath = configPath;
            _watchDirectory = watchDirectory;
            _onConfigChanged = onConfigChanged;
            _onFileChanged = onFileChanged;
        }

        public void StartWatching()
        {
            // Watch the directory containing the config file for changes to the config
            string configDirectory = Path.GetDirectoryName(_configPath);
            string configFileName = Path.GetFileName(_configPath);

            _configWatcher = new FileSystemWatcher(configDirectory)
            {
                Filter = configFileName,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _configWatcher.Changed += (sender, e) =>
            {
                Console.WriteLine($"Configuration file changed: {e.FullPath}");
                System.Threading.Thread.Sleep(500); // Brief delay to ensure file is fully written
                _onConfigChanged?.Invoke();
            };

            _configWatcher.Deleted += (sender, e) =>
            {
                Console.WriteLine($"Configuration file deleted: {e.FullPath}");
            };

            // Watch the main directory for file changes
            _directoryWatcher = new FileSystemWatcher(_watchDirectory)
            {
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _directoryWatcher.Changed += (sender, e) => _onFileChanged?.Invoke("Changed", e);
            _directoryWatcher.Created += (sender, e) => _onFileChanged?.Invoke("Created", e);
            _directoryWatcher.Deleted += (sender, e) => _onFileChanged?.Invoke("Deleted", e);
            _directoryWatcher.Renamed += (sender, e) => _onFileChanged?.Invoke("Renamed", e);

            Console.WriteLine($"Started watching config file: {_configPath}");
            Console.WriteLine($"Started watching directory: {_watchDirectory}");
        }

        public void StopWatching()
        {
            _configWatcher?.Dispose();
            _directoryWatcher?.Dispose();
        }
    }
}