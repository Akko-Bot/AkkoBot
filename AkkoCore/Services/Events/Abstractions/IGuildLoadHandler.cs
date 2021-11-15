using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Abstractions;

/// <summary>
/// Represents an object that handles caching of guild settings.
/// </summary>
public interface IGuildLoadHandler
{
    /// <summary>
    /// Removes a guild from the cache when the bot leaves a Discord guild;
    /// </summary>
    Task RemoveGuildOnLeaveAsync(DiscordClient client, GuildDeleteEventArgs eventArgs);

    /// <summary>
    /// Saves default guild settings to the database and caches it when the bot joins a Discord guild.
    /// </summary>
    Task AddGuildOnJoinAsync(DiscordClient client, GuildCreateEventArgs eventArgs);
}