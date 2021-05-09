using AkkoBot.Commands.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Timers.Abstractions;
using ConcurrentCollections;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Abstractions
{
    /// <summary>
    /// Represents a default database cache for an <see cref="AkkoDbContext"/>.
    /// </summary>
    public interface IDbCache : IDisposable
    {
        // TODO: add documentation for all of these
        ConcurrentHashSet<ulong> Blacklist { get; }
        BotConfigEntity BotConfig { get; }
        LogConfigEntity LogConfig { get; }
        ConcurrentDictionary<ulong, GuildConfigEntity> Guilds { get; }
        List<PlayingStatusEntity> PlayingStatuses { get; }
        ConcurrentDictionary<ulong, ConcurrentHashSet<AliasEntity>> Aliases { get; }
        ConcurrentDictionary<ulong, FilteredWordsEntity> FilteredWords { get; }
        ConcurrentDictionary<ulong, ConcurrentHashSet<FilteredContentEntity>> FilteredContent { get; }
        ITimerManager Timers { get; set; }
        ConcurrentDictionary<string, Command> DisabledCommandCache { get; set; }
        ICommandCooldown CooldownCommands { get; }
        ConcurrentDictionary<ulong, ConcurrentHashSet<PollEntity>> Polls { get; }

        /// <summary>
        /// Stores all repeaters with an interval lower than 1 day
        /// </summary>
        /// <remarks>The <see langword="ulong"/> is a Discord guild's ID, the <see cref="ConcurrentHashSet{T}"/> is the collection of repeaters.</remarks>
        ConcurrentDictionary<ulong, ConcurrentHashSet<RepeaterEntity>> Repeaters { get; }

        /// <summary>
        /// Safely gets a database guild.
        /// </summary>
        /// <param name="sid">The GuildId of the database entry.</param>
        /// <remarks>If the entry doesn't exist, it creates one.</remarks>
        /// <returns>The specified <see cref="GuildConfigEntity"/>.</returns>
        ValueTask<GuildConfigEntity> GetGuildAsync(ulong sid);
    }
}