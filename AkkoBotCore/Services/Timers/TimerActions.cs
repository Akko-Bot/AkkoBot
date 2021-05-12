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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceProvider _services;
        private readonly IDbCache _dbCache;
        private readonly ILocalizer _localizer;

        public TimerActions(IServiceProvider services, IDbCache dbCache, ILocalizer localizer)
        {
            _services = services;
            _dbCache = dbCache;
            _localizer = localizer;
        }

        public async Task UnbanAsync(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await _dbCache.GetDbGuildAsync(server.Id);
            var localizedReason = _localizer.GetResponseString(dbGuild.Locale, "timedban_title");

            // Unban the user - they might have been unbanned in the meantime
            if ((await server.GetBansAsync()).FirstOrDefault(x => x.User.Id == userId) is not null)
                await server.UnbanMemberAsync(userId, localizedReason);

            // Remove the entry
            var dbEntity = await db.Timers.FindAsync(entryId);
            db.Remove(dbEntity);

            await db.SaveChangesAsync();
        }

        public async Task UnmuteAsync(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await db.GuildConfig.GetGuildWithMutesAsync(server.Id);
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
            catch
            {
                return;
            }
            finally
            {
                // Remove the entries from the database
                var timerEntry = await db.Timers.FindAsync(entryId);
                var muteEntry = dbGuild.MutedUserRel.FirstOrDefault(x => x.UserId == userId);

                db.Remove(timerEntry);
                db.Remove(muteEntry);

                await db.SaveChangesAsync();
            }
        }

        public async Task AddPunishRoleAsync(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await _dbCache.GetDbGuildAsync(server.Id);
            var timerEntry = await db.Timers.FindAsync(entryId);

            try
            {
                var localizedReason = _localizer.GetResponseString(dbGuild.Locale, "timedrole");
                server.Roles.TryGetValue(timerEntry.RoleId.Value, out var punishRole);
                var user = await server.GetMemberAsync(userId);

                await user.GrantRoleAsync(punishRole, localizedReason);
            }
            catch
            {
                return;
            }
            finally
            {
                db.Remove(timerEntry);
                await db.SaveChangesAsync();
            }
        }

        public async Task RemovePunishRoleAsync(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await _dbCache.GetDbGuildAsync(server.Id);
            var timerEntry = await db.Timers.FindAsync(entryId);

            try
            {
                var localizedReason = _localizer.GetResponseString(dbGuild.Locale, "timedunrole");
                server.Roles.TryGetValue(timerEntry.RoleId.Value, out var punishRole);
                var user = await server.GetMemberAsync(userId);

                await user.RevokeRoleAsync(punishRole, localizedReason);
            }
            catch
            {
                return;
            }
            finally
            {
                db.Remove(timerEntry);
                await db.SaveChangesAsync();
            }
        }

        public async Task RemoveOldWarningAsync(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var timer = await db.Timers.FindAsync(entryId);
            var guildSettings = await db.GuildConfig
                .Include(x => x.WarnRel.Where(x => x.UserId == userId))
                .FirstOrDefaultAsync(x => x.GuildId == server.Id);

            guildSettings.WarnRel.RemoveAll(x => x.DateAdded.Add(guildSettings.WarnExpire).Subtract(DateTimeOffset.Now) <= TimeSpan.Zero);

            // Update the entries
            db.Update(guildSettings);
            db.Remove(timer);

            await db.SaveChangesAsync();
        }

        public async Task SendReminderAsync(int entryId, DiscordClient client, DiscordGuild server)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbReminder = await db.Reminders.FirstOrDefaultAsync(x => x.TimerId == entryId);
            var dbTimer = await db.Timers.FindAsync(entryId);
            var cmdHandler = client.GetCommandsNext();

            try
            {
                var dbGuild = await _dbCache.GetDbGuildAsync(dbReminder.GuildId.Value);
                var user = FindMember(dbReminder.AuthorId, server);

                var channel = (dbReminder.IsPrivate)
                        ? await user.CreateDmChannelAsync()
                        : server.GetChannel(dbReminder.ChannelId);

                if (!HasPermissionTo(server.CurrentMember, channel, Permissions.SendMessages))
                    return;

                var fakeContext = cmdHandler.CreateFakeContext(
                    user,
                    user.Guild.Channels.Values.FirstOrDefault(), // If channel is private, this throws
                    dbReminder.Content,
                    (dbReminder.IsPrivate) ? _dbCache.BotConfig.BotPrefix : dbGuild.Prefix,
                    null
                );

                var message = new SmartString(fakeContext, dbReminder.Content);
                var wasDeserialized = _services.GetService<UtilitiesService>().DeserializeEmbed(message.Content, out var dmsg);
                dmsg ??= new();

                var localizedDate = (server is null)
                    ? dbReminder.DateAdded.ToString("D", CultureInfo.CreateSpecificCulture(_dbCache.BotConfig.Locale))
                    : dbReminder.DateAdded.ToString("D", CultureInfo.CreateSpecificCulture(dbGuild.Locale));

                var header = $"⏰ {Formatter.Bold(user.GetFullname())} - {localizedDate}\n";

                dmsg.Content = (dmsg.Content is null)
                    ? (header + ((wasDeserialized) ? string.Empty : message.Content)).MaxLength(AkkoConstants.MessageMaxLength, "[...]")
                    : dmsg.Content.Insert(0, header).MaxLength(AkkoConstants.MessageMaxLength, "[...]");

                await channel.SendMessageAsync(dmsg);
            }
            catch
            {
                return;
            }
            finally
            {
                db.Remove(dbReminder);
                db.Remove(dbTimer);

                await db.SaveChangesAsync();
            }
        }

        public async Task ExecuteCommandAsync(int entryId, DiscordClient client, DiscordGuild server)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbCmd = await db.AutoCommands.FirstOrDefaultAsync(x => x.TimerId == entryId);
            var dbTimer = await db.FindAsync<TimerEntity>(entryId);
            var cmdHandler = client.GetCommandsNext();

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
            catch
            {
                return;
            }
            finally
            {
                if (dbCmd.Type is AutoCommandType.Scheduled)
                {
                    db.Remove(dbCmd);
                    db.Remove(dbTimer);

                    await db.SaveChangesAsync();
                }
            }
        }

        public async Task SendRepeaterAsync(int entryId, DiscordClient client, DiscordGuild server)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            _dbCache.Repeaters.TryGetValue(server.Id, out var repeaterCache);
            var cmdHandler = client.GetCommandsNext();
            var dbRepeater = repeaterCache.FirstOrDefault(x => x.TimerId == entryId)
                ?? await db.Repeaters.Fetch(x => x.TimerId == entryId).FirstOrDefaultAsync();

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
                var wasDeserialized = _services.GetService<UtilitiesService>().DeserializeEmbed(message.Content, out var dmsg);

                // If last message is the same repeated message, do nothing
                if (lastMessage.Author == server.CurrentMember
                    && (wasDeserialized && lastMessage.Content == dmsg.Content && lastMessage.Embeds[0] == dmsg.Embed)
                    || (!wasDeserialized && lastMessage.Content == message.Content))
                    return;

                // Send the repeater
                var discordMessage = (wasDeserialized)
                    ? await channel.SendMessageAsync(dmsg)
                    : await channel.SendMessageAsync(message.Content);

                if (HasPermissionTo(server.CurrentMember, channel, Permissions.AddReactions))
                    await discordMessage.CreateReactionAsync(AkkoEntities.RepeaterEmoji);
            }
            catch
            {
                // If an error occurs, remove the repeater
                var dbTimer = await db.Timers.FindAsync(entryId);

                db.Remove(dbRepeater);
                db.Remove(dbTimer);

                await db.SaveChangesAsync();

                _dbCache.Timers.TryRemove(dbTimer.Id);
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

            var clients = _services.GetService<DiscordShardedClient>();

            foreach (var client in clients.ShardClients.Values)
            {
                server = client.Guilds.Values.FirstOrDefault(x => x.Members.Any(x => x.Key == uid));

                if (server is not null && server.Members.TryGetValue(uid, out member))
                    return member;
            }

            return null;
        }
    }
}