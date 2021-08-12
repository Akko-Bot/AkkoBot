using Microsoft.Extensions.Logging;
using System;
using System.Globalization;

namespace AkkoCore.Services.Logging
{
    /// <summary>
    /// Groups methods for formatting log messages.
    /// </summary>
    public static class LogStrategy
    {
        /// <summary>
        /// Gets a method for formatting a log message.
        /// </summary>
        /// <param name="strategyName">The name of the log format.</param>
        /// <remarks>Defaults to "Default".</remarks>
        /// <returns>The method to format the log.</returns>
        public static Func<LogData, Exception, string> GetLogStrategy(string strategyName)
        {
            return strategyName?.ToLowerInvariant() switch
            {
                "minimalist" => Minimalist,
                "simple" => Simple,
                _ => Default
            };
        }

        /// <summary>
        /// Gets the header for a log message.
        /// </summary>
        /// <param name="eventId">The event ID.</param>
        /// <param name="strategyName">The name of the log format.</param>
        /// <param name="timeFormat">The time format to be used on the timestamps.</param>
        /// <remarks>Defaults to "Default".</remarks>
        /// <returns>The log header.</returns>
        public static string GetHeader(EventId eventId, string strategyName, string timeFormat)
        {
            return strategyName?.ToLowerInvariant() switch
            {
                "minimalist" => MinimalistHeader(eventId, timeFormat),
                "simple" => SimpleHeader(eventId, timeFormat),
                _ => DefaultHeader(eventId, timeFormat)
            };
        }

        /// <summary>
        /// Gets the default log header.
        /// </summary>
        private static string DefaultHeader(EventId eventId, string timeFormat)
        {
            var eName = (eventId.Name?.Length > 12) ? eventId.Name?.Substring(0, 12) : eventId.Name;
            return $"[{DateTimeOffset.Now.ToString(timeFormat ?? "yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)}] [{eName,-6}] ";
        }

        /// <summary>
        /// Gets the simple log header.
        /// </summary>
        private static string SimpleHeader(EventId eventId, string timeFormat)
        {
            var eName = (eventId.Name?.Length > 12) ? eventId.Name?.Substring(0, 12) : eventId.Name;
            return $"[{DateTimeOffset.Now.ToString(timeFormat ?? "HH:mm", CultureInfo.InvariantCulture)}] [{eventId.Id,-4}/{eName,-12}] ";
        }

        /// <summary>
        /// Gets the minimalist log header.
        /// </summary>
        private static string MinimalistHeader(EventId eventId, string timeFormat)
        {
            var eName = (eventId.Name?.Length > 7) ? eventId.Name?.Substring(0, 7) : eventId.Name;
            return $"[{DateTimeOffset.Now.ToString(timeFormat ?? "HH:mm", CultureInfo.InvariantCulture)}] [{eName,-7}] ";
        }

        /// <summary>
        /// Gets the default log message.
        /// </summary>
        private static string Default(LogData logData, Exception exception)
        {
            var message = (logData.Context is null)
                ? logData.OptionalMessage
                : $"[Shard {logData.Context.Client.ShardId}] {logData.OptionalMessage}\n\t" +
                    $"User: {logData.Context.User.Username}#{logData.Context.User.Discriminator} [{logData.Context.User.Id}]\n\t" +
                    $"Server: {logData.Context.Guild?.Name ?? "Private"} {((logData.Context.Guild is null) ? string.Empty : "[" + logData.Context.Guild.Id + "]")}\n\t" +
                    $"Channel: #{logData.Context.Channel.Name ?? "Private"} [{logData.Context.Channel.Id}]\n\t" +
                    $"Message: {logData.Context.Message.Content}";

            // Add the exception
            return (exception is not null)
                ? $"{message}\n{exception}\n"
                : message + "\n";
        }

        /// <summary>
        /// Gets the simple log message.
        /// </summary>
        private static string Simple(LogData logData, Exception exception)
        {
            var message = (logData.Context is null)
                ? logData.OptionalMessage
                : $"[{logData.Context.Client.ShardId}] {logData.OptionalMessage} | " +
                    $"g:{((logData.Context.Guild is null) ? "Private" : logData.Context.Guild.Id)} " +
                    $"| c:{logData.Context.Channel.Id} " +
                    $"| u:{logData.Context.User.Id} " +
                    $"| msg: {logData.Context.Message.Content} ";

            // Add the exception
            return (exception is not null)
                ? $"{message}\n{exception}\n"
                : message;
        }

        /// <summary>
        /// Gets the minimalist log message.
        /// </summary>
        private static string Minimalist(LogData logData, Exception exception)
        {
            var message = (logData.Context is null)
                ? logData.OptionalMessage
                : $"[{logData.Context.Client.ShardId}] {logData.OptionalMessage} | " +
                    $"{logData.Context.User.Username}#{logData.Context.User.Discriminator}: {logData.Context.Message.Content} " +
                    $"| {logData.Context.Guild?.Name ?? "Private"} | #{logData.Context.Channel.Name ?? "Private"}";

            // Add exception
            return (exception is not null)
                ? $"{message}\n{exception}\n"
                : message;
        }
    }
}