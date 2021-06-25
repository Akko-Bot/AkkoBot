using AkkoBot.Commands.Abstractions;
using AkkoBot.Config;
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
        /// <summary>
        /// Contains all users that have interacted with the bot.
        /// </summary>
        public ConcurrentDictionary<ulong, DiscordUserEntity> Users { get; }

        /// <summary>
        /// Contains all blacklisted IDs (guilds, channels and users).
        /// </summary>
        ConcurrentHashSet<ulong> Blacklist { get; }

        /// <summary>
        /// Contains settings that define how the bot should behave globally.
        /// </summary>
        BotConfig BotConfig { get; }

        /// <summary>
        /// Contains settings that define how logging should be handled.
        /// </summary>
        LogConfig LogConfig { get; }

        /// <summary>
        /// Contains the settings of all Discord guilds currently visible to the bot.
        /// </summary>
        /// <remarks>The <see langword="ulong"/> is the ID of the Discord guild.</remarks>
        ConcurrentDictionary<ulong, GuildConfigEntity> Guilds { get; }

        /// <summary>
        /// Contains all rotating playing statuses.
        /// </summary>
        List<PlayingStatusEntity> PlayingStatuses { get; }

        /// <summary>
        /// Contains all command aliases.
        /// </summary>
        /// <remarks>
        /// The <see langword="ulong"/> is the ID of the Discord guild if the alias is specific to a guild
        /// or zero if it is a global alias. <see cref="ConcurrentHashSet{T}"/> is a collection of aliases.
        /// </remarks>
        ConcurrentDictionary<ulong, ConcurrentHashSet<AliasEntity>> Aliases { get; }

        /// <summary>
        /// Contains all active word filters.
        /// </summary>
        /// <remarks>The <see langword="ulong"/> is the ID of the Discord guild.</remarks>
        ConcurrentDictionary<ulong, FilteredWordsEntity> FilteredWords { get; }

        /// <summary>
        /// Contains all active content filters.
        /// </summary>
        /// <remarks>
        /// The <see langword="ulong"/> is the ID of the Discord guild, the <see cref="ConcurrentHashSet{T}"/>
        /// is the collection of content filters of the guild.
        /// </remarks>
        ConcurrentDictionary<ulong, ConcurrentHashSet<FilteredContentEntity>> FilteredContent { get; }

        /// <summary>
        /// Manages creation, execution and removal of <see cref="IAkkoTimer"/>s.
        /// </summary>
        /// <remarks>This property is lazily initialized on startup.</remarks>
        ITimerManager Timers { get; set; }

        /// <summary>
        /// Contains commands that have been globally disabled.
        /// </summary>
        /// <remarks>
        /// The <see langword="string"/> is the command's qualified name.
        /// This property is lazily initialized on startup.
        /// </remarks>
        ConcurrentDictionary<string, Command> DisabledCommandCache { get; set; }

        /// <summary>
        /// Manages command cooldown.
        /// </summary>
        ICommandCooldown CooldownCommands { get; }

        /// <summary>
        /// Contains all active polls.
        /// </summary>
        /// <remarks>The <see langword="ulong"/> is the ID of the Discord guild, the <see cref="ConcurrentHashSet{T}"/> is the collection of polls of the guild.</remarks>
        ConcurrentDictionary<ulong, ConcurrentHashSet<PollEntity>> Polls { get; }

        /// <summary>
        /// Stores all repeaters with an interval lower than 1 day.
        /// </summary>
        /// <remarks>
        /// The <see langword="ulong"/> is the ID of the Discord guild, the <see cref="ConcurrentHashSet{T}"/> is the collection of repeaters of the guild.
        /// Long-running repeaters are not cached.
        /// </remarks>
        ConcurrentDictionary<ulong, ConcurrentHashSet<RepeaterEntity>> Repeaters { get; }

        /// <summary>
        /// Stores all voice roles.
        /// </summary>
        /// <remarks>The <see langword="ulong"/> is the ID of the Discord guild, the <see cref="ConcurrentHashSet{T}"/> is the collection of voice roles of the guild.</remarks>
        public ConcurrentDictionary<ulong, ConcurrentHashSet<VoiceRoleEntity>> VoiceRoles { get; }

        /// <summary>
        /// Stores all guild gatekeeping settings.
        /// </summary>
        /// <remarks>The <see langword="ulong"/> is the ID of the Discord guild.</remarks>
        public ConcurrentDictionary<ulong, GatekeepEntity> Gatekeeping { get; }

        /// <summary>
        /// Safely gets a database guild.
        /// </summary>
        /// <param name="sid">The GuildId of the database entry.</param>
        /// <remarks>If the entry doesn't exist, it creates one.</remarks>
        /// <returns>The specified <see cref="GuildConfigEntity"/>.</returns>
        ValueTask<GuildConfigEntity> GetDbGuildAsync(ulong sid);

        /// <summary>
        /// Adds a database guild and pertinent navigation properties to the cache.
        /// </summary>
        /// <param name="dbGuild">The database entry.</param>
        /// <returns><see langword="true"/> if the database guild was successfully cached, <see langword="false"/> otherwise.</returns>
        public bool TryAddDbGuild(GuildConfigEntity dbGuild);

        /// <summary>
        /// Removes a database guild and pertinent navigation properties from the cache.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <returns><see langword="true"/> if the database guild was successfully removed, <see langword="false"/> otherwise.</returns>
        public bool TryRemoveDbGuild(ulong sid);
    }
}