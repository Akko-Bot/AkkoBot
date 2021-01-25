using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace AkkoBot.Extensions
{
    public static class DiscordMessageExt
    {
        /// <summary>
        /// Gets whether the user confirmed the interctive action or not.
        /// </summary>
        /// <param name="msg">This Discord message.</param>
        /// <param name="context">The context of the message.</param>
        /// <param name="expectedResponse">The message the user is supposed to send to make command execution proceed.</param>
        /// <returns><see langword="true"/> if the user confirmed the action, <see langword="false"/> otherwise.</returns>
        public static bool UserConfirmedAction(this DiscordMessage msg, CommandContext context, string expectedResponse)
        {
            var response = msg.Content.ToLowerInvariant();
            var confirmation = context.FormatLocalized(expectedResponse);

            return response == confirmation
                || response == confirmation[..1];
        }
    }
}