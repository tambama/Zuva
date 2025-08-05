using System;

namespace Zuva.Logging
{
    /// <summary>
    /// Interface for log output targets
    /// </summary>
    public interface ILogTarget
    {
        /// <summary>
        /// Gets the target name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets the minimum log level for this target
        /// </summary>
        LogLevel MinimumLevel { get; set; }

        /// <summary>
        /// Writes a log entry
        /// </summary>
        void WriteLog(LogEntry entry);

        /// <summary>
        /// Flushes any buffered log entries
        /// </summary>
        void Flush();
    }

    /// <summary>
    /// Represents a log entry
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public string LoggerName { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }
        public string ThreadId { get; set; }

        public LogEntry()
        {
            Timestamp = DateTime.UtcNow;
            ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();
        }
    }
}