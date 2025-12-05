using System;
using System.IO;

namespace PermissionDaemon
{
    class Program
    {
        static void Main(string[] args)
        {
            // Parse command line arguments
            string configPath = "permissions.yaml";
            string watchDirectory = Directory.GetCurrentDirectory();
            string agentName = Environment.UserName; // Default to current user as agent name

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-c":
                    case "--config":
                        if (i + 1 < args.Length)
                        {
                            configPath = args[++i];
                        }
                        else
                        {
                            Console.WriteLine("Error: --config requires a path");
                            return;
                        }
                        break;
                    case "-d":
                    case "--directory":
                        if (i + 1 < args.Length)
                        {
                            watchDirectory = args[++i];
                        }
                        else
                        {
                            Console.WriteLine("Error: --directory requires a path");
                            return;
                        }
                        break;
                    case "-a":
                    case "--agent":
                        if (i + 1 < args.Length)
                        {
                            agentName = args[++i];
                        }
                        else
                        {
                            Console.WriteLine("Error: --agent requires a name");
                            return;
                        }
                        break;
                    case "-h":
                    case "--help":
                        ShowHelp();
                        return;
                }
            }

            // Ensure config file exists
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Configuration file not found: {configPath}");
                Console.WriteLine("Creating a default configuration file...");
                
                // Copy the example configuration
                File.Copy("config.example.yaml", configPath, false);
                Console.WriteLine($"Default configuration created at: {configPath}");
            }

            // Ensure watch directory exists
            if (!Directory.Exists(watchDirectory))
            {
                Console.WriteLine($"Watch directory does not exist: {watchDirectory}");
                return;
            }

            Console.WriteLine($"Starting Permission Daemon...");
            Console.WriteLine($"Config file: {configPath}");
            Console.WriteLine($"Watch directory: {watchDirectory}");
            Console.WriteLine($"Agent name: {agentName}");
            Console.WriteLine();

            var daemon = new PermissionDaemon(configPath, watchDirectory, agentName);
            
            try
            {
                daemon.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running daemon: {ex.Message}");
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Permission Daemon - Prevents unauthorized file access by AI agents");
            Console.WriteLine();
            Console.WriteLine("Usage: PermissionDaemon [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -c, --config PATH     Path to the configuration file (default: permissions.yaml)");
            Console.WriteLine("  -d, --directory PATH  Directory to watch (default: current directory)");
            Console.WriteLine("  -a, --agent NAME      Agent name to use for permission checks (default: current user)");
            Console.WriteLine("  -h, --help            Show this help message");
            Console.WriteLine();
            Console.WriteLine("Example:");
            Console.WriteLine("  PermissionDaemon --config mypermissions.yaml --agent editor --directory /path/to/project");
        }
    }
}