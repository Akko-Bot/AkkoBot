using AkkoBot.Commands.Common;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Queries;
using AkkoBot.Services.Localization.Abstractions;
using AkkoBot.Services.Timers.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Services.Timers
{
    /// <summary>
    /// Defines the actions to be performed when a timer triggers.
    /// </summary>
    public class TimerActions : ITimerActions
    {
        private readonly EventId _timerLogEvent = new(99, nameof(TimerActions));
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;
        private readonly ILocalizer _localizer;
        private readonly ILogger _logger;
        private readonly DiscordShardedClient _shardedClient;
        private readonly UtilitiesService _utilitiesService;

        public TimerActions(IServiceScopeFactory scopeFactory, IDbCache dbCache, ILocalizer localizer, DiscordShardedClient shardedClient, UtilitiesService utilitiesService)
        {
            _scopeFactory = scopeFactory;
            _dbCache = dbCache;
            _localizer = localizer;
            _shardedClient = shardedClient;
            _logger = shardedClient.Logger;
            _utilitiesService = utilitiesService;
        }

        public async Task UnbanAsync(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await _dbCache.GetDbGuildAsync(server.Id);
            var localizedReason = _localizer.GetResponseString(dbGuild.Locale, "timedban_title");

            try
            {
                // Unban the user - they might have been unbanned in the meantime
                if ((await server.GetBansAsync()).FirstOrDefault(x => x.User.Id == userId) is not null)
                    await server.UnbanMemberAsync(userId, localizedReason);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(_timerLogEvent, $"An error occurred when running a timed unban. [{ex.Message}]");
            }
            finally
            {
                // Remove the entry
                await db.Timers.DeleteAsync(x => x.Id == entryId);
            }
        }

        public async Task UnmuteAsync(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await _dbCache.GetDbGuildAsync(server.Id);
            var localizedReason = _localizer.GetResponseString(dbGuild.Locale, "timedmute");

            try
            {
                // User may not be in the guild when this method runs
                // Or role may not exist anymore
                // Or bot may not have role permissions anymore
                server.Roles.TryGetValue(dbGuild.MuteRoleId ?? 0, out var muteRole);
                var user = await server.GetMemberAsync(userId);

                if (user.VoiceState is not null)
                    await user.SetMuteAsync(false);

                if (muteRole is not null)
                    await user.RevokeRoleAsync(muteRole, localizedReason);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(_timerLogEvent, $"An error occurred when running a timed unmute. [{ex.Message}]");
            }
            finally
            {
                // Remove the entries from the database
                await db.Timers.DeleteAsync(x => x.Id == entryId);
                await db.MutedUsers.DeleteAsync(x => x.UserId == userId);
            }
        }

        public async Task AddPunishRoleAsync(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await _dbCache.GetDbGuildAsync(server.Id);
            var timerEntry = await db.Timers
                .AsNoTracking()
                .Select(x => new TimerEntity() { Id = x.Id, RoleId = x.RoleId })
                .FirstOrDefaultAsyncEF(x => x.Id == entryId);

            try
            {
                var localizedReason = _localizer.GetResponseString(dbGuild.Locale, "timedrole");
                server.Roles.TryGetValue(timerEntry.RoleId.Value, out var punishRole);
                var user = await server.GetMemberAsync(userId);

                await user.GrantRoleAsync(punishRole, localizedReason);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(_timerLogEvent, $"An error occurred when adding a punishment role. [{ex.Message}]");
            }
            finally
            {
                await db.Timers.DeleteAsync(x => x.Id == entryId);
            }
        }

        public async Task RemovePunishRoleAsync(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await _dbCache.GetDbGuildAsync(server.Id);
            var timerEntry = await db.Timers
                .AsNoTracking()
                .Select(x => new TimerEntity() { Id = x.Id, RoleId = x.RoleId })
                .FirstOrDefaultAsyncEF(x => x.Id == entryId);

            try
            {
                var localizedReason = _localizer.GetResponseString(dbGuild.Locale, "timedunrole");
                server.Roles.TryGetValue(timerEntry.RoleId.Value, out var punishRole);
                var user = await server.GetMemberAsync(userId);

                await user.RevokeRoleAsync(punishRole, localizedReason);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(_timerLogEvent, $"An error occurred when removing a punishment role. [{ex.Message}]");
            }
            finally
            {
                await db.Timers.DeleteAsync(x => x.Id == entryId);
            }
        }

        public async Task RemoveOldWarningAsync(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await _dbCache.GetDbGuildAsync(server.Id);
            var warnings = await db.Warnings
                .Fetch(x => x.GuildIdFK == server.Id && x.UserIdFK == userId)
                .Select(x => new WarnEntity() { Id = x.Id, DateAdded = x.DateAdded })
                .ToListAsyncEF();

            // Don't delete warnings that are not old enough
            warnings.RemoveAll(x => x.DateAdded.Add(dbGuild.WarnExpire).Subtract(DateTimeOffset.Now) > TimeSpan.Zero);

            await db.Warnings.DeleteAsync(x => warnings.Select(x => x.Id).Contains(x.Id));
            await db.Timers.DeleteAsync(x => x.Id == entryId);
        }

        public async Task SendReminderAsync(int entryId, DiscordClient client, DiscordGuild server)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var dbReminder = await db.Reminders.FirstOrDefaultAsyncEF(x => x.TimerIdFK == entryId);
            var cmdHandler = client.GetCommandsNext();

            try
            {
                _dbCache.Guilds.TryGetValue(dbReminder.GuildId ?? default, out var dbGuild);
                var user = FindMember(dbReminder.AuthorId, server);

                var channel = (dbReminder.IsPrivate)
                        ? await user.CreateDmChannelAsync()
                        : server.GetChannel(dbReminder.ChannelId);

                if (server is not null && !HasPermissionTo(server.CurrentMember, channel, Permissions.SendMessages))
                    return;

                var fakeContext = cmdHandler.CreateFakeContext(
                    user,
                    user.Guild.Channels.Values.FirstOrDefault(), // If channel is private, this throws
                    dbReminder.Content,
                    (dbReminder.IsPrivate) ? _dbCache.BotConfig.BotPrefix : dbGuild.Prefix,
                    null
                );

                var message = new SmartString(fakeContext, dbReminder.Content, true);
                var wasDeserialized = _utilitiesService.DeserializeEmbed(message, out var dmsg);
                dmsg ??= new();

                var localizedDate = (server is null)
                    ? dbReminder.DateAdded.ToString("D", CultureInfo.CreateSpecificCulture(_dbCache.BotConfig.Locale))
                    : dbReminder.DateAdded.ToString("D", CultureInfo.CreateSpecificCulture(dbGuild.Locale));

                var header = $"⏰ {Formatter.Bold(user.GetFullname())} - {localizedDate}\n";

                dmsg.Content = (dmsg.Content is null)
                    ? (header + ((wasDeserialized) ? string.Empty : message)).MaxLength(AkkoConstants.MaxMessageLength, "[...]")
                    : dmsg.Content.Insert(0, header).MaxLength(AkkoConstants.MaxMessageLength, "[...]");

                await channel.SendMessageAsync(dmsg);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(_timerLogEvent, $"An error occurred when trying to send a reminder. [User: {dbReminder.AuthorId}] [Server: {dbReminder.GuildId}] [{ex.Message}]");
            }
            finally
            {
                await db.Reminders.DeleteAsync(dbReminder);
                await db.Timers.DeleteAsync(x => x.Id == entryId);
            }
        }

        public async Task ExecuteCommandAsync(int entryId, DiscordClient client, DiscordGuild server)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var cmdHandler = client.GetCommandsNext();
            var dbCmd = await db.AutoCommands
                .AsNoTracking()
                .FirstOrDefaultAsyncEF(x => x.TimerIdFK == entryId);

            try
            {
                var cmd = cmdHandler.FindCommand(dbCmd.CommandString, out var args);
                var user = FindMember(dbCmd.AuthorId, server);

                var fakeContext = cmdHandler.CreateFakeContext(
                    user,
                    server.GetChannel(dbCmd.ChannelId), // If channel is private, this throws
                    dbCmd.CommandString,
                    (await _dbCache.GetDbGuildAsync(dbCmd.GuildId)).Prefix,
                    cmd,
                    args
                );

                if (!(await cmd.RunChecksAsync(fakeContext, false)).Any())
                    await cmd.ExecuteAsync(fakeContext);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(_timerLogEvent, $"An error occurred when trying to run an autocommand. [User: {dbCmd.AuthorId}] [Command: {dbCmd.CommandString}] [{ex.Message}]");
            }
            finally
            {
                if (dbCmd.Type is AutoCommandType.Scheduled)
                {
                    await db.AutoCommands.DeleteAsync(dbCmd);
                    await db.Timers.DeleteAsync(x => x.Id == entryId);
                }
            }
        }

        public async Task SendRepeaterAsync(int entryId, DiscordClient client, DiscordGuild server)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            _dbCache.Repeaters.TryGetValue(server.Id, out var repeaterCache);
            var cmdHandler = client.GetCommandsNext();
            var dbRepeater = repeaterCache?.FirstOrDefault(x => x.TimerIdFK == entryId)
                ?? await db.Repeaters.AsNoTracking().FirstOrDefaultAsyncEF(x => x.TimerIdFK == entryId);

            try
            {
                var dbGuild = await _dbCache.GetDbGuildAsync(dbRepeater.GuildIdFK);
                var user = FindMember(dbRepeater.AuthorId, server);
                var channel = server.GetChannel(dbRepeater.ChannelId);

                if (!HasPermissionTo(server.CurrentMember, channel, Permissions.SendMessages))
                    return;

                var lastMessage = await channel.GetLatestMessageAsync(client);
                var fakeContext = cmdHandler.CreateFakeContext(user, channel, dbRepeater.Content, dbGuild.Prefix, null);

                var message = new SmartString(fakeContext, dbRepeater.Content);
                var wasDeserialized = _utilitiesService.DeserializeEmbed(message, out var dmsg);

                // If last message is the same repeated message, do nothing
                if ((lastMessage.Author == server.CurrentMember && wasDeserialized && lastMessage.Content == dmsg.Content && lastMessage.Embeds[0] == dmsg.Embed)
                    || (!wasDeserialized && lastMessage.Content == message))
                    return;

                // Send the repeater
                var discordMessage = (wasDeserialized)
                    ? await channel.SendMessageAsync(dmsg)
                    : await channel.SendMessageAsync(message);

                if (HasPermissionTo(server.CurrentMember, channel, Permissions.AddReactions))
                    await discordMessage.CreateReactionAsync(AkkoEntities.RepeaterEmoji);
            }
            catch (Exception ex)
            {
                // If an error occurs, remove the repeater
                if (dbRepeater is not null)
                    await db.Repeaters.DeleteAsync(dbRepeater);

                await db.Timers.DeleteAsync(x => x.Id == entryId);

                _dbCache.Timers.TryRemove(entryId);

                _logger.LogWarning(_timerLogEvent, $"An error occurred when trying to run an autocommand. [User: {dbRepeater?.AuthorId}] [Server: {dbRepeater?.GuildIdFK}] [{ex.Message}]");
            }
        }

        /* Utility Methods */

        /// <summary>
        /// Checks if the specified user has permission to perform an action.
        /// </summary>
        /// <param name="user">The Discord user.</param>
        /// <param name="channel">The Discord channel to check the permissions for.</param>
        /// <param name="permissions">The permissions the user must have.</param>
        /// <returns><see langword="true"/> if the user has permission, <see langword="false"/> otherwise.</returns>
        private bool HasPermissionTo(DiscordMember user, DiscordChannel channel, Permissions permissions)
            => channel.IsPrivate || user.PermissionsIn(channel).HasOneFlag(Permissions.Administrator | permissions);

        /// <summary>
        /// Finds a <see cref="DiscordMember"/> with the specified ID.
        /// </summary>
        /// <param name="uid">The user ID.</param>
        /// <param name="server">The Discord server the user is possibly in.</param>
        /// <remarks>
        /// Specify a <see cref="DiscordGuild"/> to potentially get the result faster.
        /// If the user is not found in it, the search expands to all guilds the bot is in.
        /// </remarks>
        /// <returns>The <see cref="DiscordMember"/>, <see langword="null"/> if the user doesn't share any server with the bot.</returns>
        private DiscordMember FindMember(ulong uid, DiscordGuild server = null)
        {
            if (server is not null && server.Members.TryGetValue(uid, out var member))
                return member;

            foreach (var client in _shardedClient.ShardClients.Values)
            {
                server = client.Guilds.Values.FirstOrDefault(x => x.Members.Any(x => x.Key == uid));

                if (server is not null && server.Members.TryGetValue(uid, out member))
                    return member;
            }

            return null;
        }
    }
}