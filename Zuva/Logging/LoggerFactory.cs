using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Zuva.Logging
{
    /// <summary>
    /// Default implementation of logger factory
    /// </summary>
    public class LoggerFactory : ILoggerFactory
    {
        private readonly ConcurrentDictionary<string, ILogger> _loggers;
        private readonly List<ILogTarget> _targets;
        private readonly object _targetsLock = new object();
        private LogLevel _globalMinimumLevel = LogLevel.Info;

        public LoggerFactory()
        {
            _loggers = new ConcurrentDictionary<string, ILogger>();
            _targets = new List<ILogTarget>();
        }

        public ILogger CreateLogger(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Logger name cannot be null or empty", nameof(name));

            return _loggers.GetOrAdd(name, loggerName => new Logger(loggerName, this));
        }

        public ILogger CreateLogger<T>()
        {
            return CreateLogger(typeof(T).Name);
        }

        public void SetMinimumLevel(LogLevel level)
        {
            _globalMinimumLevel = level;
            
            // Update all existing loggers
            foreach (var logger in _loggers.Values)
            {
                if (logger.MinimumLevel < level)
                {
                    logger.MinimumLevel = level;
                }
            }
        }

        public void AddTarget(ILogTarget target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            lock (_targetsLock)
            {
                if (!_targets.Contains(target))
                {
                    _targets.Add(target);
                }
            }
        }

        public void RemoveTarget(ILogTarget target)
        {
            if (target == null)
                return;

            lock (_targetsLock)
            {
                _targets.Remove(target);
            }
        }

        internal void WriteToTargets(LogEntry entry)
        {
            lock (_targetsLock)
            {
                foreach (var target in _targets)
                {
                    try
                    {
                        if (entry.Level >= target.MinimumLevel)
                        {
                            target.WriteLog(entry);
                        }
                    }
                    catch
                    {
                        // Swallow target exceptions to prevent logging from breaking the application
                    }
                }
            }
        }

        internal LogLevel GetGlobalMinimumLevel()
        {
            return _globalMinimumLevel;
        }
    }

    /// <summary>
    /// Default logger implementation
    /// </summary>
    internal class Logger : ILogger
    {
        private readonly LoggerFactory _factory;

        public Logger(string name, LoggerFactory factory)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            MinimumLevel = factory.GetGlobalMinimumLevel();
        }

        public string Name { get; }
        public LogLevel MinimumLevel { get; set; }

        public void Debug(string message)
        {
            Log(LogLevel.Debug, message);
        }

        public void Debug(string message, params object[] args)
        {
            if (IsEnabled(LogLevel.Debug))
            {
                Log(LogLevel.Debug, string.Format(message, args));
            }
        }

        public void Info(string message)
        {
            Log(LogLevel.Info, message);
        }

        public void Info(string message, params object[] args)
        {
            if (IsEnabled(LogLevel.Info))
            {
                Log(LogLevel.Info, string.Format(message, args));
            }
        }

        public void Warn(string message)
        {
            Log(LogLevel.Warn, message);
        }

        public void Warn(string message, params object[] args)
        {
            if (IsEnabled(LogLevel.Warn))
            {
                Log(LogLevel.Warn, string.Format(message, args));
            }
        }

        public void Warn(string message, Exception exception)
        {
            Log(LogLevel.Warn, message, exception);
        }

        public void Error(string message)
        {
            Log(LogLevel.Error, message);
        }

        public void Error(string message, params object[] args)
        {
            if (IsEnabled(LogLevel.Error))
            {
                Log(LogLevel.Error, string.Format(message, args));
            }
        }

        public void Error(string message, Exception exception)
        {
            Log(LogLevel.Error, message, exception);
        }

        public void Fatal(string message)
        {
            Log(LogLevel.Fatal, message);
        }

        public void Fatal(string message, Exception exception)
        {
            Log(LogLevel.Fatal, message, exception);
        }

        public bool IsEnabled(LogLevel level)
        {
            return level >= MinimumLevel;
        }

        public void Log(LogLevel level, string message)
        {
            Log(level, message, null);
        }

        public void Log(LogLevel level, string message, Exception exception)
        {
            if (!IsEnabled(level))
                return;

            var entry = new LogEntry
            {
                Level = level,
                LoggerName = Name,
                Message = message,
                Exception = exception
            };

            _factory.WriteToTargets(entry);
        }
    }
}