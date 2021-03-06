using System.Linq;
using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Command.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for issuing punishments to Discord users.
    /// </summary>
    public class UserPunishmentService : ICommandService
    {
        private readonly IServiceProvider _services;

        public UserPunishmentService(IServiceProvider services)
            => _services = services;

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
            var dm = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized(message, Formatter.Bold(context.Guild.Name), time?.ToString(@"%d\d\ %h\h\ %m\m")));

            if (reason is not null)
                dm.AddField(context.FormatLocalized("reason"), reason);

            // This returns null if it fails
            return await context.SendLocalizedDmAsync(user, dm, true);
        }

        /// <summary>
        /// Gets the confirmation embed for kicking/banning users.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user being punished.</param>
        /// <param name="emoji">Emoji name with colons or its unicode representation.</param>
        /// <param name="titleKey">The response string for the embed title.</param>
        /// <returns>An embed with basic information about the punished user.</returns>
        public DiscordEmbedBuilder GetPunishEmbed(CommandContext context, DiscordUser user, string emoji, string titleKey)
        {
            return new DiscordEmbedBuilder()
                .WithTitle($"{emoji} " + context.FormatLocalized(titleKey))
                .AddField("user", user.GetFullname(), true)
                .AddField("id", user.Id.ToString(), true);
        }

        /// <summary>
        /// Kicks a user and registers the occurrence in the database.
        /// </summary>
        /// <param name="server">The Discord guild where the punishment took place.</param>
        /// <param name="userId">The ID of the Discord user that got punished.</param>
        /// <param name="reason">The reason for the punishment.</param>
        public async Task KickUser(DiscordGuild server, ulong userId, string reason)
            => await KickUser(server, await server.GetMemberAsync(userId), reason);

        /// <summary>
        /// Kicks a user and registers the occurrence in the database.
        /// </summary>
        /// <param name="server">The Discord guild where the punishment took place.</param>
        /// <param name="user">The Discord user that got punished.</param>
        /// <param name="reason">The reason for the punishment.</param>
        public async Task KickUser(DiscordGuild server, DiscordMember user, string reason)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            await user.RemoveAsync(reason);

            var occurrence = new OccurrenceEntity()
            {
                GuildIdFK = server.Id,
                UserId = user.Id,
                Kicks = 1
            };

            await db.GuildConfig.CreateOccurrenceAsync(server, user.Id, occurrence);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Soft-bans a user and registers the occurrence in the database.
        /// </summary>
        /// <param name="server">The Discord guild where the punishment took place.</param>
        /// <param name="user">The Discord user that got punished.</param>
        /// <param name="days">Days to remove the messages from.</param>
        /// <param name="reason">The reason for the punishment.</param>
        public async Task SoftbanUser(DiscordGuild server, DiscordMember user, int days, string reason)
            => await SoftbanUser(server, user.Id, days, reason);

        /// <summary>
        /// Soft-bans a user and registers the occurrence in the database.
        /// </summary>
        /// <param name="server">The Discord guild where the punishment took place.</param>
        /// <param name="userId">The ID of the Discord user that got punished.</param>
        /// <param name="days">Days to remove the messages from.</param>
        /// <param name="reason">The reason for the punishment.</param>
        public async Task SoftbanUser(DiscordGuild server, ulong userId, int days, string reason)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            // Ban the user
            await server.BanMemberAsync(userId, days, reason);

            // Unban the user
            await server.UnbanMemberAsync(userId);

            var occurrence = new OccurrenceEntity()
            {
                GuildIdFK = server.Id,
                UserId = userId,
                Softbans = 1
            };

            await db.GuildConfig.CreateOccurrenceAsync(server, userId, occurrence);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Soft-bans a user and registers the occurrence in the database.
        /// </summary>
        /// <param name="server">The Discord guild where the punishment took place.</param>
        /// <param name="user">The Discord user that got punished.</param>
        /// <param name="days">Days to remove the messages from.</param>
        /// <param name="reason">The reason for the punishment.</param>
        public async Task BanUser(DiscordGuild server, DiscordMember user, int days, string reason)
            => await BanUser(server, user.Id, days, reason);

        /// <summary>
        /// Soft-bans a user and registers the occurrence in the database.
        /// </summary>
        /// <param name="server">The Discord guild where the punishment took place.</param>
        /// <param name="userId">The ID of the Discord user that got punished.</param>
        /// <param name="days">Days to remove the messages from.</param>
        /// <param name="reason">The reason for the punishment.</param>
        public async Task BanUser(DiscordGuild server, ulong userId, int days, string reason)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            // Ban the user
            await server.BanMemberAsync(userId, days, reason);

            var occurrence = new OccurrenceEntity()
            {
                GuildIdFK = server.Id,
                UserId = userId,
                Bans = 1
            };

            await db.GuildConfig.CreateOccurrenceAsync(server, userId, occurrence);
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
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);

            // If time is less than a minute, set it to a minute.
            if (time < TimeSpan.FromMinutes(1))
                time = TimeSpan.FromMinutes(1);

            // Ban the user
            await BanUser(context.Guild, userId, 1, context.Member.GetFullname() + " | " + reason);

            // Create the new database entry
            var newEntry = new TimerEntity()
            {
                GuildId = context.Guild.Id,
                UserId = userId,
                IsAbsolute = true,
                IsRepeatable = false,
                Interval = time,
                Type = TimerType.TimedBan,
                ElapseAt = DateTimeOffset.Now.Add(time)
            };

            var occurrence = new OccurrenceEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserId = userId,
                Bans = 1
            };

            await db.GuildConfig.CreateOccurrenceAsync(context.Guild, userId, occurrence);

            // Upsert the entry
            db.Timers.Update(newEntry);
            await db.SaveChangesAsync();

            // Add the timer to the cache
            db.Timers.Cache.AddOrUpdateByEntity(context.Client, newEntry);
        }

        /// <summary>
        /// Adds or removes a <paramref name="role"/> to the <paramref name="user"/> and creates a timer for it.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="type"><see cref="WarnPunishType.AddRole"/> to add a temporary role or <see cref="WarnPunishType.RemoveRole"/> to remove a role temporarily.</param>
        /// <param name="time">For how long the role should be added/removed.</param>
        /// <param name="user">The Discord user to have the role added/removed from.</param>
        /// <param name="role">The Discord role to be added/removed.</param>
        /// <param name="reason">The reason for the punishment.</param>
        /// <exception cref="ArgumentException">Occurs when <paramref name="type"/> is invalid.</exception>
        public async Task TimedRolePunish(CommandContext context, WarnPunishType type, TimeSpan time, DiscordMember user, DiscordRole role, string reason = null)
        {
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);

            // If time is less than a minute, set it to a minute.
            if (time < TimeSpan.FromMinutes(1))
                time = TimeSpan.FromMinutes(1);

            // Determine which action should be taken
            if (type == WarnPunishType.AddRole)
                await user.GrantRoleAsync(role, reason);
            else if (type == WarnPunishType.RemoveRole)
                await user.RevokeRoleAsync(role, reason);
            else
                throw new ArgumentException(@"Only punishment types of type ""AddRole"" and ""RemoveRole"" are supported.");

            // Create the new database entry
            var newEntry = new TimerEntity()
            {
                GuildId = context.Guild.Id,
                UserId = user.Id,
                RoleId = role.Id,
                IsAbsolute = true,
                IsRepeatable = false,
                Interval = time,
                Type = (type == WarnPunishType.AddRole) ? TimerType.TimedUnrole : TimerType.TimedRole,
                ElapseAt = DateTimeOffset.Now.Add(time)
            };

            // *Not sure if I should support occurrences here* //

            // Upsert the entry
            db.Timers.AddOrUpdate(newEntry, out var dbEntry);
            await db.SaveChangesAsync();

            // Add the timer to the cache
            db.Timers.Cache.AddOrUpdateByEntity(context.Client, dbEntry);
        }
    }
}