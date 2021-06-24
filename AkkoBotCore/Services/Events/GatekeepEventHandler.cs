﻿using AkkoBot.Commands.Common;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Events.Abstractions;
using ConcurrentCollections;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Services.Events
{
    /// <summary>
    /// Handles events related to gatekeeping.
    /// </summary>
    internal class GatekeepEventHandler : IGatekeepingEventHandler, IDisposable
    {
        private readonly ConcurrentHashSet<ulong> _waitingGreets = new();
        private readonly ConcurrentHashSet<ulong> _waitingFarewells = new();

        private readonly IDbCache _dbCache;
        private readonly IMemberAggregator _greetAggregator;
        private readonly IMemberAggregator _farewellAggregator;
        private readonly UtilitiesService _utilitiesService;

        public GatekeepEventHandler(IDbCache dbCache, IMemberAggregator greetAggregator, IMemberAggregator farewellAggregator, UtilitiesService utilitiesService)
        {
            _dbCache = dbCache;
            _greetAggregator = greetAggregator;
            _farewellAggregator = farewellAggregator;
            _utilitiesService = utilitiesService;
        }

        public async Task SanitizeNameOnUpdateAsync(DiscordClient _, GuildMemberUpdateEventArgs eventArgs)
        {
            if (!_dbCache.Gatekeeping.TryGetValue(eventArgs.Guild.Id, out var gatekeeper) || !gatekeeper.SanitizeNames
                || (!char.IsPunctuation(eventArgs.Member.DisplayName[0]) && !char.IsSymbol(eventArgs.Member.DisplayName[0]))
                || !eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.ManageNicknames)))
                return;

            await eventArgs.Member.ModifyAsync(user =>
                    user.Nickname = (string.IsNullOrWhiteSpace(gatekeeper.CustomSanitizedName))
                        ? EnsureNameSanitization(eventArgs.Member)
                        : gatekeeper.CustomSanitizedName
            );
        }

        public async Task SanitizeNameOnJoinAsync(DiscordClient _, GuildMemberAddEventArgs eventArgs)
        {
            var dbGuild = await _dbCache.GetDbGuildAsync(eventArgs.Guild.Id);

            if (!_dbCache.Gatekeeping.TryGetValue(eventArgs.Guild.Id, out var gatekeeper) || !gatekeeper.SanitizeNames
                || (!char.IsPunctuation(eventArgs.Member.DisplayName[0]) && !char.IsSymbol(eventArgs.Member.DisplayName[0]))
                || !eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.ManageNicknames)))
                return;

            await eventArgs.Member.ModifyAsync(user =>
                    user.Nickname = (string.IsNullOrWhiteSpace(gatekeeper.CustomSanitizedName))
                        ? EnsureNameSanitization(eventArgs.Member)
                        : gatekeeper.CustomSanitizedName
            );
        }

        public async Task SendGreetDmMessageAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs)
        {
            if (!_dbCache.Gatekeeping.TryGetValue(eventArgs.Guild.Id, out var gatekeeper)
                || !gatekeeper.GreetDm || eventArgs.Member.IsBot || string.IsNullOrWhiteSpace(gatekeeper.GreetMessage))
                return;

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);
            eventArgs.Guild.Channels.TryGetValue(gatekeeper.GreetChannelId ?? default, out var channel);

            channel ??= eventArgs.Guild.GetDefaultChannel();
            var cmdHandler = client.GetCommandsNext();
            var fakeContext = cmdHandler.CreateFakeContext(eventArgs.Member, channel, gatekeeper.GreetMessage, dbGuild.Prefix, null);
            var parsedString = new SmartString(fakeContext, gatekeeper.GreetMessage);

            if (_utilitiesService.DeserializeEmbed(parsedString, out var message))
                await eventArgs.Member.SendMessageSafelyAsync(message);
            else
                await eventArgs.Member.SendMessageSafelyAsync(parsedString);
        }

        public Task SendGreetMessageAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs)
        {
            if (!_dbCache.Gatekeeping.TryGetValue(eventArgs.Guild.Id, out var gatekeeper)
                || !gatekeeper.GreetChannelId.HasValue || gatekeeper.GreetDm //|| eventArgs.Member.IsBot
                || string.IsNullOrWhiteSpace(gatekeeper.GreetMessage)
                || !eventArgs.Guild.Channels.TryGetValue(gatekeeper.GreetChannelId.Value, out var channel)
                || !eventArgs.Guild.CurrentMember.PermissionsIn(channel).HasPermission(Permissions.AccessChannels | Permissions.SendMessages))
                return Task.CompletedTask;

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var cmdHandler = client.GetCommandsNext();
            var fakeContext = cmdHandler.CreateFakeContext(eventArgs.Member, channel, gatekeeper.GreetMessage, dbGuild.Prefix, null);

            _ = SendGatekeepMessageAsync(
                fakeContext, eventArgs.Member, channel, gatekeeper.GreetDeleteTime,
                _dbCache.BotConfig.BulkGatekeepTime, _greetAggregator, _waitingGreets, gatekeeper.GreetMessage
            );

            return Task.CompletedTask;
        }

        public Task SendFarewellMessageAsync(DiscordClient client, GuildMemberRemoveEventArgs eventArgs)
        {
            if (!_dbCache.Gatekeeping.TryGetValue(eventArgs.Guild.Id, out var gatekeeper)
                || !gatekeeper.FarewellChannelId.HasValue //|| eventArgs.Member.IsBot
                || string.IsNullOrWhiteSpace(gatekeeper.FarewellMessage)
                || !eventArgs.Guild.Channels.TryGetValue(gatekeeper.FarewellChannelId.Value, out var channel)
                || !eventArgs.Guild.CurrentMember.PermissionsIn(channel).HasPermission(Permissions.AccessChannels | Permissions.SendMessages))
                return Task.CompletedTask;

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var cmdHandler = client.GetCommandsNext();
            var fakeContext = cmdHandler.CreateFakeContext(eventArgs.Member, channel, gatekeeper.FarewellMessage, dbGuild.Prefix, null);

            _ = SendGatekeepMessageAsync(
                fakeContext, eventArgs.Member, channel, gatekeeper.FarewellDeleteTime,
                _dbCache.BotConfig.BulkGatekeepTime, _farewellAggregator, _waitingFarewells, gatekeeper.FarewellMessage
            );

            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns a valid display name for a user.
        /// </summary>
        /// <param name="user">The user to have its name sanitized.</param>
        /// <returns>
        /// The sanitized display name or "No Symbols Allowed" if their nickname AND username
        /// are comprised of special characters only.
        /// </returns>
        private string EnsureNameSanitization(DiscordMember user)
        {
            var result = user.DisplayName.SanitizeUsername();

            return (string.IsNullOrWhiteSpace(result))
                ? (user.Username.All(x => char.IsPunctuation(x) || char.IsSymbol(x))) ? "No Symbols Allowed" : user.Username.SanitizeUsername()
                : result;
        }

        /// <summary>
        /// Sends a greeting or farewell Discord message according to the specified settings.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user that triggered the event.</param>
        /// <param name="channel">The Discord channel the message should be sent to.</param>
        /// <param name="deleteTime">How long the message should last before being deleted.</param>
        /// <param name="waitTime">How long the bot should wait before sending bulk messages.</param>
        /// <param name="aggregator">The member aggregator.</param>
        /// <param name="activeWaits">A collection of IDs of guilds that are on wait for a bulk message.</param>
        /// <param name="rawMessage">The message to be parsed.</param>
        private async Task SendGatekeepMessageAsync(CommandContext context, DiscordMember user, DiscordChannel channel, TimeSpan deleteTime,
            TimeSpan waitTime, IMemberAggregator aggregator, ConcurrentHashSet<ulong> activeWaits, string rawMessage)
        {
            // If there is a waiting list, but too early or too little to send a bulk message
            if (aggregator.Add(user) && !aggregator.SendsBulk(context.Guild.Id, waitTime))
            {
                // If first time, wait. Else, quit.
                if (activeWaits.Contains(context.Guild.Id))
                    return;
                else
                {
                    activeWaits.Add(context.Guild.Id);
                    await Task.Delay(waitTime).ConfigureAwait(false);
                }
            }

            var parsedString = aggregator.ParseMessage(context, rawMessage);
            activeWaits.TryRemove(context.Guild.Id);    // Stop collecting members of this guild, as the message has been already parsed.

            var discordMessage = (_utilitiesService.DeserializeEmbed(parsedString, out var message))
                ? await channel.SendMessageAsync(message)
                : await channel.SendMessageAsync(parsedString);

            if (deleteTime > TimeSpan.Zero)
                await discordMessage.DeleteWithDelayAsync(deleteTime);
        }

        public void Dispose()
        {
            _waitingGreets.Clear();
            _waitingFarewells.Clear();
            _greetAggregator.Dispose();
            _farewellAggregator.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}