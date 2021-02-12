using System.Linq;
using System;
using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus;
using AkkoBot.Services.Database.Entities;

namespace AkkoBot.Command.Modules.Administration.Services
{
    public class RoleService : ICommandService
    {
        private readonly IServiceProvider _services;

        public RoleService(IServiceProvider services)
            => _services = services;

        /// <summary>
        /// Checks if the <paramref name="context"/> user can perform actions on the specified <paramref name="user"/> and sends an error message if the check fails.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="user">The targeted user.</param>
        /// <param name="errorMessage">The error message to be sent if the check fails.</param>
        /// <returns><see langword="true"/> if the context user is above in the hierarchy, <see langword="false"/> otherwise.</returns>
        public async Task<bool> CheckHierarchyAsync(CommandContext context, DiscordMember user, string errorMessage)
        {
            if ((context.Member.Hierarchy <= user.Hierarchy
            && context.Guild.CurrentMember.Hierarchy <= user.Hierarchy)
            || user.Equals(context.Guild.CurrentMember))
            {
                var embed = new DiscordEmbedBuilder().WithDescription(errorMessage);
                await context.RespondLocalizedAsync(embed, isError: true);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the mute role of the specified server.
        /// </summary>
        /// <param name="server">The Discord server.</param>
        /// <remarks>If there isn't one, a new mute role is created.</remarks>
        /// <returns>The server mute role.</returns>
        public async Task<DiscordRole> FetchMuteRole(DiscordGuild server)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            var guildSettings = db.GuildConfig.GetGuild(server.Id);

            if (!server.Roles.TryGetValue(guildSettings.MuteRoleId, out var muteRole))
            {
                // Create a new mute role
                muteRole = await server.CreateRoleAsync("AkkoMute", Permissions.ReadMessageHistory);

                // Save it to the database
                guildSettings.MuteRoleId = muteRole.Id;
                db.GuildConfig.CreateOrUpdate(guildSettings);
                await db.SaveChangesAsync();
            }

            return muteRole;
        }

        /// <summary>
        /// Sets the channel overriders for the mute role on all channels visible to the bot.
        /// </summary>
        /// <param name="server">The Discord server.</param>
        /// <param name="muteRole">The mute role.</param>
        public async Task SetMuteOverwrites(DiscordGuild server, DiscordRole muteRole)
        {
            foreach (var channel in server.Channels.Values.Where(x => x.Users.Contains(server.CurrentMember)))
                await channel.AddOverwriteAsync(muteRole, Permissions.None, Permissions.SendMessages | Permissions.AddReactions | Permissions.AttachFiles | Permissions.Speak);
        }

        /// <summary>
        /// Assigns the mute role to a Discord <paramref name="user"/>, registers the muted user
        /// to the database and creates a timer for when they should be unmuted.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="muteRole">The mute role.</param>
        /// <param name="user">The Discord user being muted.</param>
        /// <param name="time">For how long the user should remain muted.</param>
        /// <param name="reason">The reason for the mute.</param>
        /// <remarks>
        /// If <paramref name="time"/> is <see cref="TimeSpan.Zero"/>, no timer is created,
        /// effectively making the mute permanent.
        /// </remarks>
        /// <returns><see langword="true"/> if a timer was created, <see langword="false"/> otherwise.</returns>
        public async Task<bool> MuteUser(CommandContext context, DiscordRole muteRole, DiscordMember user, TimeSpan time, string reason)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            // Mute the user
            await user.GrantRoleAsync(muteRole, reason);

            // Save to the database
            var muteEntry = new MutedUserEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserId = user.Id,
                ElapseAt = DateTimeOffset.Now.Add(time)
            };

            db.MutedUsers.AddOrUpdate(muteEntry);

            // Add timer if mute is not permanent
            if (time != TimeSpan.Zero)
            {
                var timerEntry = new TimerEntity(muteEntry);

                db.Timers.AddOrUpdate(timerEntry, out var dbTimerEntry);
                await db.SaveChangesAsync();

                db.Timers.Cache.AddOrUpdateByEntity(context.Client, dbTimerEntry);
                return true;
            }

            await db.SaveChangesAsync();
            return false;
        }

        /// <summary>
        /// Unmutes a Discord user and removes their associated register and timer.
        /// </summary>
        /// <param name="server">The Discord server.</param>
        /// <param name="muteRole">The mute role.</param>
        /// <param name="user">The Discord user being unmuted.</param>
        /// <param name="reason">The reason for the unmute.</param>
        public async Task UnmuteUser(DiscordGuild server, DiscordRole muteRole, DiscordMember user, string reason)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            // Unmute the user
            await user.RevokeRoleAsync(muteRole, reason);

            // Remove from the database
            var muteEntry = db.MutedUsers.GetMutedUser(server.Id, user.Id);

            var timerEntry = (await db.Timers.GetAsync(x => x.GuildId == server.Id && x.UserId == user.Id))
                .FirstOrDefault();

            if (muteEntry is not null)
                db.MutedUsers.Delete(muteEntry);

            if (timerEntry is not null)
            {
                db.Timers.Delete(timerEntry);
                db.Timers.Cache.TryRemove(timerEntry.Id);
            }

            await db.SaveChangesAsync();
        }
    }
}