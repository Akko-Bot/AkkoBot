using DSharpPlus.Entities;

namespace AkkoBot.Extensions
{
    public static class DiscordUserExt
    {
        /// <summary>
        /// Gets the username and discriminator of this user.
        /// </summary>
        /// <param name="user">This DiscordUser.</param>
        /// <returns>A string in the format "Username#1234".</returns>
        public static string GetFullname(this DiscordUser user)
            => $"{user.Username}#{user.Discriminator}";
    }
}