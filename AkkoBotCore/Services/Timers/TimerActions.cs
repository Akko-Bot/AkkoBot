using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.Entities;
using System;
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
        /// <param name="entryId">The ID of the database entry.</param>
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
        /// <param name="entryId">The ID of the database entry.</param>
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
                server.Roles.TryGetValue(guildSettings.MuteRoleId, out var muteRole);
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
        /// <param name="entryId">The ID of the database entry.</param>
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
        /// <param name="entryId">The ID of the database entry.</param>
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
        /// <param name="entryId">The ID of the database entry.</param>
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
    }
}