using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using AkkoBot.Command.Attributes;
using AkkoBot.Command.Modules.Self.Services;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Command.Modules.Self
{
    [BotOwner]
    [RequireBotPermissions(Permissions.AddReactions)]
    [Group("config"), Aliases("self")]
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
        public async Task SetBotLocale(CommandContext context, [Description("arg_locale")] string locale)
            => await ChangeProperty(context, x => x.Locale = locale);

        [Command("okcolor")]
        [Description("cmd_config_okcolor")]
        public async Task SetBotOkColor(CommandContext context, [Description("arg_color")] string okColor)
            => await ChangeProperty(context, x => x.OkColor = okColor);

        [Command("errorcolor")]
        [Description("cmd_config_errorcolor")]
        public async Task SetBotErrorColor(CommandContext context, [Description("arg_color")] string errorColor)
            => await ChangeProperty(context, x => x.ErrorColor = errorColor);

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
        public async Task SetBotTimeout(CommandContext context, [Description("arg_uint")] uint time)
            => await ChangeProperty(context, x => x.InteractiveTimeout = new TimeSpan(0, 0, (time < 10) ? 10 : (int)time));

        [Command("locale")]
        [Description("cmd_config_locale")]
        public async Task ListLocales(CommandContext context)
        {
            var locales = _service.GetLocales(context)
                .Select(x => $"{Formatter.InlineCode(x)} - {new CultureInfo(x).NativeName}")
                .OrderBy(x => x)
                .ToArray();

            var embed = new DiscordEmbedBuilder()
                .WithTitle("locales_title")
                .WithDescription(string.Join("\n", locales));

            await context.RespondLocalizedAsync(embed, false);
        }

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_config_list")]
        public async Task GetBotSettings(CommandContext context)
        {
            var configNames = new StringBuilder();
            var configValues = new StringBuilder();

            foreach (var setting in _service.GetConfigs(context))
            {
                configNames.AppendLine(setting.Key);
                configValues.AppendLine(setting.Value);
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle("bot_settings_title")
                .AddField("settings", configNames.ToString(), true)
                .AddField("value", configValues.ToString(), true);

            await context.RespondLocalizedAsync(embed);
        }

        private async Task ChangeProperty(CommandContext context, Action<BotConfigEntity> selector)
        {
            _service.SetProperty(context, selector);

            await context.Message.CreateReactionAsync(
                DiscordEmoji.FromName(context.Client, ":white_check_mark:")
            );
        }

        [BotOwner]
        [RequireBotPermissions(Permissions.AddReactions)]
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

            private async Task ChangeProperty(CommandContext context, Action<LogConfigEntity> selector)
            {
                _service.SetProperty(context, selector);

                await context.Message.CreateReactionAsync(
                    DiscordEmoji.FromName(context.Client, ":white_check_mark:")
                );
            }
        }
    }
}