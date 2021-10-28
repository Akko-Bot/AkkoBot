using AkkoCore.Commands.Attributes;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
using AkkoCore.Services.Database.Queries;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="DiscordRole"/> objects.
    /// </summary>
    [CommandService(ServiceLifetime.Singleton)]
    public sealed class RoleService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAkkoCache _akkoCache;
        private readonly IDbCache _dbCache;

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

        public RoleService(IServiceScopeFactory scopeFactory, IAkkoCache akkoCache, IDbCache dbCache)
        {
            _scopeFactory = scopeFactory;
            _akkoCache = akkoCache;
            _dbCache = dbCache;
        }

        /// <summary>
        /// A more permissive version of <see cref="CheckHierarchyAsync(CommandContext, DiscordMember, string)"/>
        /// that does not check for the bot's position in the role hierarchy and sends an error message if the check fails.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="user">The targeted user.</param>
        /// <param name="errorMessage">The error message to be sent if the check fails.</param>
        /// <returns><see langword="true"/> if the context user is higher than the target user, <see langword="false"/> otherwise.</returns>
        public async Task<bool> SoftCheckHierarchyAsync(CommandContext context, DiscordMember user, string errorMessage)
            => context.Member.Hierarchy >= user.Hierarchy || await CheckHierarchyAsync(context, user, errorMessage);

        /// <summary>
        /// Checks if the <paramref name="context"/> user can perform actions on the specified <paramref name="user"/> and sends an error message if the check fails.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="user">The targeted user.</param>
        /// <param name="errorMessage">The error message to be sent if the check fails.</param>
        /// <returns><see langword="true"/> if the context user is above in the hierarchy, <see langword="false"/> otherwise.</returns>
        public async Task<bool> CheckHierarchyAsync(CommandContext context, DiscordMember user, string errorMessage)
        {
            if (!CheckHierarchyAsync(context.Member, user))
            {
                var embed = new SerializableDiscordEmbed()
                    .WithDescription(errorMessage);

                await context.RespondLocalizedAsync(embed, isError: true);

                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the <paramref name="userA"/> can perform actions on <paramref name="userB"/>.
        /// </summary>
        /// <param name="userA">The user that is performing the action.</param>
        /// <param name="userB">The user that is being acted upon.</param>
        /// <returns><see langword="true"/> if the <paramref name="userA"/> is above in the hierarchy, <see langword="false"/> otherwise.</returns>
        public bool CheckHierarchyAsync(DiscordMember userA, DiscordMember userB)
        {
            return (userA.Hierarchy > userB.Hierarchy || userA.Guild.CurrentMember.Hierarchy > userB.Hierarchy)
            && !userB.Equals(userA.Guild.CurrentMember);
        }

        /// <summary>
        /// Gets the mute role of the specified server.
        /// </summary>
        /// <param name="server">The Discord server.</param>
        /// <remarks>If there isn't one, a new mute role is created.</remarks>
        /// <returns>The server mute role.</returns>
        public async Task<DiscordRole> FetchMuteRoleAsync(DiscordGuild server)
        {
            var dbGuild = await _dbCache.GetDbGuildAsync(server.Id);

            if (server.Roles.TryGetValue(dbGuild.MuteRoleId ?? default, out var muteRole))
                return muteRole;

            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            // Create a new mute role
            muteRole = await server.CreateRoleAsync("AkkoMute", MutePermsAllow);

            // Save it to the database
            dbGuild.MuteRoleId = muteRole.Id;

            await db.GuildConfig.UpdateAsync(
                x => x.GuildId == server.Id,
                _ => new GuildConfigEntity() { MuteRoleId = muteRole.Id }
            );

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
        public async Task<bool> MuteUserAsync(CommandContext context, DiscordRole muteRole, DiscordMember user, TimeSpan time, string? reason)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            // Mute the user
            await user.GrantRoleAsync(muteRole, reason);

            if (user.VoiceState is not null)
                await user.SetMuteAsync(true);

            // Save to the database

            var muteEntry = new MutedUserEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserId = user.Id
            };

            // If user is already muted, do nothing
            if (!await db.MutedUsers.AnyAsyncEF(x => x.GuildIdFK == context.Guild.Id && x.UserId == user.Id))
            {
                var notice = new WarnEntity()
                {
                    GuildIdFK = context.Guild.Id,
                    UserIdFK = user.Id,
                    AuthorId = context.User.Id,
                    Type = WarnType.Notice,
                    WarningText = context.FormatLocalized("auto_punish", "mute", time.ToString(@"%d\d\ %h\h\ %m\m\ %s\s"))
                };

                var occurrence = new OccurrenceEntity()
                {
                    GuildIdFK = context.Guild.Id,
                    UserId = user.Id,
                    Notices = 1,
                    Mutes = 1
                };

                db.Add(notice);
                db.Add(muteEntry);
                db.Upsert(occurrence);
            }

            // Add timer if mute is not permanent
            if (time > TimeSpan.Zero)
            {
                var timerEntry = new TimerEntity(muteEntry, time);

                db.Timers.Upsert(timerEntry);
                await db.SaveChangesAsync();

                _akkoCache.Timers.AddOrUpdateByEntity(context.Client, timerEntry);
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
        public async Task UnmuteUserAsync(DiscordGuild server, DiscordRole muteRole, DiscordMember user, string? reason)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            // Unmute the user
            await user.RevokeRoleAsync(muteRole, reason);

            if (user.VoiceState is not null)
                await user.SetMuteAsync(false);

            // Remove from the database
            await db.MutedUsers.DeleteAsync(x => x.GuildIdFK == server.Id && x.UserId == user.Id);
            var timerId = await db.Timers
                .Where(x => x.GuildIdFK == server.Id && x.UserIdFK == user.Id && x.Type == TimerType.TimedMute)
                .Select(x => x.Id)
                .FirstOrDefaultAsyncEF();

            if (timerId is not default(int))
            {
                await db.Timers.DeleteAsync(x => x.Id == timerId);
                _akkoCache.Timers.TryRemove(timerId);
            }
        }

        /// <summary>
        /// Sets server voice mute to a Discord user.
        /// </summary>
        /// <param name="user">The Discord user to set the server voice mute.</param>
        /// <param name="isMuting"><see langword="true"/> if they should be muted, <see langword="false"/> if they should be unmuted.</param>
        /// <param name="responseKey">The key of the response string to be used in the response.</param>
        /// <param name="reason">The reason for the mute/unmute.</param>
        /// <returns>An embed with the appropriate response key.</returns>
        public async Task<SerializableDiscordEmbed> SetVoiceMuteAsync(DiscordMember user, bool isMuting, string responseKey, string? reason)
        {
            var embed = new SerializableDiscordEmbed();

            if (user.VoiceState is null)
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
        public async Task<SerializableDiscordEmbed> SetDeafAsync(DiscordMember user, bool isDeafening, string responseKey, string? reason)
        {
            var embed = new SerializableDiscordEmbed();

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