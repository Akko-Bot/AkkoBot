using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Queries;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="ReminderEntity"/> objects.
    /// </summary>
    public class ReminderService : ICommandService
    {
        private readonly IServiceProvider _services;
        private readonly IDbCache _dbCache;

        public ReminderService(IServiceProvider services, IDbCache dbCache)
        {
            _services = services;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Adds a reminder to the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="channel">The channel where the reminder should be sent to.</param>
        /// <param name="time">How long until the reminder activation. Minimum of 1 minute, maximum of 365 days.</param>
        /// <param name="isPrivate">Defines whether the target <paramref name="channel"/> is private or not.</param>
        /// <param name="content">The content to be sent in the reminder.</param>
        /// <remarks>A timer for triggering the reminder will also be created.</remarks>
        /// <returns><see langword="true"/> if the reminder got successfully added to the database, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddReminderAsync(CommandContext context, DiscordChannel channel, TimeSpan time, bool isPrivate, string content)
        {
            if (time < TimeSpan.FromMinutes(1) || time > TimeSpan.FromDays(365))
                return false;

            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            // Limit of 120 reminders per user
            if (await db.CountAsync<ReminderEntity>(x => x.AuthorId == context.User.Id) >= 120)
                return false;

            var newTimer = new TimerEntity()
            {
                GuildId = context.Guild?.Id,
                ChannelId = channel.Id,
                UserId = context.User.Id,
                IsAbsolute = true,
                IsRepeatable = false,
                Interval = time,
                Type = TimerType.Reminder,
                ElapseAt = DateTimeOffset.Now.Add(time)
            };

            db.Add(newTimer);
            await db.SaveChangesAsync();

            var newReminder = new ReminderEntity()
            {
                Content = content,
                TimerId = newTimer.Id,
                AuthorId = context.User.Id,
                ChannelId = channel.Id,
                GuildId = context.Guild?.Id,
                IsPrivate = isPrivate,
                ElapseAt = DateTimeOffset.Now.Add(time)
            };

            db.Add(newReminder);
            await db.SaveChangesAsync();

            _dbCache.Timers.AddOrUpdateByEntity(context.Client, newTimer);

            return true;
        }

        /// <summary>
        /// Removes a reminder by its database ID.
        /// </summary>
        /// <param name="user">Discord user to remove the reminder from.</param>
        /// <param name="id">The ID of the reminder.</param>
        /// <returns><see langword="true"/> if the reminder got successfully removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveReminderAsync(DiscordUser user, int id)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);
            var dbEntry = await db.Reminders.FindAsync(id);

            if (dbEntry is null || user.Id != dbEntry.AuthorId)
                return false;

            var dbTimer = await db.Timers.FindAsync(dbEntry.TimerId);

            db.Remove(dbEntry);
            db.Remove(dbTimer);

            _dbCache.Timers.TryRemove(dbTimer.Id);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Gets all reminders for the specified user.
        /// </summary>
        /// <param name="user">The user to get the reminders for.</param>
        /// <remarks>The list is ordered by elapse time, in ascending order.</remarks>
        /// <returns>A collection of reminders."/></returns>
        public async Task<ReminderEntity[]> GetRemindersAsync(DiscordUser user)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            return await db.Reminders.Fetch(x => x.AuthorId == user.Id)
                .OrderBy(x => x.ElapseAt)
                .ToArrayAsync();
        }
    }
}