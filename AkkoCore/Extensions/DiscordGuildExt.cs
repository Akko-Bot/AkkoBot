using AkkoCore.Services.Caching.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoCore.Extensions;

public static class DiscordGuildExt
{
    /// <summary>
    /// Safely gets the the member with the specified ID.
    /// </summary>
    /// <param name="server">This Discord guild.</param>
    /// <param name="uid">The Discord user ID.</param>
    /// <returns>The member with the specified ID, <see langword="null"/> if they are not in the server.</returns>
    public static async Task<DiscordMember?> GetMemberSafelyAsync(this DiscordGuild server, ulong uid)
    {
        if (server.Members.TryGetValue(uid, out var member))
            return member;

        try { return await server.GetMemberAsync(uid); }
        catch { return default; }
    }

    /// <summary>
    /// Gets a Discord mention for the specified ID. Defaults to a user mention if
    /// a channel or role with the ID are not found.
    /// </summary>
    /// <param name="server">This Discord guild.</param>
    /// <param name="id">The ID of a Discord snowflake object.</param>
    /// <returns>The mention string.</returns>
    public static string GetMention(this DiscordGuild server, ulong id)
    {
        return (server.Channels.TryGetValue(id, out var channel))
            ? channel.Mention
            : server.Roles.TryGetValue(id, out var role)
                ? role.Mention
                : $"<@{id}>";
    }

    /// <summary>
    /// Gets a Discord mention for the specified ID. Defaults to the raw ID if
    /// no match was found.
    /// </summary>
    /// <param name="server">This Discord guild.</param>
    /// <param name="dbCache">The database cache.</param>
    /// <param name="id">The ID of a Discord snowflake object.</param>
    /// <returns>The mention string.</returns>
    public static string GetMentionWithoutPing(this DiscordGuild server, IDbCache dbCache, ulong id)
    {
        return (server.Channels.TryGetValue(id, out var channel))
            ? channel.Mention
            : server.Roles.TryGetValue(id, out var role)
                ? Formatter.InlineCode('@' + role.Name)
                : server.Members.TryGetValue(id, out var member)
                    ? Formatter.InlineCode(member.GetFullname())
                    : dbCache.Users.TryGetValue(id, out var dbUser)
                        ? dbUser.FullName
                        : Formatter.InlineCode(id.ToString());
    }
}