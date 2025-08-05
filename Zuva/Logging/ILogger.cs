using System;

namespace Zuva.Logging
{
    /// <summary>
    /// Interface for logging functionality
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Gets the logger name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets or sets the minimum log level
        /// </summary>
        LogLevel MinimumLevel { get; set; }

        /// <summary>
        /// Logs a debug message
        /// </summary>
        void Debug(string message);

        /// <summary>
        /// Logs a debug message with parameters
        /// </summary>
        void Debug(string message, params object[] args);

        /// <summary>
        /// Logs an info message
        /// </summary>
        void Info(string message);

        /// <summary>
        /// Logs an info message with parameters
        /// </summary>
        void Info(string message, params object[] args);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        void Warn(string message);

        /// <summary>
        /// Logs a warning message with parameters
        /// </summary>
        void Warn(string message, params object[] args);

        /// <summary>
        /// Logs a warning message with exception
        /// </summary>
        void Warn(string message, Exception exception);

        /// <summary>
        /// Logs an error message
        /// </summary>
        void Error(string message);

        /// <summary>
        /// Logs an error message with parameters
        /// </summary>
        void Error(string message, params object[] args);

        /// <summary>
        /// Logs an error message with exception
        /// </summary>
        void Error(string message, Exception exception);

        /// <summary>
        /// Logs a fatal message
        /// </summary>
        void Fatal(string message);

        /// <summary>
        /// Logs a fatal message with exception
        /// </summary>
        void Fatal(string message, Exception exception);

        /// <summary>
        /// Checks if a log level is enabled
        /// </summary>
        bool IsEnabled(LogLevel level);

        /// <summary>
        /// Logs a message at the specified level
        /// </summary>
        void Log(LogLevel level, string message);

        /// <summary>
        /// Logs a message at the specified level with exception
        /// </summary>
        void Log(LogLevel level, string message, Exception exception);
    }

    /// <summary>
    /// Log levels
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        Fatal = 4,
        Off = 5
    }
}