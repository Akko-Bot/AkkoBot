using AkkoBot.Core.Services.Abstractions;
using AkkoBot.Services.Events;
using AkkoBot.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using LinqToDB;
using System;
using System.Linq;

namespace AkkoBot.Core.Services
{
    /// <summary>
    /// Defines the behavior the bot should have for specific user actions.
    /// </summary>
    internal class DiscordEventManager : IDiscordEventManager
    {
        private readonly IStartupEventHandler _startup;
        private readonly IVoiceRoleConnectionHandler _voiceRoleHandler;
        private readonly IGuildLoadHandler _guildLoader;
        private readonly IGuildEventsHandler _guildEventsHandler;
        private readonly IGlobalEventsHandler _globalEventsHandler;
        private readonly ICommandLogHandler _cmdLogHandler;
        private readonly DiscordShardedClient _shardedClient;

        public DiscordEventManager(IVoiceRoleConnectionHandler vcRoleHandler, IStartupEventHandler startup, IGuildLoadHandler guildLoader, IGuildEventsHandler guildEventsHandler,
            IGlobalEventsHandler globalEventsHandler, ICommandLogHandler cmdLogHandler, DiscordShardedClient shardedClient)
        {
            _startup = startup;
            _voiceRoleHandler = vcRoleHandler;
            _guildLoader = guildLoader;
            _guildEventsHandler = guildEventsHandler;
            _globalEventsHandler = globalEventsHandler;
            _cmdLogHandler = cmdLogHandler;
            _shardedClient = shardedClient;
        }

        public void RegisterStartupEvents()
        {
            // Create bot configs on ready, if there isn't one already
            _shardedClient.Ready += _startup.LoadInitialStateAsync;

            // Unregisters all disabled commands
            _shardedClient.Ready += _startup.UnregisterCommandsAsync;

            // Initializes all timers
            _shardedClient.GuildDownloadCompleted += _startup.InitializeTimersAsync;

            // Save visible guilds on ready
            _shardedClient.GuildDownloadCompleted += _startup.SaveNewGuildsAsync;

            // Initialize the timers stored in the database
            _shardedClient.GuildDownloadCompleted += _startup.InitializeTimersAsync;

            // Caches guild filtered words
            _shardedClient.GuildDownloadCompleted += _startup.CacheFilteredElementsAsync;

            // Executes startup commands
            _shardedClient.GuildDownloadCompleted += _startup.ExecuteStartupCommandsAsync;
        }

        /// <summary>
        /// Registers events the bot should listen to in order to react to specific user actions.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void RegisterEvents()
        {
            // Save guild on join
            _shardedClient.GuildCreated += _guildLoader.AddGuildOnJoinAsync;

            // Decache guild on leave
            _shardedClient.GuildDeleted += _guildLoader.RemoveGuildOnLeaveAsync;

            // Sanitize username on join
            _shardedClient.GuildMemberAdded += _guildEventsHandler.SanitizeNameOnJoinAsync;

            // Sanitize nickname on update
            _shardedClient.GuildMemberUpdated += _guildEventsHandler.SanitizeNameOnUpdateAsync;

            // Prevent mute evasion
            _shardedClient.GuildMemberAdded += _guildEventsHandler.RemuteAsync;

            // Prevent events from running for blacklisted users, channels and servers
            _shardedClient.MessageCreated += _globalEventsHandler.BlockBlacklistedAsync;

            // Voting for anonymous polls in the context channel
            _shardedClient.MessageCreated += _guildEventsHandler.PollVoteAsync;

            // Show prefix regardless of current config
            _shardedClient.MessageCreated += _globalEventsHandler.DefaultPrefixAsync;

            // Catch aliased commands and execute them
            _shardedClient.MessageCreated += _globalEventsHandler.HandleCommandAliasAsync;

            // Delete messages with filtered words
            _shardedClient.MessageCreated += _guildEventsHandler.FilterWordAsync;

            // Delete messages with server invites
            _shardedClient.MessageCreated += _guildEventsHandler.FilterInviteAsync;

            // Delete messages with stickers
            _shardedClient.MessageCreated += _guildEventsHandler.FilterStickerAsync;

            // Deletes messages that don't contain a certain type of content
            _shardedClient.MessageCreated += _guildEventsHandler.FilterContentAsync;

            // Assign role on channel join/leave
            _shardedClient.VoiceStateUpdated += _voiceRoleHandler.VoiceRoleAsync;

            foreach (var cmdHandler in _shardedClient.ShardClients.Values.Select(x => x.GetCommandsNext()))
            {
                // Log command execution
                cmdHandler.CommandExecuted += _cmdLogHandler.LogCmdExecutionAsync;
                cmdHandler.CommandErrored += _cmdLogHandler.LogCmdErrorAsync;
            }
        }

        public void UnregisterStartupEvents() => throw new NotImplementedException();

        public void UnregisterEvents() => throw new NotImplementedException();
    }
}