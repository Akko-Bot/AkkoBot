using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using LinqToDB;
using System;
using System.Linq;

namespace AkkoCore.Services.Events
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
        private readonly IGatekeepEventHandler _gatekeeper;
        private readonly IGuildLogEventHandler _guildLogger;
        private readonly DiscordShardedClient _shardedClient;

        public DiscordEventManager(IVoiceRoleConnectionHandler vcRoleHandler, IStartupEventHandler startup, IGuildLoadHandler guildLoader, IGuildEventsHandler guildEventsHandler,
            IGlobalEventsHandler globalEventsHandler, ICommandLogHandler cmdLogHandler, IGatekeepEventHandler gatekeeper, IGuildLogEventHandler guildLogger, DiscordShardedClient shardedClient)
        {
            _startup = startup;
            _voiceRoleHandler = vcRoleHandler;
            _guildLoader = guildLoader;
            _guildEventsHandler = guildEventsHandler;
            _globalEventsHandler = globalEventsHandler;
            _cmdLogHandler = cmdLogHandler;
            _gatekeeper = gatekeeper;
            _guildLogger = guildLogger;
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
            _shardedClient.GuildDownloadCompleted += _startup.CacheActiveGuildsAsync;

            // Executes startup commands
            _shardedClient.GuildDownloadCompleted += _startup.ExecuteStartupCommandsAsync;
        }

        /// <summary>
        /// Registers events the bot should listen to in order to react to specific user actions.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        public void RegisterEvents()
        {
            #region Log Events

            _shardedClient.MessageCreated += _guildLogger.CacheMessageOnCreationAsync;

            _shardedClient.MessageUpdated += _guildLogger.LogUpdatedMessageAsync;

            _shardedClient.MessageDeleted += _guildLogger.LogDeletedMessageAsync;

            _shardedClient.MessagesBulkDeleted += _guildLogger.LogBulkDeletedMessagesAsync;

            _shardedClient.GuildEmojisUpdated += _guildLogger.LogEmojiUpdateAsync;

            _shardedClient.InviteCreated += _guildLogger.LogCreatedInviteAsync;

            _shardedClient.InviteDeleted += _guildLogger.LogDeletedInviteAsync;

            _shardedClient.GuildBanAdded += _guildLogger.LogBannedUserAsync;

            _shardedClient.GuildBanRemoved += _guildLogger.LogUnbannedUserAsync;

            _shardedClient.GuildRoleCreated += _guildLogger.LogCreatedRoleAsync;

            _shardedClient.GuildRoleDeleted += _guildLogger.LogDeletedRoleAsync;

            _shardedClient.GuildRoleUpdated += _guildLogger.LogEditedRoleAsync;

            _shardedClient.ChannelCreated += _guildLogger.LogCreatedChannelAsync;

            _shardedClient.ChannelDeleted += _guildLogger.LogDeletedChannelAsync;

            _shardedClient.ChannelUpdated += _guildLogger.LogEditedChannelAsync;

            _shardedClient.VoiceStateUpdated += _guildLogger.LogVoiceStateAsync;

            _shardedClient.GuildMemberAdded += _guildLogger.LogJoiningMemberAsync;

            _shardedClient.GuildMemberRemoved += _guildLogger.LogLeavingMemberAsync;

            #endregion Log Events

            #region Bot Events

            // Save guild on join
            _shardedClient.GuildCreated += _guildLoader.AddGuildOnJoinAsync;

            // Decache guild on leave
            _shardedClient.GuildDeleted += _guildLoader.RemoveGuildOnLeaveAsync;

            // Punish alts
            _shardedClient.GuildMemberAdded += _gatekeeper.PunishAltAsync;

            // Sanitize username on join
            _shardedClient.GuildMemberAdded += _gatekeeper.SanitizeNameOnJoinAsync;

            // Sanitize nickname on update
            _shardedClient.GuildMemberUpdated += _gatekeeper.SanitizeNameOnUpdateAsync;

            // Send farewell message
            _shardedClient.GuildMemberRemoved += _gatekeeper.SendFarewellMessageAsync;

            // Send dm greet message
            _shardedClient.GuildMemberAdded += _gatekeeper.SendGreetDmMessageAsync;

            // Send greet message
            _shardedClient.GuildMemberAdded += _gatekeeper.SendGreetMessageAsync;

            // Prevent mute evasion
            _shardedClient.GuildMemberAdded += _guildEventsHandler.RemuteAsync;

            // Add join roles
            _shardedClient.GuildMemberAdded += _guildEventsHandler.AddJoinRolesAsync;

            // Prevent events from running for blacklisted users, channels and servers
            _shardedClient.MessageCreated += _globalEventsHandler.BlockBlacklistedAsync;

            // Voting for anonymous polls in the context channel
            _shardedClient.MessageCreated += _guildEventsHandler.PollVoteAsync;

            // Show prefix regardless of current config
            _shardedClient.MessageCreated += _globalEventsHandler.DefaultPrefixAsync;

            // Catch aliased commands and execute them
            _shardedClient.MessageCreated += _globalEventsHandler.HandleCommandAliasAsync;

            // Enables and disables channel slow mode
            _shardedClient.MessageCreated += _guildEventsHandler.AutoSlowmodeAsync;

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

            #endregion Bot Events

            #region Command Handler Events

            foreach (var cmdHandler in _shardedClient.ShardClients.Values.Select(x => x.GetCommandsNext()))
            {
                // Command handler events
                cmdHandler.CommandExecuted += _cmdLogHandler.LogCmdExecutionAsync;
                cmdHandler.CommandExecuted += _guildEventsHandler.DeleteCommandOnMessageAsync;
                cmdHandler.CommandErrored += _cmdLogHandler.LogCmdErrorAsync;
            }

            #endregion Command Handler Events
        }

        public void UnregisterStartupEvents() => throw new NotImplementedException();

        public void UnregisterEvents() => throw new NotImplementedException();
    }
}