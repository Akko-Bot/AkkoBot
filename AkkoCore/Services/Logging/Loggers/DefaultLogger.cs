using AkkoCore.Config;
using AkkoCore.Services.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace AkkoCore.Services.Logging.Loggers
{
    /// <summary>
    /// Default logger for logging events.
    /// </summary>
    public class DefaultLogger : AkkoLogger
    {
        private LogLevel _minimumLevel;
        private IFileLogger _fileLogger;
        private string _logFormat;
        private string _timeFormat;

        public DefaultLogger(LogLevel minimumLevel, IFileLogger fileLogger, string logFormat, string timeFormat)
        {
            _minimumLevel = minimumLevel;
            _fileLogger = fileLogger;
            _logFormat = logFormat;
            _timeFormat = timeFormat;
        }

        public override bool IsEnabled(LogLevel logLevel)
            => logLevel >= _minimumLevel;

        public override void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            lock (lockObject) // Static protected member, inherited from AkkoLogger
            {
                base.ChangeConsoleTextColor(logLevel);

                // Add the header
                var logBuilder = new StringBuilder(LogStrategy.GetHeader(eventId, _logFormat, _timeFormat));
                logBuilder.Append(formatter(state, exception));

                var log = logBuilder.ToString();

                Console.WriteLine(log);

                // Create the log file
                if (_fileLogger is not null && !_fileLogger.IsDisposed)
                    _fileLogger.CacheLogging(log);

                Console.ResetColor();
                logBuilder.Clear();
            }
        }

        public override IDisposable BeginScope<TState>(TState state)
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
    }
}