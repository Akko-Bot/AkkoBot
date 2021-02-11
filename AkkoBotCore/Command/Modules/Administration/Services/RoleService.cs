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

        public async Task<DiscordRole> GetMuteRole(DiscordGuild server)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            var guildSettings = db.GuildConfig.GetGuild(server.Id);

            if (!server.Roles.TryGetValue(guildSettings.MuteRoleId, out var role))
            {
                // Create a new mute role
                role = await server.CreateRoleAsync("AkkoMute", Permissions.ReadMessageHistory);

                // Save it to the database
                guildSettings.MuteRoleId = role.Id;
                db.GuildConfig.CreateOrUpdate(guildSettings);
                await db.SaveChangesAsync();
            }
            
            return role;
        }

        public async Task SetMuteOverwrites(DiscordGuild server, DiscordRole role)
        {
            foreach (var channel in server.Channels.Values.Where(x => x.Users.Contains(server.CurrentMember)))
                await channel.AddOverwriteAsync(role, Permissions.None, Permissions.SendMessages | Permissions.AddReactions | Permissions.AttachFiles | Permissions.Speak);
        }

        public async Task PermaMuteUser(DiscordGuild server, DiscordRole role, DiscordMember user, string reason)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            // Mute the user
            await user.GrantRoleAsync(role, reason);

            // Save to the database
            var newEntry = new MutedUserEntity()
            {
                GuildIdFK = server.Id,
                UserId = user.Id,
                ElapseAt = DateTimeOffset.Now.Add(TimeSpan.FromHours(1))
            };

            db.MutedUsers.AddOrUpdate(newEntry);
            await db.SaveChangesAsync();
        }

        public async Task UnmuteUser(DiscordGuild server, DiscordRole role, DiscordMember user, string reason)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            // Unmute the user
            await user.RevokeRoleAsync(role, reason);

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