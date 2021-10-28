using Microsoft.Extensions.Logging;
using System;

namespace AkkoCore.Services.Logging.Abstractions
{
    /// <summary>
    /// Represents an object that logs bot events.
    /// </summary>
    public abstract class AkkoLogger : ILogger
    {
        /// <summary>
        /// Dummy object used for locking purposes.
        /// </summary>
        protected static readonly object lockObject = new();

        public abstract IDisposable? BeginScope<TState>(TState state);

        public abstract bool IsEnabled(LogLevel logLevel);

        public abstract void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter);

        /// <summary>
        /// Changes the text color of the console according to the specified log level.
        /// </summary>
        /// <param name="logLevel">The level of the current log.</param>
        protected virtual void ChangeConsoleTextColor(LogLevel logLevel)
        {
            Console.ForegroundColor = logLevel switch
            {
                LogLevel.Trace => ConsoleColor.Gray,
                LogLevel.Debug => ConsoleColor.DarkGreen,
                LogLevel.Warning => ConsoleColor.DarkMagenta,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.Black,
                _ => Console.ForegroundColor
            };

            if (logLevel is LogLevel.Critical)
                Console.BackgroundColor = ConsoleColor.Red;
        }
    }
}