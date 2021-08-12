using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Commands.Modules.Self.Services;
using AkkoCore.Common;
using AkkoCore.Config;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services;
using AkkoCore.Services.Logging;
using AkkoCore.Services.Logging.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Self
{
    [BotOwner]
    [Group("botconfig"), Aliases("self", "bot")]
    [Description("cmd_config")]
    public class BotConfigCommands : AkkoCommandModule
    {
        private readonly BotConfigService _botService;
        private readonly GuildConfigService _guildService;

        public BotConfigCommands(BotConfigService botService, GuildConfigService guildService)
        {
            _botService = botService;
            _guildService = guildService;
        }

        [Command("prefix")]
        [Description("cmd_config_prefix")]
        public async Task SetBotPrefixAsync(CommandContext context, [Description("arg_prefix")] string prefix)
            => await ChangePropertyAsync(context, x => x.BotPrefix = prefix);

        [Command("reloadlocales"), Aliases("reloadresponses")]
        [Description("cmd_config_reloadlocales")]
        public async Task ReloadResponseStringsAsync(CommandContext context)
                => await context.Message.CreateReactionAsync((_botService.ReloadLocales() is not 0) ? AkkoStatics.SuccessEmoji : AkkoStatics.WarningEmoji);

        [Command("locale")]
        [Description("cmd_config_locale")]
        public async Task ListLocalesAsync(CommandContext context)
        {
            var locales = _botService.GetLocales()
                .OrderBy(code => code);

            var embed = new SerializableDiscordMessage()
                .WithTitle("locales_title")
                .AddField("code", string.Join('\n', locales), true)
                .AddField("language", string.Join('\n', locales.Select(x => GeneralService.GetCultureInfo(x)?.NativeName ?? x)), true);

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("locale")]
        [Description("cmd_config_locale")]
        public async Task SetBotLocaleAsync(CommandContext context, [Description("arg_locale")] string languageCode)
        {
            if (!_guildService.IsLocaleRegistered(languageCode, out var result))
            {
                await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
                return;
            }

            await ChangePropertyAsync(context, x => x.Locale = result);
        }

        [Command("okcolor")]
        [Description("cmd_config_okcolor")]
        public async Task SetBotOkColorAsync(CommandContext context, [Description("arg_color")] string newColor)
        {
            if (!GeneralService.GetColor(newColor).HasValue)
            {
                await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
                return;
            }

            await ChangePropertyAsync(context, x => x.OkColor = newColor);
        }

        [Command("errorcolor")]
        [Description("cmd_config_errorcolor")]
        public async Task SetBotErrorColorAsync(CommandContext context, [Description("arg_color")] string newColor)
        {
            if (!GeneralService.GetColor(newColor).HasValue)
            {
                await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
                return;
            }

            await ChangePropertyAsync(context, x => x.ErrorColor = newColor.MaxLength(AkkoConstants.MaxUsernameLength));
        }

        [Command("webhooklogname")]
        [Description("cmd_config_webhooklogname")]
        public async Task SetWebhookDefaultNameAsync(CommandContext context, [Description("arg_webhooklogname"), RemainingText] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
                return;
            }

            await ChangePropertyAsync(context, x => x.WebhookLogName = name);
        }

        [Command("embed"), Aliases("useembed")]
        [Description("cmd_config_embed")]
        public async Task SetBotUseEmbedAsync(CommandContext context, [Description("arg_bool")] bool useEmbed)
            => await ChangePropertyAsync(context, x => x.UseEmbed = useEmbed);

        [Command("dm"), Aliases("dms", "respondtodms")]
        [Description("cmd_config_dm")]
        public async Task SetBotRespondDmsAsync(CommandContext context, [Description("arg_bool")] bool respondToDms)
            => await ChangePropertyAsync(context, x => x.RespondToDms = respondToDms);

        [Command("help"), Aliases("withhelp")]
        [Description("cmd_config_help")]
        public async Task SetBotEnableHelpAsync(CommandContext context, [Description("arg_bool")] bool enableHelp)
            => await ChangePropertyAsync(context, x => x.EnableHelp = enableHelp);

        [Command("mentionprefix"), Aliases("mention")]
        [Description("cmd_config_mention")]
        public async Task SetBotMentionPrefixAsync(CommandContext context, [Description("arg_bool")] bool mention)
            => await ChangePropertyAsync(context, x => x.MentionPrefix = mention);

        [Command("casesensitive"), Aliases("case")]
        [Description("cmd_config_case")]
        public async Task SetBotCaseSensitiveAsync(CommandContext context, [Description("arg_bool")] bool caseSensitive)
            => await ChangePropertyAsync(context, x => x.CaseSensitiveCommands = caseSensitive);

        [Command("cachesize"), Aliases("cache")]
        [Description("cmd_config_cache")]
        public async Task SetBotCacheSizeAsync(CommandContext context, [Description("arg_uint")] uint cacheSize)
            => await ChangePropertyAsync(context, x => x.MessageSizeCache = (int)cacheSize);

        [Command("timeout")]
        [Description("cmd_config_timeout")]
        public async Task SetBotTimeoutAsync(CommandContext context, [Description("arg_config_timeout")] TimeSpan time)
            => await ChangePropertyAsync(context, x => x.InteractiveTimeout = (time < TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : time));

        [Command("warnexpire"), Aliases("warne")]
        [Description("cmd_warne")]
        public async Task SetMinWarnExpireAsync(CommandContext context, [Description("arg_timed_warn")] TimeSpan time)
            => await ChangePropertyAsync(context, x => x.MinWarnExpire = time);

        [Command("gatekeepbulktime")]
        [Description("cmd_gatekeepbulktime")]
        public async Task SetBulkGatekeepTimeAsync(CommandContext context, [Description("arg_gatekeep_bulktime")] TimeSpan time)
            => await ChangePropertyAsync(context, x => x.BulkGatekeepTime = time);

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_config_list")]
        public async Task ListBotSettingsAsync(CommandContext context)
        {
            var settings = _botService.GetConfigs();

            var embed = new SerializableDiscordMessage()
                .WithTitle("bot_settings_title")
                .AddField("settings", string.Join("\n", settings.Keys.ToArray()), true)
                .AddField("value", string.Join("\n", settings.Values.ToArray()), true);

            await context.RespondLocalizedAsync(embed);
        }

        private async Task ChangePropertyAsync<T>(CommandContext context, Func<BotConfig, T> selector)
        {
            _botService.GetOrSetProperty(selector);
            await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
        }

        [Group("log"), Aliases("logs", "logging")]
        [Description("cmd_config_log")]
        public class LogConfigCommands : AkkoCommandModule
        {
            private readonly BotConfigService _service;

            public LogConfigCommands(BotConfigService service)
                => _service = service;

            [Command("level")]
            [Description("cmd_config_log_level")]
            public async Task SetBotLogLevelAsync(CommandContext context, [Description("arg_loglevel")] LogLevel logLevel)
            {
                await ChangePropertyAsync(context, x => x.LogLevel = logLevel);

                var logConfig = _service.GetLogConfig();
                context.Services.GetService<IAkkoLoggerProvider>().UpdateLoggers(logConfig);
            }

            [Command("format")]
            [Description("cmd_config_log_format")]
            public async Task SetBotLogFormatAsync(CommandContext context, [Description("cmd_config_log_format_arg")] string logFormat)
            {
                await ChangePropertyAsync(context, x => x.LogFormat = logFormat);

                var logConfig = _service.GetLogConfig();
                context.Services.GetService<IAkkoLoggerProvider>().UpdateLoggers(logConfig);
            }

            [Command("timeformat")]
            [Description("cmd_config_log_timeformat")]
            public async Task SetBotLogTimeFormatAsync(CommandContext context, [Description("cmd_config_log_timeformat_arg")] string logTimeFormat = null)
            {
                if (!GeneralService.IsValidTimeFormat(logTimeFormat))
                {
                    await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
                    return;
                }

                await ChangePropertyAsync(context, x => x.LogTimeFormat = logTimeFormat);

                var logConfig = _service.GetLogConfig();
                context.Services.GetService<IAkkoLoggerProvider>().UpdateLoggers(logConfig);
            }

            [Command("filetimestamp")]
            [Description("cmd_config_log_filetimestamp")]
            public async Task SetFileLogTimeFormatAsync(CommandContext context, [Description("cmd_config_log_timeformat_arg")] string timestampFormat = null)
            {
                if (!GeneralService.IsValidTimeFormat(timestampFormat))
                {
                    await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
                    return;
                }

                await ChangePropertyAsync(context, x => x.LogTimeStamp = timestampFormat);

                if (context.Client.Logger.BeginScope(null) is IFileLogger fileLogger)
                    fileLogger.TimeStampFormat = timestampFormat;
            }

            [Command("save")]
            [Description("cmd_config_log_save")]
            public async Task SetFileLoggingAsync(CommandContext context, [Description("arg_bool")] bool isEnabled)
            {
                var wasEnabled = _service.GetLogConfig().IsLoggedToFile;
                await ChangePropertyAsync(context, x => x.IsLoggedToFile = isEnabled);

                if (isEnabled && isEnabled != wasEnabled)
                {
                    var logConfig = _service.GetLogConfig();
                    var fileLogger = new AkkoFileLogger(logConfig.LogSizeMb, logConfig.LogTimeStamp);
                    context.Services.GetService<IAkkoLoggerProvider>().UpdateFileLogger(fileLogger);
                }
                else if (!isEnabled)
                    context.Services.GetService<IAkkoLoggerProvider>().UpdateFileLogger(null);
            }

            [Command("size"), Aliases("setsize")]
            [Description("cmd_config_log_size")]
            public async Task SetFileMaxSizeAsync(CommandContext context, [Description("arg_double")] double size)
            {
                await ChangePropertyAsync(context, x => x.LogSizeMb = size);

                if (context.Client.Logger.BeginScope(null) is IFileLogger fileLogger)
                    fileLogger.FileSizeLimitMB = size;
            }

            private async Task ChangePropertyAsync<T>(CommandContext context, Func<LogConfig, T> selector)
            {
                _service.GetOrSetProperty(selector);
                await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
            }
        }

        [Group("owner"), Aliases("owners")]
        [Description("cmd_config_owner")]
        public class OwnerConfig : AkkoCommandModule
        {
            private readonly Credentials _creds;
            private readonly BotConfigService _service;

            public OwnerConfig(Credentials creds, BotConfigService service)
            {
                _creds = creds;
                _service = service;
            }

            [Command("add")]
            [Description("cmd_config_owner_add")]
            public async Task AddOwnerAsync(CommandContext context, [Description("arg_discord_user")] DiscordUser user)
            {
                if (_creds.OwnerIds.Add(user.Id))
                {
                    using var writer = _service.SerializeCredentials(_creds);
                    await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
                }
                else
                    await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
            }

            [Command("remove"), Aliases("rm")]
            [Description("cmd_config_owner_rem")]
            public async Task RemoveOwnerAsync(CommandContext context, [Description("arg_discord_user")] DiscordUser user)
            {
                if (_creds.OwnerIds.TryRemove(user.Id))
                {
                    using var writer = _service.SerializeCredentials(_creds);
                    await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
                }
                else
                    await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
            }

            [GroupCommand, Command("list"), Aliases("show")]
            [Description("cmd_config_owner_list")]
            public async Task ListOwnersAsync(CommandContext context)
            {
                var ids = _creds.OwnerIds
                    .Select(id => $"<@{id}>")
                    .ToArray();

                var embed = new SerializableDiscordMessage()
                    .AddField("owners", (ids.Length is 0) ? "-" : string.Join("\n", ids), true)
                    .AddField("app_owners", string.Join("\n", context.Client.CurrentApplication.Owners.Select(user => user.Mention).ToArray()), true);

                await context.RespondLocalizedAsync(embed);
            }
        }
    }
}