using AkkoCore.Common;
using AkkoCore.Config.Abstractions;
using AkkoCore.Extensions;
using ConcurrentCollections;
using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace AkkoCore.Config.Models
{
    /// <summary>
    /// Stores settings related to the bot.
    /// </summary>
    public sealed class BotConfig : Settings, IMessageSettings
    {
        private string _locale = AkkoConstants.DefaultLanguage;
        private string _okColor = "007FFF";
        private string _errorColor = "FB3D28";
        private TimeSpan _bulkGatekeepingTime = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Groups the qualified name of commands that have been disabled globally.
        /// </summary>
        [YamlMember(Description = @"Groups the qualified name of commands that have been disabled globally.")]
        public ConcurrentHashSet<string> DisabledCommands { get; init; } = new();

        /// <summary>
        /// The default bot locale.
        /// </summary>
        [YamlMember(Description = @"The default bot locale. Defaults to """ + AkkoConstants.DefaultLanguage + @""".")]
        public string Locale
        {
            get => _locale;
            set => _locale = value?.MaxLength(10);
        }

        /// <summary>
        /// The default color for embeds, in hexadecimal.
        /// </summary>
        [YamlMember(Description = @"The default color for embeds, in hexadecimal. Defaults to ""007FFF"".")]
        public string OkColor
        {
            get => _okColor;
            set => _okColor = value?.MaxLength(6).ToUpperInvariant();
        }

        /// <summary>
        /// The default color for error embeds, in hexadecimal.
        /// </summary>
        [YamlMember(Description = @"The default error color for embeds, in hexadecimal. Defaults to ""FB3D28"".")]
        public string ErrorColor
        {
            get => _errorColor;
            set => _errorColor = value?.MaxLength(6).ToUpperInvariant();
        }

        /// <summary>
        /// Defines how long the bot waits on bulk greetings and farewells.
        /// </summary>
        [YamlMember(Description = @"Defines how long the bot waits on bulk greetings and farewells. Defaults to ""00:00:05"".")]
        public TimeSpan BulkGatekeepTime
        {
            get => _bulkGatekeepingTime;
            set => _bulkGatekeepingTime = value <= TimeSpan.Zero ? TimeSpan.FromSeconds(5) : value;
        }

        /// <summary>
        /// The default bot prefix.
        /// </summary>
        [YamlMember(Description = @"The default bot prefix. Defaults to ""!"".")]
        public string Prefix { get; set; } = "!";

        /// <summary>
        /// The default name to be used on webhooks for guild logging.
        /// </summary>
        [YamlMember(Description = @"The default name to be used on webhooks for guild logging. Defaults to ""AkkoLog"".")]
        public string WebhookLogName { get; set; } = "AkkoLog";

        /// <summary>
        /// Defines whether embeds should be used in responses by default.
        /// </summary>
        [YamlMember(Description = @"Defines whether embeds should be used in responses by default. Defaults to ""true"". Values: true, false")]
        public bool UseEmbed { get; set; } = true;

        /// <summary>
        /// Defines whether the bot responds to commands in direct message.
        /// </summary>
        [YamlMember(Description = @"Defines whether the bot responds to commands in direct message. Defaults to ""true"". Values: true, false")]
        public bool RespondToDms { get; set; } = true;

        /// <summary>
        /// Defines whether the bot responds to commands prefixed with a mention to the bot.
        /// </summary>
        [YamlMember(Description = @"Defines whether the bot responds to commands prefixed with a mention to the bot. Defaults to ""false"". Values: true, false")]
        public bool MentionPrefix { get; set; } = false;

        /// <summary>
        /// Defines whether the bot should respond to help commands.
        /// </summary>
        [YamlMember(Description = @"Defines whether the bot should respond to help commands. Defaults to ""true"". Values: true, false")]
        public bool EnableHelp { get; set; } = true;

        /// <summary>
        /// Defines whether the bot has statuses in rotation.
        /// </summary>
        [YamlMember(Description = @"Defines whether the bot has statuses in rotation. Setting it to ""false"" disables status rotation.")]
        public bool RotateStatus { get; set; } = false;

        /// <summary>
        /// Defines whether commands are case sensitive or not.
        /// </summary>
        [YamlMember(Description = @"Defines whether commands are case sensitive or not. Defaults to ""true"". Values: true, false")]
        public bool CaseSensitiveCommands { get; set; } = true;

        /// <summary>
        /// Defines the message cache size for every <see cref="DSharpPlus.DiscordClient"/>.
        /// </summary>
        [YamlMember(Description = @"Defines the message cache size for every Discord client. Defaults to ""200"".")]
        public int MessageSizeCache { get; set; } = 200;

        /// <summary>
        /// Defines the minimum amount of time that guilds are allowed to set automatic expirations of warnings.
        /// </summary>
        [YamlMember(Description = @"Defines the minimum amount of time that guilds are allowed to set automatic expirations of warnings. Defaults to ""30.00:00:00"".")]
        public TimeSpan MinWarnExpire { get; set; } = TimeSpan.FromDays(30);

        /// <summary>
        /// Defines the maximum amount of time that an interactive command waits for user input.
        /// </summary>
        [YamlMember(Description = @"Defines the maximum amount of time that an interactive command waits for user input. Defaults to ""00:01:00"".")]
        public TimeSpan? InteractiveTimeout { get; set; } = TimeSpan.FromSeconds(60);

        public override IReadOnlyDictionary<string, string> GetSettings()
        {
            var result = base.GetSettings() as Dictionary<string, string>;
            result[nameof(DisabledCommands).ToSnakeCase()] = string.Join(", ", DisabledCommands);

            return result;
        }
    }
}