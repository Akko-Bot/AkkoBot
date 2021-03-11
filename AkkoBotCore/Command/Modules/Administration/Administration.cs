using AkkoBot.Command.Abstractions;
using AkkoBot.Command.Attributes;
using AkkoBot.Command.Modules.Administration.Services;
using AkkoBot.Command.Modules.Self.Services;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Command.Modules.Administration
{
    [RequireGuild]
    public class Administration : AkkoCommandModule
    {
        private readonly GuildConfigService _guildService;
        private readonly BotConfigService _botService;

        public Administration(GuildConfigService guildService, BotConfigService botService)
        {
            _guildService = guildService;
            _botService = botService;
        }

        [BotOwner, Hidden]
        [Command("sudo")]
        [Description("cmd_sudo")]
        public async Task Sudo(
            CommandContext context,
            [Description("arg_discord_user")] DiscordUser user,
            [RemainingText, Description("arg_command")] string command)
        {
            var cmd = context.CommandsNext.FindCommand(command, out var args);

            if (cmd is null)
            {
                var embed = new DiscordEmbedBuilder().WithDescription("command_not_found");
                await context.RespondLocalizedAsync(embed, isError: true);

                return;
            }

            var fakeContext = context.CommandsNext.CreateFakeContext(user, context.Channel, command, context.Prefix, cmd, args);
            var failedChecks = await cmd.RunChecksAsync(fakeContext, false);

            if (failedChecks.Any())
            {
                var embed = new DiscordEmbedBuilder().WithDescription("command_check_failed");
                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                await cmd.ExecuteAsync(fakeContext);
            }
        }

        [Command("prefix")]
        [Description("cmd_guild_prefix")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task ChangePrefix(CommandContext context, [RemainingText, Description("arg_prefix")] string newPrefix = null)
        {
            if (string.IsNullOrWhiteSpace(newPrefix))
            {
                await CheckPrefixAsync(context);
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
        private async Task CheckPrefixAsync(CommandContext context)
        {
            var prefix = _guildService.GetOrSetProperty(context, x => x?.Prefix)
                ?? _botService.GetOrSetProperty(x => x.BotPrefix);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_prefix_check", Formatter.InlineCode(prefix)));

            await context.RespondLocalizedAsync(embed);
        }
    }
}