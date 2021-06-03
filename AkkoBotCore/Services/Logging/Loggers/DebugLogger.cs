using AkkoBot.Config;
using AkkoBot.Services.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace AkkoBot.Services.Logging.Loggers
{
    /// <summary>
    /// Logger class that forces information logs to be logged as debug logs.
    /// </summary>
    public class DebugLogger : ILogger
    {
        private static readonly object _lock = new();
        private LogLevel _minimumLevel;
        private IFileLogger _fileLogger;
        private string _logFormat;
        private string _timeFormat;

        public DebugLogger(LogLevel minimumLevel, IFileLogger fileLogger, string logFormat, string timeFormat)
        {
            _minimumLevel = minimumLevel;
            _fileLogger = fileLogger;
            _logFormat = logFormat;
            _timeFormat = timeFormat;
        }

        public bool IsEnabled(LogLevel logLevel)
            => (logLevel >= _minimumLevel) && _minimumLevel is not LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            if (eventId == default) // For some reason, LinqToDB doesn't emit EventIds
                eventId = new(20101, "LinqToDB");

            lock (_lock)
            {
                ChangeConsoleTextColor((logLevel is LogLevel.Information) ? LogLevel.Debug : logLevel);

                var logBuilder = new StringBuilder(LogStrategy.GetHeader(eventId, _logFormat, _timeFormat));
                logBuilder.AppendLine(formatter(state, exception));

                var log = logBuilder.ToString();

                Console.WriteLine(log);

                // Create the log file
                if (_fileLogger is not null && !_fileLogger.IsDisposed)
                    _fileLogger.CacheLogging(log);

                Console.ResetColor();
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            if (state is not null and LogConfig logConfig)
            {
                _minimumLevel = logConfig.LogLevel;
                _logFormat = logConfig.LogFormat;
                _timeFormat = logConfig.LogTimeFormat;
            }

            if (state is not null and IFileLogger fileLogger)
                _fileLogger = fileLogger;

            return _fileLogger;
        }

        /// <summary>
        /// Changes the text color of the console according to the specified log level.
        /// </summary>
        /// <param name="logLevel">The level of the current log.</param>
        private void ChangeConsoleTextColor(LogLevel logLevel)
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