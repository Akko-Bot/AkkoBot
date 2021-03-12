using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Command.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="DiscordRole"/> objects.
    /// </summary>
    public class RoleService : ICommandService
    {
        private readonly IServiceProvider _services;

        /// <summary>
        /// Defines the set of denied permissions to be applied to a muted user.
        /// </summary>
        public const Permissions MutePermsDeny = Permissions.SendMessages | Permissions.AddReactions | Permissions.AttachFiles | Permissions.Speak | Permissions.Stream;

        /// <summary>
        /// Defines the set of denied permissions to be applied to a text muted user.
        /// </summary>
        public const Permissions MuteTextPermsDeny = Permissions.SendMessages | Permissions.AddReactions | Permissions.AttachFiles;

        /// <summary>
        /// Defines the set of allowed permissions to be applied to a muted user.
        /// </summary>
        public const Permissions MutePermsAllow = Permissions.AccessChannels;

        public RoleService(IServiceProvider services)
            => _services = services;

        /// <summary>
        /// A more permissive version of <see cref="HierarchyCheckAsync(CommandContext, DiscordMember, string)"/>
        /// that does not check for the bot's position in the role hierarchy.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="user">The targeted user.</param>
        /// <param name="errorMessage">The error message to be sent if the check fails.</param>
        /// <returns><see langword="true"/> if the context user is higher than the target user, <see langword="false"/> otherwise.</returns>
        public async Task<bool> SoftHierarchyCheckAsync(CommandContext context, DiscordMember user, string errorMessage)
            => context.Member.Hierarchy >= user.Hierarchy || await HierarchyCheckAsync(context, user, errorMessage);

        /// <summary>
        /// Checks if the <paramref name="context"/> user can perform actions on the specified <paramref name="user"/> and sends an error message if the check fails.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="user">The targeted user.</param>
        /// <param name="errorMessage">The error message to be sent if the check fails.</param>
        /// <returns><see langword="true"/> if the context user is above in the hierarchy, <see langword="false"/> otherwise.</returns>
        public async Task<bool> HierarchyCheckAsync(CommandContext context, DiscordMember user, string errorMessage)
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
        public async Task<DiscordRole> FetchMuteRoleAsync(DiscordGuild server)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            var guildSettings = db.GuildConfig.GetGuild(server.Id);

            if (!server.Roles.TryGetValue(guildSettings.MuteRoleId ?? 0, out var muteRole))
            {
                // Create a new mute role
                muteRole = await server.CreateRoleAsync("AkkoMute", MutePermsAllow);

                // Save it to the database
                guildSettings.MuteRoleId = muteRole.Id;
                db.GuildConfig.CreateOrUpdate(guildSettings);
                await db.SaveChangesAsync();
            }

            return muteRole;
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
        public async Task<bool> MuteUserAsync(CommandContext context, DiscordRole muteRole, DiscordMember user, TimeSpan time, string reason)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            // Mute the user
            await user.GrantRoleAsync(muteRole, reason);

            if (user.VoiceState is not null)
                await user.SetMuteAsync(true);

            // Save to the database
            var guildSettings = await db.GuildConfig.GetGuildWithMutesAsync(context.Guild.Id);

            var muteEntry = new MutedUserEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserId = user.Id
            };

            var occurrence = new OccurrenceEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserId = user.Id,
                Mutes = 1
            };

            // If user is already muted, do nothing
            if (!guildSettings.MutedUserRel.Any(x => x.UserId == user.Id))
            {
                await db.GuildConfig.CreateOccurrenceAsync(context.Guild, user.Id, occurrence);
                guildSettings.MutedUserRel.Add(muteEntry);
                db.GuildConfig.Update(guildSettings);
            }

            // Add timer if mute is not permanent
            if (time > TimeSpan.Zero)
            {
                var timerEntry = new TimerEntity(muteEntry, time);

                db.Timers.AddOrUpdate(timerEntry, out var dbTimerEntry);
                await db.SaveChangesAsync();

                db.Timers.Cache.AddOrUpdateByEntity(context.Client, dbTimerEntry);
                return true;
            }

            await db.SaveChangesAsync();
            return false;
        }

        /// <summary>
        /// Unmutes a Discord user and removes their associated register and timer from the database.
        /// </summary>
        /// <param name="server">The Discord server.</param>
        /// <param name="muteRole">The mute role.</param>
        /// <param name="user">The Discord user being unmuted.</param>
        /// <param name="reason">The reason for the unmute.</param>
        public async Task UnmuteUserAsync(DiscordGuild server, DiscordRole muteRole, DiscordMember user, string reason)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            // Unmute the user
            await user.RevokeRoleAsync(muteRole, reason);

            if (user.VoiceState is not null)
                await user.SetMuteAsync(false);

            // Remove from the database
            var guildSettings = await db.GuildConfig.GetGuildWithMutesAsync(server.Id);
            var muteEntry = guildSettings.MutedUserRel.FirstOrDefault(x => x.UserId == user.Id);

            var timerEntry = (await db.Timers.GetAsync(x => x.GuildId == server.Id && x.UserId == user.Id))
                .FirstOrDefault();

            if (muteEntry is not null)
            {
                guildSettings.MutedUserRel.Remove(muteEntry);
                db.GuildConfig.Update(guildSettings);
            }

            if (timerEntry is not null)
            {
                db.Timers.Delete(timerEntry);
                db.Timers.Cache.TryRemove(timerEntry.Id);
            }

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Sets server voice mute to a Discord user.
        /// </summary>
        /// <param name="user">The Discord user to set the server voice mute.</param>
        /// <param name="isMuting"><see langword="true"/> if they should be muted, <see langword="false"/> if they should be unmuted.</param>
        /// <param name="responseKey">The key of the response string to be used in the response.</param>
        /// <param name="reason">The reason for the mute/unmute.</param>
        /// <returns>An embed with the appropriate response key.</returns>
        public async Task<DiscordEmbedBuilder> SetVoiceMuteAsync(DiscordMember user, bool isMuting, string responseKey, string reason)
        {
            var embed = new DiscordEmbedBuilder();

            if (user.VoiceState is not null)
                embed.WithDescription("voice_failure");
            else
            {
                await user.SetMuteAsync(isMuting, reason);
                embed.WithDescription(responseKey);
            }

            return embed;
        }

        /// <summary>
        /// Sets server deafen to a Discord user.
        /// </summary>
        /// <param name="user">The Discord user to set the server voice mute.</param>
        /// <param name="isDeafening"><param name="isMuting"><see langword="true"/> if they should be deafened, <see langword="false"/> if they should be "undeafened".</param></param>
        /// <param name="responseKey">The key of the response string to be used in the response.</param>
        /// <param name="reason">The reason for the deaf/undeaf.</param>
        /// <returns>An embed with the appropriate response key.</returns>
        public async Task<DiscordEmbedBuilder> SetDeafAsync(DiscordMember user, bool isDeafening, string responseKey, string reason)
        {
            var embed = new DiscordEmbedBuilder();

            if (user.VoiceState is not null)
                embed.WithDescription("voice_failure");
            else
            {
                await user.SetDeafAsync(isDeafening, reason);
                embed.WithDescription(responseKey);
            }

            return embed;
        }
    }
}