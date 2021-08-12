using AkkoCore.Common;
using DSharpPlus.Entities;

namespace AkkoCore.Extensions
{
    public static class DiscordInviteExt
    {
        /// <summary>
        /// Gets the invite URL for this guild invite.
        /// </summary>
        /// <param name="invite">This guild invite.</param>
        /// <returns>The invite URL.</returns>
        public static string GetInviteLink(this DiscordInvite invite)
            => AkkoConstants.DiscordInviteLinkBase + invite.Code;
    }
}