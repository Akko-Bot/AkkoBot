using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Caching.Abstractions;
using AkkoDatabase;
using AkkoDatabase.Entities;
using AkkoDatabase.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="ReminderEntity"/> objects.
    /// </summary>
    public class ReminderService : ICommandService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAkkoCache _akkoCache;

        public ReminderService(IServiceScopeFactory scopeFactory, IAkkoCache akkoCache)
        {
            _scopeFactory = scopeFactory;
            _akkoCache = akkoCache;
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

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var userReminders = await db.Reminders
                .Where(x => x.AuthorId == context.User.Id)
                .Select(x => x.GuildId)
                .ToArrayAsync();

            // Limit of 20 reminders per user, 3 reminders per guild if user doesn't have permission to manage messages
            if (userReminders.Length >= 20
                || (context.Guild is not null && userReminders.Count(sid => sid == context.Guild.Id) >= 3 && !context.Member.PermissionsIn(channel).HasPermission(Permissions.ManageMessages)))
                return false;

            var newTimer = new TimerEntity()
            {
                GuildIdFK = context.Guild?.Id,
                ChannelId = channel.Id,
                UserIdFK = context.User.Id,
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
                TimerIdFK = newTimer.Id,
                AuthorId = context.User.Id,
                ChannelId = channel.Id,
                GuildId = context.Guild?.Id,
                IsPrivate = isPrivate,
                ElapseAt = DateTimeOffset.Now.Add(time)
            };

            db.Add(newReminder);
            await db.SaveChangesAsync();

            _akkoCache.Timers.AddOrUpdateByEntity(context.Client, newTimer);

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
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);
            var dbReminder = await db.Reminders
                .Select(x => new ReminderEntity() { Id = x.Id, AuthorId = x.AuthorId, TimerIdFK = x.TimerIdFK, TimerRel = new() { Id = x.TimerIdFK } })
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dbReminder is null || user.Id != dbReminder.AuthorId)
                return false;

            db.Remove(dbReminder);
            db.Remove(dbReminder.TimerRel);

            _akkoCache.Timers.TryRemove(dbReminder.TimerIdFK);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Gets all reminders for the specified user.
        /// </summary>
        /// <param name="user">The user to get the reminders for.</param>
        /// <remarks>The list is ordered by elapse time, in ascending order.</remarks>
        /// <returns>A collection of reminders."/></returns>
        public async Task<IReadOnlyCollection<ReminderEntity>> GetRemindersAsync(DiscordUser user)
            => await GetRemindersAsync(user, x => x);

        /// <summary>
        /// Gets a collection of <typeparamref name="T"/> from the reminder entries in the database.
        /// </summary>
        /// <typeparam name="T">The selected returning type.</typeparam>
        /// <param name="user">The user to get the reminders for.</param>
        /// <param name="selector">Expression tree to select the columns to be returned.</param>
        /// <returns>A collection of <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Occurs when <paramref name="selector"/> is <see langword="null"/>.</exception>
        public async Task<IReadOnlyCollection<T>> GetRemindersAsync<T>(DiscordUser user, Expression<Func<ReminderEntity, T>> selector)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            return await db.Reminders
                .Where(x => x.AuthorId == user.Id)
                .OrderBy(x => x.ElapseAt)
                .Select(selector)
                .ToArrayAsync();
        }
    }
}