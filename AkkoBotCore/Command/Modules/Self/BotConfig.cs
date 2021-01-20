using System;
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

namespace AkkoBot.Command.Modules.Self
{
    [BotOwner]
    [RequireBotPermissions(Permissions.AddReactions)]
    [Group("config"), Aliases("self", "bot")]
    [Description("This module manages the settings that define how the bot should behave globally.")]
    public class BotConfig : AkkoCommandModule
    {
        private readonly BotConfigService _service;

        public BotConfig(BotConfigService service)
            => _service = service;

        [Command("prefix")]
        [Description("Sets the default prefix for the bot.")]
        public async Task SetBotPrefix(CommandContext context, string prefix)
            => await ChangeProperty(context, x => x.BotPrefix = prefix);

        [Command("locale")]
        [Description("Sets the default locale for the bot.")]
        public async Task SetBotLocale(CommandContext context, string locale)
            => await ChangeProperty(context, x => x.Locale = locale);

        [Command("okcolor")]
        [Description("Sets the default okcolor for the bot.")]
        public async Task SetBotOkColor(CommandContext context, string okColor)
            => await ChangeProperty(context, x => x.OkColor = okColor);

        [Command("errorcolor")]
        [Description("Sets the default okcolor for the bot.")]
        public async Task SetBotErrorColor(CommandContext context, string errorColor)
            => await ChangeProperty(context, x => x.ErrorColor = errorColor);

        [Command("logformat")]
        [Description("Sets the log format used for logging on the console.")]
        public async Task SetBotLogFormat(CommandContext context, string logFormat)
            => await ChangeProperty(context, x => x.LogFormat = logFormat);

        [Command("logtimeformat")]
        [Description("Sets the time format used for logging on the console.")]
        public async Task SetBotLogTimeFormat(CommandContext context, string logTimeFormat)
            => await ChangeProperty(context, x => x.LogTimeFormat = logTimeFormat);

        [Command("embed"), Aliases("useembed")]
        [Description("Sets whether the bot should use embeds for responses or not.")]
        public async Task SetBotUseEmbed(CommandContext context, bool useEmbed)
            => await ChangeProperty(context, x => x.UseEmbed = useEmbed);

        [Command("dm"), Aliases("dms", "respondtodms")]
        [Description("Sets whether the bot should execute commands in direct messages or not.")]
        public async Task SetBotRespondDms(CommandContext context, bool respondToDms)
            => await ChangeProperty(context, x => x.RespondToDms = respondToDms);

        [Command("help"), Aliases("withhelp")]
        [Description("Sets whether the bot respond to help commands or not.")]
        public async Task SetBotEnableHelp(CommandContext context, bool enableHelp)
            => await ChangeProperty(context, x => x.EnableHelp = enableHelp);

        [Command("mentionprefix"), Aliases("mention")]
        [Description("Sets whether the bot should respond to commands executed with a mention to itself as a prefix.")]
        public async Task SetBotMentionPrefix(CommandContext context, bool mention)
            => await ChangeProperty(context, x => x.MentionPrefix = mention);

        [Command("casesensitive"), Aliases("case")]
        [Description("Sets whether commands are case sensitive or not.")]
        public async Task SetBotCaseSensitive(CommandContext context, bool caseSensitive)
            => await ChangeProperty(context, x => x.CaseSensitiveCommands = caseSensitive);

        [Command("cachesize"), Aliases("cache")]
        [Description("Sets how many messages should be cached per channel.")]
        public async Task SetBotCacheSize(CommandContext context, int cacheSize)
            => await ChangeProperty(context, x => x.MessageSizeCache = cacheSize);

        [Command("list"), Aliases("show")]
        [Description("Shows the bot's current settings.")]
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
    }
}