using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AkkoBot.Commands.Modules.Utilities.Services
{
    /// <summary>
    /// Groups utility methods for generating Discord log messages.
    /// </summary>
    public class LogService : ICommandService
    {
        private readonly string _headerSeparator = new('=', 30);
        private readonly ILocalizer _localizer;

        public LogService(ILocalizer localizer)
            => _localizer = localizer;

        /// <summary>
        /// Generates a message log for the specified Discord messages.
        /// </summary>
        /// <param name="messages">The messages to be logged.</param>
        /// <param name="channel">The Discord channel the messages are from.</param>
        /// <param name="locale">The locale to be used for the header.</param>
        /// <param name="extraInfo">Extra info to be appended to the header.</param>
        /// <returns>The message log, <see langword="null"/> if the message collection is empty.</returns>
        public string GenerateMessageLog(IEnumerable<DiscordMessage> messages, DiscordChannel channel, string locale, string extraInfo = null)
        {
            var amount = messages?.Count();

            if (amount is 0 or null || channel is null)
                return null;

            var msgLog = new StringBuilder(GenerateLogHeader(channel, amount.Value, locale, extraInfo));

            foreach (var message in messages)
            {
                msgLog.AppendLine(
                    $"{message.Author.GetFullname()} ({message.Author.Id}) [{message.CreationTimestamp.LocalDateTime}]" + Environment.NewLine +
                    message.Content + ((string.IsNullOrWhiteSpace(message.Content)) ? string.Empty : Environment.NewLine) +
                    string.Join(Environment.NewLine, message.Attachments.Select(x => x.Url)) + Environment.NewLine
                );
            }

            return msgLog.ToString();
        }

        /// <summary>
        /// Generates the header of a message log.
        /// </summary>
        /// <param name="channel">The Discord channel the messages are from.</param>
        /// <param name="messageAmount">The amount of messages being logged.</param>
        /// <param name="locale">The locale to be used for the header.</param>
        /// <param name="extraInfo">Extra info to be appended to the header.</param>
        /// <returns>The log header.</returns>
        private string GenerateLogHeader(DiscordChannel channel, int messageAmount, string locale, string extraInfo = null)
        {
            return
                $"==> {_localizer.GetResponseString(locale, "log_channel_name")}: {channel.Name} | {_localizer.GetResponseString(locale, "id")}: {channel.Id}" + Environment.NewLine +
                $"==> {_localizer.GetResponseString(locale, "log_channel_topic")}: {channel.Topic}" + Environment.NewLine +
                $"==> {_localizer.GetResponseString(locale, "category")}: {channel.Parent.Name}" + Environment.NewLine +
                $"==> {_localizer.GetResponseString(locale, "log_messages_logged")}: {messageAmount}" + Environment.NewLine +
                extraInfo +
                $"{_headerSeparator}/{_localizer.GetResponseString(locale, "log_start")}/{_headerSeparator}" +
                Environment.NewLine + Environment.NewLine;
        }
    }
}
