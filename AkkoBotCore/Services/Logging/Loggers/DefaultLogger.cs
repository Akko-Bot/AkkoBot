using AkkoBot.Services.Logging.Abstractions;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace AkkoBot.Services.Logging.Loggers
{
    public class DefaultLogger : ILogger
    {
        private static readonly object _lock = new();

        private readonly LogLevel _minimumLevel;
        private readonly string _timeFormat;
        private readonly IFileLogger _fileLogger;
        private CommandContext _cmdContext;

        public DefaultLogger(LogLevel minLevel, string timestampFormat, IFileLogger fileLogger = null)
        {
            _minimumLevel = minLevel;
            _timeFormat = timestampFormat ?? "yyyy-MM-dd HH:mm:ss zzz";
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
                if (logLevel != LogLevel.Information)
                    ChangeConsoleTextColor(logLevel);

                // Add the header
                var logEntry = new StringBuilder(string.Empty);
                var eName = (eventId.Name?.Length > 12) ? eventId.Name?.Substring(0, 12) : eventId.Name;

                logEntry.Append($"[{DateTimeOffset.Now.ToString(_timeFormat)}] [{eName,-6}] ");

                // Add the message
                var message = formatter(state, exception);

                if (_cmdContext is null)
                {
                    logEntry.AppendLine(message);
                }
                else
                {
                    var logBody =
                        $"[Shard {_cmdContext.Client.ShardId}]\n\t" +
                        $"User: {_cmdContext.User.Username}#{_cmdContext.User.Discriminator} [{_cmdContext.User.Id}]\n\t" +
                        $"Server: {_cmdContext.Guild?.Name ?? "Private"} {((_cmdContext.Guild is null) ? string.Empty : "[" + _cmdContext.Guild.Id + "]")}\n\t" +
                        $"Channel: #{_cmdContext.Channel?.Name ?? "Private"} [{_cmdContext.Channel.Id}]\n\t" +
                        $"Message: {message}\n";

                    logEntry.AppendLine(logBody);
                }

                // Add the exception
                if (exception is not null)
                    logEntry.AppendLine(exception.ToString());

                // Create the log file
                if (_fileLogger is not null && !_fileLogger.IsDisposed)
                    _fileLogger.CacheLogging(logEntry.ToString());

                // Print the log
                Console.Write(logEntry.ToString());

                // Clean up
                _cmdContext = null;
                logEntry.Clear();
                Console.ResetColor();
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

        private void ChangeConsoleTextColor(LogLevel logLevel)
        {
            Console.ForegroundColor = logLevel switch
            {
                LogLevel.Trace => ConsoleColor.Gray,
                LogLevel.Debug => ConsoleColor.DarkGreen,
                LogLevel.Warning => ConsoleColor.DarkMagenta,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.Black,
                _ => ConsoleColor.White
            };

            if (logLevel == LogLevel.Critical)
                Console.BackgroundColor = ConsoleColor.Red;
        }
    }
}