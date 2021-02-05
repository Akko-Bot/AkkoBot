using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Command.Modules.Administration.Services
{
    public class UserPunishmentService : ICommandService
    {
        /// <summary>
        /// Sends a direct message to the specified user with a localized message of the punishment they
        /// received in the context guild.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user that's being punished.</param>
        /// <param name="message">The localized message to be sent.</param>
        /// <param name="reason">The reason of the punishment.</param>
        /// <returns>The <see cref="DiscordMessage"/> that has been sent, <see langword="null"/> if it failed to send the message.</returns>
        public async Task<DiscordMessage> SendPunishmentDm(CommandContext context, DiscordMember user, string message, string reason)
        {
            // Create the notification dm
            var dm = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized(message, Formatter.Bold(context.Guild.Name)));

            if (reason is not null)
                dm.AddField(context.FormatLocalized("reason"), reason);

            // This returns null if it fails
            return await context.SendLocalizedDmAsync(user, dm, true);
        }
    }
}