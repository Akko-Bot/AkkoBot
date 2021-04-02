using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Commands.Modules.Self.Services;
using AkkoBot.Common;
using AkkoBot.Credential;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Self
{
    [BotOwner]
    [Group("botconfig"), Aliases("self", "botcfg")]
    [Description("cmd_config")]
    public class BotConfig : AkkoCommandModule
    {
        private readonly BotConfigService _service;

        public BotConfig(BotConfigService service)
            => _service = service;

        [Command("prefix")]
        [Description("cmd_config_prefix")]
        public async Task SetBotPrefix(CommandContext context, [Description("arg_prefix")] string prefix)
            => await ChangeProperty(context, x => x.BotPrefix = prefix);

        [Command("locale")]
        [Description("cmd_config_locale")]
        public async Task ListLocales(CommandContext context)
        {
            var locales = _service.GetLocales()
                .Select(code => (code, new CultureInfo(code).NativeName))
                .OrderBy(code => code);

            var embed = new DiscordEmbedBuilder()
                .WithTitle("locales_title")
                .AddField("code", string.Join('\n', locales.Select(x => x.code).ToArray()), true)
                .AddField("language", string.Join('\n', locales.Select(x => x.NativeName).ToArray()), true);

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("locale")]
        [Description("cmd_config_locale")]
        public async Task SetBotLocale(CommandContext context, [Description("arg_locale")] string languageCode)
            => await ChangeProperty(context, x => x.Locale = languageCode);

        [Command("okcolor")]
        [Description("cmd_config_okcolor")]
        public async Task SetBotOkColor(CommandContext context, [Description("arg_color")] string newColor)
            => await ChangeProperty(context, x => x.OkColor = newColor);

        [Command("errorcolor")]
        [Description("cmd_config_errorcolor")]
        public async Task SetBotErrorColor(CommandContext context, [Description("arg_color")] string newColor)
            => await ChangeProperty(context, x => x.ErrorColor = newColor);

        [Command("embed"), Aliases("useembed")]
        [Description("cmd_config_embed")]
        public async Task SetBotUseEmbed(CommandContext context, [Description("arg_bool")] bool useEmbed)
            => await ChangeProperty(context, x => x.UseEmbed = useEmbed);

        [Command("dm"), Aliases("dms", "respondtodms")]
        [Description("cmd_config_dm")]
        public async Task SetBotRespondDms(CommandContext context, [Description("arg_bool")] bool respondToDms)
            => await ChangeProperty(context, x => x.RespondToDms = respondToDms);

        [Command("help"), Aliases("withhelp")]
        [Description("cmd_config_help")]
        public async Task SetBotEnableHelp(CommandContext context, [Description("arg_bool")] bool enableHelp)
            => await ChangeProperty(context, x => x.EnableHelp = enableHelp);

        [Command("mentionprefix"), Aliases("mention")]
        [Description("cmd_config_mention")]
        public async Task SetBotMentionPrefix(CommandContext context, [Description("arg_bool")] bool mention)
            => await ChangeProperty(context, x => x.MentionPrefix = mention);

        [Command("casesensitive"), Aliases("case")]
        [Description("cmd_config_case")]
        public async Task SetBotCaseSensitive(CommandContext context, [Description("arg_bool")] bool caseSensitive)
            => await ChangeProperty(context, x => x.CaseSensitiveCommands = caseSensitive);

        [Command("cachesize"), Aliases("cache")]
        [Description("cmd_config_cache")]
        public async Task SetBotCacheSize(CommandContext context, [Description("arg_uint")] uint cacheSize)
            => await ChangeProperty(context, x => x.MessageSizeCache = (int)cacheSize);

        [Command("timeout")]
        [Description("cmd_config_timeout")]
        public async Task SetBotTimeout(CommandContext context, [Description("arg_config_timeout")] TimeSpan time)
            => await ChangeProperty(context, x => x.InteractiveTimeout = (time < TimeSpan.FromSeconds(10) ? TimeSpan.FromSeconds(10) : time));

        [Command("warnexpire"), Aliases("warne")]
        [Description("cmd_warne")]
        public async Task SetMinWarnExpire(CommandContext context, [Description("arg_timed_warn")] TimeSpan time)
            => await ChangeProperty(context, x => x.MinWarnExpire = time);

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_config_list")]
        public async Task ListBotSettings(CommandContext context)
        {
            var settings = _service.GetConfigs();

            var embed = new DiscordEmbedBuilder()
                .WithTitle("bot_settings_title")
                .AddField("settings", string.Join("\n", settings.Keys.ToArray()), true)
                .AddField("value", string.Join("\n", settings.Values.ToArray()), true);

            await context.RespondLocalizedAsync(embed);
        }

        private async Task ChangeProperty<T>(CommandContext context, Func<BotConfigEntity, T> selector)
        {
            _service.GetOrSetProperty(selector);
            await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
        }

        [Group("log"), Aliases("logs", "logging")]
        [Description("cmd_config_log")]
        public class LogConfig : AkkoCommandModule
        {
            private readonly BotConfigService _service;

            public LogConfig(BotConfigService service)
                => _service = service;

            [Command("level")]
            [Description("cmd_config_log_level")]
            public async Task SetBotLogLevel(CommandContext context, [Description("arg_loglevel")] LogLevel logLevel)
                => await ChangeProperty(context, x => x.LogLevel = logLevel);

            [Command("format")]
            [Description("cmd_config_log_format")]
            public async Task SetBotLogFormat(CommandContext context, [Description("cmd_config_log_format_arg")] string logFormat)
                => await ChangeProperty(context, x => x.LogFormat = logFormat);

            [Command("timeformat")]
            [Description("cmd_config_log_timeformat")]
            public async Task SetBotLogTimeFormat(CommandContext context, [Description("cmd_config_log_timeformat_arg")] string logTimeFormat = null)
                => await ChangeProperty(context, x => x.LogTimeFormat = logTimeFormat);

            [Command("save")]
            [Description("cmd_config_log_save")]
            public async Task SetFileLogging(CommandContext context, [Description("arg_bool")] bool isEnabled)
                => await ChangeProperty(context, x => x.IsLoggedToFile = isEnabled);

            [Command("size"), Aliases("setsize")]
            [Description("cmd_config_log_size")]
            public async Task SetFileMaxSize(CommandContext context, [Description("arg_double")] double size)
                => await ChangeProperty(context, x => x.LogSizeMB = size);

            private async Task ChangeProperty<T>(CommandContext context, Func<LogConfigEntity, T> selector)
            {
                _service.GetOrSetProperty(selector);
                await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
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
            public async Task AddOwner(CommandContext context, [Description("arg_discord_user")] DiscordUser user)
            {
                if (_creds.OwnerIds.Add(user.Id))
                {
                    using var writer = _service.SerializeCredentials(_creds);
                    await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
                }
                else
                    await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
            }

            [Command("remove"), Aliases("rem")]
            [Description("cmd_config_owner_rem")]
            public async Task RemoveOwner(CommandContext context, [Description("arg_discord_user")] DiscordUser user)
            {
                if (_creds.OwnerIds.Remove(user.Id))
                {
                    using var writer = _service.SerializeCredentials(_creds);
                    await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
                }
                else
                    await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
            }

            [GroupCommand, Command("list")]
            [Description("cmd_config_owner_list")]
            public async Task ListOwners(CommandContext context)
            {
                var ids = _creds.OwnerIds
                    .Select(id => $"<@{id}>")
                    .ToArray();

                var embed = new DiscordEmbedBuilder()
                    .AddField("owners", (ids.Length is 0) ? "-" : string.Join("\n", ids), true)
                    .AddField("app_owners", string.Join("\n", context.Client.CurrentApplication.Owners.Select(user => user.Mention).ToArray()), true);

                await context.RespondLocalizedAsync(embed);
            }
        }
    }
}