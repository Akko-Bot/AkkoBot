using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoCore.Extensions
{
    public static class DiscordGuildExt
    {
        /// <summary>
        /// Safely gets the the member with the specified ID.
        /// </summary>
        /// <param name="server">This Discord guild.</param>
        /// <param name="uid">The Discord user ID.</param>
        /// <returns>The member with the specified ID, <see langword="null"/> if they are not in the server.</returns>
        public static async Task<DiscordMember> GetMemberSafelyAsync(this DiscordGuild server, ulong uid)
        {
            if (server.Members.TryGetValue(uid, out var member))
                return member;

            try { return await server.GetMemberAsync(uid); }
            catch { return null; }
        }
    }
}