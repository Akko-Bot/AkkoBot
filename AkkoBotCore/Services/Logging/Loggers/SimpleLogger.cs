using System;
using System.Text;
using AkkoBot.Services.Logging.Abstractions;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Services.Logging.Loggers
{
    public class SimpleLogger : ILogger
    {
        private static readonly object _lock = new();

        private readonly LogLevel _minimumLevel;
        private readonly string _timeFormat;
        private readonly IAkkoFileLogger _fileLogger;
        private CommandContext _cmdContext;

        public SimpleLogger(LogLevel minLevel, string timestampFormat, IAkkoFileLogger fileLogger = null)
        {
            _minimumLevel = minLevel;
            _timeFormat = timestampFormat ?? "HH:mm";
            _fileLogger = fileLogger;
        }

        public bool IsEnabled(LogLevel logLevel)
            => logLevel >= _minimumLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            lock (_lock)
            {
                // Add the header
                var logEntry = new StringBuilder(string.Empty);
                var eName = (eventId.Name?.Length > 12) ? eventId.Name?.Substring(0, 12) : eventId.Name;

                logEntry.Append($"[{DateTimeOffset.Now.ToString(_timeFormat)}] [{eventId.Id,-4}/{eName,-12}] ");
                Console.Write(logEntry.ToString());

                logEntry.Append(PrintColoredLabel(logLevel));

                // Fix foreground color issue with logs of critical level
                if (logLevel == LogLevel.Critical)
                    Console.Write(" ");

                // Add the message
                var message = formatter(state, exception);

                if (_cmdContext is null)
                {
                    Console.WriteLine(message);
                    logEntry.AppendLine(message);
                }
                else
                {
                    var logBody =
                        $"[{_cmdContext.Client.ShardId}] " +
                        $"g:{_cmdContext.Guild.Id} " +
                        $"| c:{_cmdContext.Channel.Id} " +
                        $"| u:{_cmdContext.User.Id} " +
                        $"| msg: {message} ";

                    Console.WriteLine(logBody);
                    logEntry.AppendLine(logBody);
                }

                // Add the exception
                if (exception is not null)
                {
                    Console.WriteLine(exception);
                    logEntry.AppendLine(exception.ToString());
                }

                // Create the log file
                if (_fileLogger is not null && !_fileLogger.IsDisposed)
                    _fileLogger.CacheLogging(logEntry.ToString());

                // Clean up
                logEntry.Clear();
                _cmdContext = null;
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            _cmdContext = state as CommandContext;

            if (_cmdContext is null)
                throw new InvalidOperationException("This logger instance only supports \"CommandContext\" objects.");
            else
                return default;
        }

        private string PrintColoredLabel(LogLevel logLevel)
        {
            Console.ForegroundColor = logLevel switch
            {
                LogLevel.Trace => ConsoleColor.Gray,
                LogLevel.Debug => ConsoleColor.DarkMagenta,
                LogLevel.Information => ConsoleColor.DarkCyan,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.Black,
                _ => ConsoleColor.White
            };

            if (logLevel == LogLevel.Critical)
                Console.BackgroundColor = ConsoleColor.Red;

            var label = logLevel switch
            {
                LogLevel.Trace => "[Trace] ",
                LogLevel.Debug => "[Debug] ",
                LogLevel.Information => "[Info ] ",
                LogLevel.Warning => "[Warn ] ",
                LogLevel.Error => "[Error] ",
                LogLevel.Critical => "[Crit ]",
                LogLevel.None => "[None ] ",
                _ => "[?????] "
            };

            Console.Write(label);
            Console.ResetColor();

            return label;
        }
    }
}
