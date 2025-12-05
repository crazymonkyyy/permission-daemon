using System.Collections.Generic;

namespace PermissionDaemon
{
    public class DaemonConfig
    {
        public string Version { get; set; } = "1.0";
        public List<Rule> Rules { get; set; } = new List<Rule>();
        public LoggingConfig Logging { get; set; } = new LoggingConfig();
    }

    public class Rule
    {
        public List<string> Patterns { get; set; } = new List<string>();
        public Dictionary<string, string> Permissions { get; set; } = new Dictionary<string, string>();
    }

    public class LoggingConfig
    {
        public string Level { get; set; } = "verbose";
        public bool Audit { get; set; } = true;
    }
}