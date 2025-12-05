using System;
using System.IO;

namespace PermissionDaemon
{
    public enum LogLevel
    {
        Minimal,
        Verbose,
        Debug
    }

    public interface ILogger
    {
        void Log(LogLevel level, string message);
        void LogInfo(string message);
        void LogWarning(string message);
        void LogError(string message);
        void LogDebug(string message);
        void LogAudit(string message);
    }

    public class ConsoleLogger : ILogger
    {
        private readonly LogLevel _logLevel;
        private readonly bool _auditEnabled;

        public ConsoleLogger(LogLevel logLevel, bool auditEnabled)
        {
            _logLevel = logLevel;
            _auditEnabled = auditEnabled;
        }

        public void Log(LogLevel level, string message)
        {
            if (ShouldLog(level))
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var levelStr = level.ToString().ToUpper();
                Console.WriteLine($"[{timestamp}] [{levelStr}] {message}");
            }
        }

        public void LogInfo(string message)
        {
            Log(LogLevel.Verbose, message);
        }

        public void LogWarning(string message)
        {
            Log(LogLevel.Minimal, $"WARNING: {message}");
        }

        public void LogError(string message)
        {
            Log(LogLevel.Minimal, $"ERROR: {message}");
        }

        public void LogDebug(string message)
        {
            Log(LogLevel.Debug, $"DEBUG: {message}");
        }

        public void LogAudit(string message)
        {
            if (_auditEnabled)
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                Console.WriteLine($"[{timestamp}] [AUDIT] {message}");
            }
        }

        private bool ShouldLog(LogLevel level)
        {
            return level switch
            {
                LogLevel.Minimal => true,
                LogLevel.Verbose => _logLevel <= LogLevel.Verbose,
                LogLevel.Debug => _logLevel == LogLevel.Debug,
                _ => true
            };
        }
    }
}