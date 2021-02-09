using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Command.Modules.Administration.Services
{
    public class RoleService : ICommandService
    {
        /// <summary>
        /// Checks if the <paramref name="context"/> user can perform actions on the specified <paramref name="user"/> and sends an error message if the check fails.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="user">The targeted user.</param>
        /// <param name="errorMessage">The error message to be sent if the check fails.</param>
        /// <returns><see langword="true"/> if the context user is above in the hierarchy, <see langword="false"/> otherwise.</returns>
        public async Task<bool> CheckHierarchyAsync(CommandContext context, DiscordMember user, string errorMessage)
        {
            if ((context.Member.Hierarchy <= user.Hierarchy 
            && context.Guild.CurrentMember.Hierarchy <= user.Hierarchy)
            || user.Equals(context.Guild.CurrentMember))
            {
                var embed = new DiscordEmbedBuilder().WithDescription(errorMessage);
                await context.RespondLocalizedAsync(embed, isError: true);

                return false;
            }

            return true;
        }
    }
}