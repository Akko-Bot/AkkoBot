using AkkoBot.Command.Abstractions;
using AkkoBot.Command.Attributes;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Command.Modules.Administration
{
    public class Administration : AkkoCommandModule
    {
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
    }
}
