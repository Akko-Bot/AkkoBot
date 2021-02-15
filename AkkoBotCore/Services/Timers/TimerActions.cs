using System.Linq;
using System;
using System.Windows.Input;
using AkkoBot.Command.Abstractions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using AkkoBot.Extensions;

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
        public async Task Unban(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            
            var settings = db.GuildConfig.GetGuild(server.Id);
            var localizedReason = _localizer.GetResponseString(settings.Locale, "timedban_title");

            // Perform the action
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
        public async Task Unmute(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var guildSettings = await db.GuildConfig.GetGuildWithMutesAsync(server.Id);
            var localizedReason = _localizer.GetResponseString(guildSettings.Locale, "timedmute");

            try
            {
                // *User may not be in the guild when this method runs
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
    }
}