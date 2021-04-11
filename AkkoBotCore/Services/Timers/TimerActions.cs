using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Common;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Queries;
using AkkoBot.Services.Localization.Abstractions;
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
    /// Encapsulates the set of timed actions a Discord user can create.
    /// </summary>
    public class TimerActions : ICommandService
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

        /// <summary>
        /// Unbans a user from a Discord server.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="server">The Discord server to unban from.</param>
        /// <param name="userId">The ID of the user to be unbanned.</param>
        public async Task UnbanAsync(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await _dbCache.GetGuildAsync(server.Id);
            var localizedReason = _localizer.GetResponseString(dbGuild.Locale, "timedban_title");

            // Unban the user - they might have been unbanned in the meantime
            if ((await server.GetBansAsync()).FirstOrDefault(x => x.User.Id == userId) is not null)
                await server.UnbanMemberAsync(userId, localizedReason);

            // Remove the entry
            var dbEntity = await db.Timers.FindAsync(entryId);
            db.Remove(dbEntity);

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Unmutes a user on a Discord server.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="server">The Discord server to unmute from.</param>
        /// <param name="userId">The ID of the user to be unmuted.</param>
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
                dbGuild.MutedUserRel.Remove(muteEntry);

                db.Remove(timerEntry);
                db.Update(dbGuild);

                await db.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Adds a role to a Discord user.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="server">The Discord server to unmute from.</param>
        /// <param name="userId">The ID of the user to be unmuted.</param>
        public async Task AddPunishRoleAsync(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await _dbCache.GetGuildAsync(server.Id);
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

        /// <summary>
        /// Removes a role from a Discord user.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="server">The Discord server to unmute from.</param>
        /// <param name="userId">The ID of the user to be unmuted.</param>
        public async Task RemovePunishRoleAsync(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await _dbCache.GetGuildAsync(server.Id);
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

        /// <summary>
        /// Removes old warnings from the specified user.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="server">The Discord server to unmute from.</param>
        /// <param name="userId">The ID of the user to be unmuted.</param>
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

        /// <summary>
        /// Sends a reminder to the channel specified in the database.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="client">The Discord client that created the reminder.</param>
        /// <param name="server">The Discord server to unmute from.</param>
        public async Task SendReminderAsync(int entryId, DiscordClient client, DiscordGuild server)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbReminder = await db.Reminders.FirstOrDefaultAsync(x => x.TimerId == entryId);
            var dbTimer = await db.Timers.FindAsync(entryId);
            var dbGuild = await _dbCache.GetGuildAsync(dbReminder.GuildId.Value);
            var cmdHandler = client.GetExtension<CommandsNextExtension>();

            try
            {
                var user = FindMember(dbReminder.AuthorId, server);

                var channel = (dbReminder.IsPrivate)
                        ? await user.CreateDmChannelAsync()
                        : server.GetChannel(dbReminder.ChannelId);

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

        /// <summary>
        /// Executes a command in the context stored in the database.
        /// </summary>
        /// <param name="entryId">The ID of the timer in the database.</param>
        /// <param name="client">The Discord client that created the autocommand.</param>
        /// <param name="server">The Discord server to unmute from.</param>
        public async Task ExecuteCommandAsync(int entryId, DiscordClient client, DiscordGuild server)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbCmd = await db.AutoCommands.FirstOrDefaultAsync(x => x.TimerId == entryId);
            var dbTimer = await db.FindAsync<TimerEntity>(entryId);
            var cmdHandler = client.GetExtension<CommandsNextExtension>();

            try
            {
                var cmd = cmdHandler.FindCommand(dbCmd.CommandString, out var args);
                var user = FindMember(dbCmd.AuthorId, server);

                var fakeContext = cmdHandler.CreateFakeContext(
                    user,
                    server.GetChannel(dbCmd.ChannelId), // If channel is private, this throws
                    dbCmd.CommandString,
                    (await _dbCache.GetGuildAsync(dbCmd.GuildId)).Prefix,
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
                if (dbCmd.Type is CommandType.Scheduled)
                {
                    db.Remove(dbCmd);
                    db.Remove(dbTimer);

                    await db.SaveChangesAsync();
                }
            }
        }

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