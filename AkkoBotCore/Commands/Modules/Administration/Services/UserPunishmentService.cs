using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Common;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Extensions;
using AkkoBot.Models.Serializable;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Enums;
using AkkoBot.Services.Database.Queries;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for issuing punishments to Discord users.
    /// </summary>
    public class UserPunishmentService : ICommandService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAkkoCache _akkoCache;
        private readonly IDbCache _dbCache;
        private readonly UtilitiesService _utilitiesService;

        public UserPunishmentService(IServiceScopeFactory scopeFactory, IAkkoCache akkoCache, IDbCache dbCache, UtilitiesService utilitiesService)
        {
            _scopeFactory = scopeFactory;
            _akkoCache = akkoCache;
            _dbCache = dbCache;
            _utilitiesService = utilitiesService;
        }

        /// <summary>
        /// Sends a direct message to the specified user with a localized message of the punishment they
        /// received in the context guild.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user that's being punished.</param>
        /// <param name="message">The localized message to be sent.</param>
        /// <param name="reason">The reason of the punishment.</param>
        /// <param name="time">If the punishment is temporary, for how long will it last.</param>
        /// <returns>The <see cref="DiscordMessage"/> that has been sent, <see langword="null"/> if it failed to send the message.</returns>
        public async Task<DiscordMessage> SendPunishmentDmAsync(CommandContext context, DiscordMember user, string message, string reason, TimeSpan? time = null)
        {
            // Create the notification dm
            var dm = new SerializableDiscordMessage()
                .WithDescription(context.FormatLocalized(message, Formatter.Bold(context.Guild.Name), time?.ToString(@"%d\d\ %h\h\ %m\m")));

            if (reason is not null)
                dm.AddField(context.FormatLocalized("reason"), reason);

            // This returns null if it fails
            return await context.SendLocalizedDmAsync(user, dm, true);
        }

        /// <summary>
        /// Sends a direct message to the specified user with the guild's template ban notification or
        /// with the default notification if the guild has no ban template.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user that's being punished.</param>
        /// <param name="reason">The reason of the punishment.</param>
        /// <returns>The <see cref="DiscordMessage"/> that has been sent, <see langword="null"/> if it failed to send the message.</returns>
        public async Task<DiscordMessage> SendBanDmAsync(CommandContext context, DiscordMember user, string reason)
        {
            if (!_dbCache.Guilds.TryGetValue(context.Guild.Id, out var dbGuild))
                return null;

            var template = new SmartString(context, dbGuild.BanTemplate);

            return (_utilitiesService.DeserializeEmbed(template, out var message) || message is not null)
                ? await user.SendMessageSafelyAsync(message)                                    // Send database ban notification
                : string.IsNullOrWhiteSpace(template)                                   // If template is not serializable
                    ? await SendPunishmentDmAsync(context, user, "ban_notification", reason)        // Send default ban notification
                    : await user.SendMessageSafelyAsync(template);                          // Send database ban notification (no embed)
        }

        /// <summary>
        /// Gets the confirmation embed for kicking/banning users.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user being punished.</param>
        /// <param name="emoji">Emoji name with colons or its unicode representation.</param>
        /// <param name="titleKey">The response string for the embed title.</param>
        /// <returns>An embed with basic information about the punished user.</returns>
        public SerializableDiscordMessage GetPunishEmbed(CommandContext context, DiscordUser user, string emoji, string titleKey)
        {
            return new SerializableDiscordMessage()
                .WithTitle($"{emoji} " + context.FormatLocalized(titleKey))
                .AddField("user", user.GetFullname(), true)
                .AddField("id", user.Id.ToString(), true);
        }

        /// <summary>
        /// Kicks a user and registers the occurrence in the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="userId">The ID of the Discord user that got punished.</param>
        /// <param name="reason">The reason for the punishment.</param>
        public async Task KickUserAsync(CommandContext context, ulong userId, string reason)
            => await KickUserAsync(context, await context.Guild.GetMemberAsync(userId), reason);

        /// <summary>
        /// Kicks a user and registers the occurrence in the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The Discord user that got punished.</param>
        /// <param name="reason">The reason for the punishment.</param>
        public async Task KickUserAsync(CommandContext context, DiscordMember user, string reason)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            await user.RemoveAsync(reason);

            var notice = new WarnEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserIdFK = user.Id,
                AuthorId = context.User.Id,
                Type = WarnType.Notice,
                WarningText = context.FormatLocalized("auto_punish", "kick", reason)
            };

            var occurrence = new OccurrenceEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserId = user.Id,
                Notices = 1,
                Kicks = 1
            };

            db.Add(notice);
            db.Upsert(occurrence);

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Soft-bans a user and registers the occurrence in the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The Discord user that got punished.</param>
        /// <param name="days">Days to remove the messages from.</param>
        /// <param name="reason">The reason for the punishment.</param>
        public async Task SoftbanUserAsync(CommandContext context, DiscordMember user, int days, string reason)
            => await SoftbanUserAsync(context, user.Id, days, reason);

        /// <summary>
        /// Soft-bans a user and registers the occurrence in the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="userId">The ID of the Discord user that got punished.</param>
        /// <param name="days">Days to remove the messages from.</param>
        /// <param name="reason">The reason for the punishment.</param>
        public async Task SoftbanUserAsync(CommandContext context, ulong userId, int days, string reason)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            // Ban the user
            await context.Guild.BanMemberAsync(userId, days, reason);

            // Unban the user
            await context.Guild.UnbanMemberAsync(userId);

            var notice = new WarnEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserIdFK = userId,
                AuthorId = context.User.Id,
                Type = WarnType.Notice,
                WarningText = context.FormatLocalized("auto_punish", "softban", reason)
            };

            var occurrence = new OccurrenceEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserId = userId,
                Notices = 1,
                Softbans = 1
            };

            db.Add(notice);
            db.Upsert(occurrence);

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Soft-bans a user and registers the occurrence in the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The Discord user that got punished.</param>
        /// <param name="days">Days to remove the messages from.</param>
        /// <param name="reason">The reason for the punishment.</param>
        public async Task BanUserAsync(CommandContext context, DiscordMember user, int days, string reason)
            => await BanUserAsync(context, user.Id, days, reason);

        /// <summary>
        /// Soft-bans a user and registers the occurrence in the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="userId">The ID of the Discord user that got punished.</param>
        /// <param name="days">Days to remove the messages from.</param>
        /// <param name="reason">The reason for the punishment.</param>
        public async Task BanUserAsync(CommandContext context, ulong userId, int days, string reason)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            // Ban the user
            await context.Guild.BanMemberAsync(userId, days, reason);

            var notice = new WarnEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserIdFK = userId,
                AuthorId = context.User.Id,
                Type = WarnType.Notice,
                WarningText = context.FormatLocalized("auto_punish", "ban", reason)
            };

            var occurrence = new OccurrenceEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserId = userId,
                Notices = 1,
                Bans = 1
            };

            db.Add(notice);
            db.Upsert(occurrence);

            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Bans a user and creates a timer for when they should be unbanned.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="time">When the user should be unbanned.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="reason">The reason for the ban.</param>
        public async Task TimedBanAsync(CommandContext context, TimeSpan time, ulong userId, string reason = null)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            // If time is less than a minute, set it to a minute.
            if (time < TimeSpan.FromMinutes(1))
                time = TimeSpan.FromMinutes(1);

            // Ban the user
            await context.Guild.BanMemberAsync(userId, time.Days, context.Member.GetFullname() + " | " + reason);

            // Create the new database entry
            var dbTimer = new TimerEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserIdFK = userId,
                IsRepeatable = false,
                Interval = time,
                Type = TimerType.TimedBan,
                ElapseAt = DateTimeOffset.Now.Add(time)
            };

            var notice = new WarnEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserIdFK = userId,
                AuthorId = context.User.Id,
                Type = WarnType.Notice,
                WarningText = context.FormatLocalized("auto_punish", "ban", $"{time:%d\\d\\ %h\\h\\ %m\\m\\ %s\\s} | {reason}")
            };

            var occurrence = new OccurrenceEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserId = userId,
                Notices = 1,
                Bans = 1
            };

            db.Add(notice);
            db.Upsert(occurrence);
            db.Update(dbTimer);

            await db.SaveChangesAsync();

            // Add the timer to the cache
            _akkoCache.Timers.AddOrUpdateByEntity(context.Client, dbTimer);
        }

        /// <summary>
        /// Adds or removes a <paramref name="role"/> to the <paramref name="user"/> and creates a timer for it.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="type"><see cref="PunishmentType.AddRole"/> to add a temporary role or <see cref="PunishmentType.RemoveRole"/> to remove a role temporarily.</param>
        /// <param name="time">For how long the role should be added/removed.</param>
        /// <param name="user">The Discord user to have the role added/removed from.</param>
        /// <param name="role">The Discord role to be added/removed.</param>
        /// <param name="reason">The reason for the punishment.</param>
        /// <exception cref="ArgumentException">Occurs when <paramref name="type"/> is invalid.</exception>
        public async Task TimedRolePunishAsync(CommandContext context, PunishmentType type, TimeSpan time, DiscordMember user, DiscordRole role, string reason = null)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            // If time is less than a minute, set it to a minute.
            if (time < TimeSpan.FromMinutes(1))
                time = TimeSpan.FromMinutes(1);

            // Determine which action should be taken
            if (type == PunishmentType.AddRole)
                await user.GrantRoleAsync(role, reason);
            else if (type == PunishmentType.RemoveRole)
                await user.RevokeRoleAsync(role, reason);
            else
                throw new ArgumentException(@"Only punishment types of type ""AddRole"" and ""RemoveRole"" are supported.");

            // Create the new database entry
            var dbTimer = new TimerEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserIdFK = user.Id,
                RoleId = role.Id,
                IsRepeatable = false,
                Interval = time,
                Type = (type is PunishmentType.AddRole) ? TimerType.TimedUnrole : TimerType.TimedRole,
                ElapseAt = DateTimeOffset.Now.Add(time)
            };

            var notice = new WarnEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserIdFK = user.Id,
                AuthorId = context.User.Id,
                Type = WarnType.Notice,
                WarningText = context.FormatLocalized("auto_punish", type.ToString().ToSnakeCase(), $"{role.Name} | {time:%d\\d\\ %h\\h\\ %m\\m\\ %s\\s}")
            };

            var occurrence = new OccurrenceEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserId = user.Id,
                Notices = 1,
            };

            // Upsert the entry
            db.Add(notice);
            db.Upsert(occurrence);
            db.Upsert(dbTimer);

            await db.SaveChangesAsync();

            // Add the timer to the cache
            _akkoCache.Timers.AddOrUpdateByEntity(context.Client, dbTimer);
        }
    }
}