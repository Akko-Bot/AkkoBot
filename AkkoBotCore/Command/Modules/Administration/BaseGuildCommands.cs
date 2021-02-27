using AkkoBot.Command.Abstractions;
using AkkoBot.Command.Modules.Administration.Services;
using AkkoBot.Command.Modules.Self.Services;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Command.Modules.Administration
{
    [RequireGuild]
    public class BaseGuildCommands : AkkoCommandModule
    {
        private readonly GuildConfigService _guildService;
        private readonly BotConfigService _botService;

        public BaseGuildCommands(GuildConfigService guildService, BotConfigService botService)
        {
            _guildService = guildService;
            _botService = botService;
        }

        [Command("prefix")]
        [Description("cmd_guild_prefix")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task ChangePrefix(CommandContext context, [RemainingText, Description("arg_prefix")] string newPrefix = null)
        {
            if (string.IsNullOrWhiteSpace(newPrefix))
            {
                await CheckPrefix(context);
                return;
            }

            // Account for dumb users - this is not 100% accurate
            var prefix = newPrefix.Replace("set ", string.Empty);

            _guildService.GetOrSetProperty(context, x => x.Prefix = prefix);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_prefix_change", Formatter.InlineCode(prefix)));

            await context.RespondLocalizedAsync(embed);
        }

        /// <summary>
        /// Sends a message with the guild prefix to the context that triggered it.
        /// </summary>
        /// <param name="context">The command context.</param>
        private async Task CheckPrefix(CommandContext context)
        {
            var prefix = _guildService.GetOrSetProperty(context, x => x?.Prefix)
                ?? _botService.GetOrSetProperty(x => x.BotPrefix);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_prefix_check", Formatter.InlineCode(prefix)));

            await context.RespondLocalizedAsync(embed);
        }
    }
}