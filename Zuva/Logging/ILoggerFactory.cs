namespace Zuva.Logging
{
    /// <summary>
    /// Interface for creating loggers
    /// </summary>
    public interface ILoggerFactory
    {
        /// <summary>
        /// Creates a logger with the specified name
        /// </summary>
        ILogger CreateLogger(string name);

        /// <summary>
        /// Creates a logger for the specified type
        /// </summary>
        ILogger CreateLogger<T>();

        /// <summary>
        /// Sets the global minimum log level
        /// </summary>
        void SetMinimumLevel(LogLevel level);

        /// <summary>
        /// Adds a log output target
        /// </summary>
        void AddTarget(ILogTarget target);

        /// <summary>
        /// Removes a log output target
        /// </summary>
        void RemoveTarget(ILogTarget target);
    }
}