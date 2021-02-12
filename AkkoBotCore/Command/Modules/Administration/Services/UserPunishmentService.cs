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
        /// <summary>
        /// Sends a direct message to the specified user with a localized message of the punishment they
        /// received in the context guild.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user that's being punished.</param>
        /// <param name="message">The localized message to be sent.</param>
        /// <param name="reason">The reason of the punishment.</param>
        /// <returns>The <see cref="DiscordMessage"/> that has been sent, <see langword="null"/> if it failed to send the message.</returns>
        public async Task<DiscordMessage> SendPunishmentDm(CommandContext context, DiscordMember user, string message, string reason)
        {
            // Create the notification dm
            var dm = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized(message, Formatter.Bold(context.Guild.Name)));

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
        /// Bans a user and creates a timer for when they should be unbanned.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="time">When the user should be unbanned.</param>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="reason">The reason for the ban.</param>
        public async Task TimedBan(CommandContext context, TimeSpan time, ulong userId, string reason = null)
        {
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);

            // If time is less than a minute, set it to a minute.
            if (time < TimeSpan.FromMinutes(1))
                time = TimeSpan.FromMinutes(1);

            // Ban the user
            await context.Guild.BanMemberAsync(userId, 1, context.Member.GetFullname() + " | " + reason);

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

            // Upsert the entry
            db.Timers.AddOrUpdate(newEntry, out var dbEntry);
            await db.SaveChangesAsync();

            // Add the timer to the cache
            db.Timers.Cache.AddOrUpdateByEntity(context.Client, dbEntry);
        }
    }
}