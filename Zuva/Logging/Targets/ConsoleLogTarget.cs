using System;

namespace Zuva.Logging.Targets
{
    /// <summary>
    /// Log target that writes to console (cTrader Print function)
    /// </summary>
    public class ConsoleLogTarget : ILogTarget
    {
        private readonly Action<string> _printFunction;

        public ConsoleLogTarget(Action<string> printFunction)
        {
            _printFunction = printFunction ?? throw new ArgumentNullException(nameof(printFunction));
            Name = "Console";
            MinimumLevel = LogLevel.Info;
        }

        public string Name { get; }
        public LogLevel MinimumLevel { get; set; }

        public void WriteLog(LogEntry entry)
        {
            if (entry == null)
                return;

            var message = FormatLogEntry(entry);
            _printFunction(message);
        }

        public void Flush()
        {
            // Console output is typically not buffered
        }

        private string FormatLogEntry(LogEntry entry)
        {
            var timestamp = entry.Timestamp.ToString("HH:mm:ss.fff");
            var level = entry.Level.ToString().ToUpper().PadRight(5);
            var logger = entry.LoggerName.Length > 20 
                ? entry.LoggerName.Substring(0, 17) + "..." 
                : entry.LoggerName.PadRight(20);

            var message = $"[{timestamp}] {level} {logger} | {entry.Message}";

            if (entry.Exception != null)
            {
                message += $"\n    Exception: {entry.Exception.GetType().Name}: {entry.Exception.Message}";
                if (!string.IsNullOrEmpty(entry.Exception.StackTrace))
                {
                    // Only show first few lines of stack trace to avoid overwhelming the console
                    var stackLines = entry.Exception.StackTrace.Split('\n');
                    var lineCount = Math.Min(3, stackLines.Length);
                    for (int i = 0; i < lineCount; i++)
                    {
                        message += $"\n    {stackLines[i].Trim()}";
                    }
                    if (stackLines.Length > 3)
                    {
                        message += $"\n    ... ({stackLines.Length - 3} more lines)";
                    }
                }
            }

            return message;
        }
    }
}