using AkkoBot.Commands.Abstractions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="ReminderEntity"/> objects.
    /// </summary>
    public class ReminderService : AkkoCommandService
    {
        public ReminderService(IServiceProvider services) : base(services) { }

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

            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            // Limit of 120 reminders per user
            if (await db.Reminders.UserReminderCountAsync(context.User.Id) >= 120)
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

            db.Timers.Create(newTimer);
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

            db.Reminders.Create(newReminder);
            await db.SaveChangesAsync();

            return db.Timers.Cache.AddOrUpdateByEntity(context.Client, newTimer);
        }

        /// <summary>
        /// Removes a reminder by its database ID.
        /// </summary>
        /// <param name="user">Discord user to remove the reminder from.</param>
        /// <param name="id">The ID of the reminder.</param>
        /// <returns><see langword="true"/> if the reminder got successfully removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveReminderAsync(DiscordUser user, int id)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();
            var dbEntry = await db.Reminders.GetAsync(id);

            if (dbEntry is null || user.Id != dbEntry.AuthorId)
                return false;

            var dbTimer = await db.Timers.GetAsync(dbEntry.TimerId);

            db.Reminders.Delete(dbEntry);
            db.Timers.Delete(dbTimer);

            db.Timers.Cache.TryRemove(dbTimer.Id);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Gets all reminders for the specified user.
        /// </summary>
        /// <param name="user">The user to get the reminders for.</param>
        /// <remarks>The list is ordered by elapse time, in ascending order.</remarks>
        /// <returns>A collection of reminders."/></returns>
        public async Task<List<ReminderEntity>> GetRemindersAsync(DiscordUser user)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            return (await db.Reminders.GetAsync(x => x.AuthorId == user.Id))
                .OrderBy(x => x.ElapseAt)
                .ToList();
        }
    }
}
