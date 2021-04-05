using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Common;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
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
        private readonly ILocalizer _localizer;

        public TimerActions(IServiceProvider services, ILocalizer localizer)
        {
            _services = services;
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
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var settings = db.GuildConfig.GetGuild(server.Id);
            var localizedReason = _localizer.GetResponseString(settings.Locale, "timedban_title");

            // Unban the user - they might have been unbanned in the meantime
            if ((await server.GetBansAsync()).FirstOrDefault(x => x.User.Id == userId) is not null)
                await server.UnbanMemberAsync(userId, localizedReason);

            // Remove the entry
            var dbEntity = await db.Timers.GetAsync(entryId);
            db.Timers.Delete(dbEntity);

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
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var guildSettings = await db.GuildConfig.GetGuildWithMutesAsync(server.Id);
            var localizedReason = _localizer.GetResponseString(guildSettings.Locale, "timedmute");

            try
            {
                // User may not be in the guild when this method runs
                // Or role may not exist anymore
                // Or bot may not have role permissions anymore
                server.Roles.TryGetValue(guildSettings.MuteRoleId ?? 0, out var muteRole);
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
                var timerEntry = await db.Timers.GetAsync(entryId);
                var muteEntry = guildSettings.MutedUserRel.FirstOrDefault(x => x.UserId == userId);
                guildSettings.MutedUserRel.Remove(muteEntry);

                db.Timers.Delete(timerEntry);
                db.GuildConfig.Update(guildSettings);

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
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var guildSettings = await db.GuildConfig.GetGuildWithWarningsAsync(server.Id, userId);
            var timerEntry = await db.Timers.GetAsync(entryId);
            var localizedReason = _localizer.GetResponseString(guildSettings.Locale, "timedrole");

            try
            {
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
                db.Timers.Delete(timerEntry);
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
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var guildSettings = await db.GuildConfig.GetGuildWithWarningsAsync(server.Id, userId);
            var timerEntry = await db.Timers.GetAsync(entryId);
            var localizedReason = _localizer.GetResponseString(guildSettings.Locale, "timedunrole");

            try
            {
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
                db.Timers.Delete(timerEntry);
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
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var guildSettings = await db.GuildConfig.GetGuildWithWarningsAsync(server.Id, userId);
            var timer = await db.Timers.GetAsync(entryId);

            guildSettings.WarnRel.RemoveAll(x => x.DateAdded.Add(guildSettings.WarnExpire).Subtract(DateTimeOffset.Now) <= TimeSpan.Zero);

            // Update the entries
            db.GuildConfig.Update(guildSettings);
            db.Timers.Delete(timer);

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
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var dbReminder = await db.Reminders.Table.FirstOrDefaultAsync(x => x.TimerId == entryId);
            var dbTimer = await db.Timers.GetAsync(entryId);
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
                    (dbReminder.IsPrivate) ? db.BotConfig.Cache.BotPrefix : db.GuildConfig.GetGuild(dbReminder.GuildId.Value).Prefix,
                    null
                );

                var message = new SmartString(fakeContext, dbReminder.Content);
                var wasDeserialized = _services.GetService<UtilitiesService>().DeserializeEmbed(message.Content, out var dmsg);
                dmsg ??= new();

                var localizedDate = (server is null)
                    ? dbReminder.DateAdded.ToString("D", CultureInfo.CreateSpecificCulture(db.BotConfig.Cache.Locale))
                    : dbReminder.DateAdded.ToString("D", CultureInfo.CreateSpecificCulture(db.GuildConfig.GetGuild(server.Id).Locale));

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
                db.Reminders.Delete(dbReminder);
                db.Timers.Delete(dbTimer);

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
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var dbCmd = await db.AutoCommands.Table.FirstOrDefaultAsync(x => x.TimerId == entryId);
            var dbTimer = await db.Timers.GetAsync(entryId);
            var cmdHandler = client.GetExtension<CommandsNextExtension>();

            try
            {
                var cmd = cmdHandler.FindCommand(dbCmd.CommandString, out var args);
                var user = FindMember(dbCmd.AuthorId, server);

                var fakeContext = cmdHandler.CreateFakeContext(
                    user,
                    server.GetChannel(dbCmd.ChannelId), // If channel is private, this throws
                    dbCmd.CommandString,
                    db.GuildConfig.GetGuild(dbCmd.GuildId).Prefix,
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
                    db.AutoCommands.Delete(dbCmd);
                    db.Timers.Delete(dbTimer);

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