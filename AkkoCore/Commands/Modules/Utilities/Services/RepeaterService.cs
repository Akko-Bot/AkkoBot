using AkkoCore.Commands.Attributes;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
using AkkoCore.Services.Database.Queries;
using AkkoCore.Services.Timers.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Utilities.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="RepeaterEntity"/> objects.
    /// </summary>
    [CommandService(ServiceLifetime.Singleton)]
    public sealed class RepeaterService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAkkoCache _akkoCache;
        private readonly IDbCache _dbCache;

        public RepeaterService(IServiceScopeFactory scopeFactory, IAkkoCache akkoCache, IDbCache dbCache)
        {
            _scopeFactory = scopeFactory;
            _akkoCache = akkoCache;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Adds a repeater to the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="channel">The channel where the repeater is going to run.</param>
        /// <param name="time">How long until the repeater activation. Minimum of 1 minute, maximum of 365 days.</param>
        /// <param name="content">The content to be repeated.</param>
        /// <param name="timeOfDay">Time of day the repeater should trigger. This overrides <paramref name="time"/>, turning this repeater into a daily repeater.</param>
        /// <remarks>Repeaters are limited to the maximum of 5 per Discord guild.</remarks>
        /// <returns><see langword="true"/> if the repeater was successfully added to the database, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddRepeaterAsync(CommandContext context, DiscordChannel channel, TimeSpan time, string content, TimeOfDay? timeOfDay = default)
        {
            if (context.Guild is null || (timeOfDay is null && (time < TimeSpan.FromMinutes(1) || time > TimeSpan.FromDays(365))))
                return false;

            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            // Limit of 5 repeaters per guild
            if (await db.Repeaters.CountAsyncEF(x => x.GuildIdFK == context.Guild.Id) >= 5)
                return false;

            var newTimer = new TimerEntity()
            {
                GuildIdFK = context.Guild?.Id,
                ChannelId = channel.Id,
                UserIdFK = context.User.Id,
                IsRepeatable = true,
                Interval = time,
                Type = TimerType.Repeater,
                TimeOfDay = timeOfDay?.Time,
                ElapseAt = DateTimeOffset.Now.Add(timeOfDay?.Interval ?? time)
            };

            db.Add(newTimer);
            await db.SaveChangesAsync();

            // Stop tracking the db timer, so the db repeater doesn't get cached with it
            // in the navigation property
            db.ChangeTracker.Clear();

            var newRepeater = new RepeaterEntity()
            {
                Content = content,
                TimerIdFK = newTimer.Id,
                GuildIdFK = context.Guild!.Id,
                AuthorId = context.User.Id,
                ChannelId = channel.Id,
                Interval = (timeOfDay is null) ? time : TimeSpan.FromDays(1)
            };

            db.Add(newRepeater);
            await db.SaveChangesAsync();

            // Cache the repeater and create its timer
            _dbCache.Repeaters.TryAdd(context.Guild.Id, new());
            _dbCache.Repeaters[context.Guild.Id].Add(newRepeater);
            _akkoCache.Timers.AddOrUpdateByEntity(context.Client, newTimer);

            return true;
        }

        /// <summary>
        /// Removes a repeater from the database.
        /// </summary>
        /// <param name="server">The Discord guild associated with the repeater.</param>
        /// <param name="id">The database ID of the repeater.</param>
        /// <returns><see langword="true"/> if the repeater was successfully removed from the database, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveRepeaterAsync(DiscordGuild server, int id)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);
            if (!_dbCache.Repeaters.TryGetValue(server.Id, out var repeaterCache))
                return false;

            var dbRepeater = repeaterCache.FirstOrDefault(x => x.Id == id)
                ?? await db.Repeaters
                    .Where(x => x.Id == id)
                    .Select(x => new RepeaterEntity() { Id = x.Id, TimerIdFK = x.TimerIdFK, TimerRel = new() { Id = x.TimerIdFK } })
                    .FirstOrDefaultAsyncEF();

            db.Remove(dbRepeater);
            db.Remove(dbRepeater.TimerRel);
            var result = await db.SaveChangesAsync() is not 0;

            // Remove from the cache after removing from the database,
            // so daily repeaters don't get re-added to the cache
            _dbCache.Repeaters[server.Id].TryRemove(dbRepeater);
            _akkoCache.Timers.TryRemove(dbRepeater.TimerIdFK);

            return result;
        }

        /// <summary>
        /// Removes all repeaters from a given Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild.</param>
        /// <returns><see langword="true"/> if at least one repeater was successfully removed from the database, <see langword="false"/> otherwise.</returns>
        public async Task<bool> ClearRepeatersAsync(DiscordGuild server)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);
            if (!_dbCache.Repeaters.TryRemove(server.Id, out var repeaterCache))
                return false;

            var dbRepeaters = await db.Repeaters
                .Where(x => x.GuildIdFK == server.Id)
                .Select(x => new RepeaterEntity() { TimerIdFK = x.TimerIdFK })
                .ToArrayAsyncEF();

            var result = await db.Timers
                .DeleteAsync(x => x.GuildIdFK == server.Id && x.Type == TimerType.Repeater);

            // Deleting the db timers deletes the db repeaters in cascade
            //await db.Repeaters.DeleteAsync(x => x.GuildIdFK == server.Id);

            // Remove from the cache after removing from the database,
            // so daily repeaters don't get re-added to the cache
            repeaterCache.Clear();

            foreach (var dbRepeater in dbRepeaters)
                _akkoCache.Timers.TryRemove(dbRepeater.TimerIdFK);

            return result is not 0;
        }

        /// <summary>
        /// Gets extra information pertinent to a repeater if the passed data is not available.
        /// </summary>
        /// <param name="dbRepeater">The database repeater.</param>
        /// <param name="user">The author of the repeater.</param>
        /// <param name="timer">The timer responsible for running the repeater.</param>
        /// <remarks>
        /// If <paramref name="timer"/> is <see langword="null"/>, <see cref="TimerEntity"/> will be <see langword="null"/>.
        /// if <paramref name="user"/> is <see langword="null"/>, <see cref="DiscordUserEntity"/> will be <see langword="null"/>.
        /// </remarks>
        /// <returns>The database timer and user from the database.</returns>
        public async Task<(TimerEntity?, DiscordUserEntity?)> GetRepeaterExtraInfoAsync(IAkkoTimer? timer, RepeaterEntity dbRepeater, DiscordMember? user)
        {
            if (dbRepeater is null)
                return (default, default);

            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            var dbTimer = (timer?.Id is null) ? await db.Timers.Where(x => x.Id == dbRepeater.TimerIdFK).FirstOrDefaultAsyncEF() : default;
            var dbUser = (user is null) ? await db.DiscordUsers.Where(x => x.UserId == dbRepeater.AuthorId).FirstOrDefaultAsyncEF() : default;

            return (dbTimer, dbUser);
        }

        /// <summary>
        /// Gets the active repeaters for the specified Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild.</param>
        /// <param name="predicate">Expression tree to filter the result.</param>
        /// <remarks>If <paramref name="predicate"/> is <see langword="null"/>, it returns all guild repeaters.</remarks>
        /// <returns>A collection of repeaters.</returns>
        public IReadOnlyList<RepeaterEntity> GetRepeaters(DiscordGuild server, Func<RepeaterEntity, bool>? predicate = default)
            => GetRepeaters(server, predicate, x => x);

        /// <summary>
        /// Gets a collection of <typeparamref name="T"/> from the repeater entries in the database.
        /// </summary>
        /// <typeparam name="T">The selected returning type.</typeparam>
        /// <param name="server">The Discord guild.</param>
        /// <param name="predicate">Expression tree to filter the result.</param>
        /// <param name="selector">Expression tree to select the columns to be returned.</param>
        /// <remarks>If <paramref name="predicate"/> is <see langword="null"/>, it returns all guild repeaters.</remarks>
        /// <returns>A collection of <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Occurs when <paramref name="selector"/> is <see langword="null"/>.</exception>
        public IReadOnlyList<T> GetRepeaters<T>(DiscordGuild server, Func<RepeaterEntity, bool>? predicate, Func<RepeaterEntity, T> selector)
        {
            _dbCache.Repeaters.TryGetValue(server.Id, out var repeaters);

            repeaters ??= new(1, 0);

            return repeaters
                .Where(predicate ?? (x => true))
                .OrderBy(x => x.DateAdded)
                .Select(selector)
                .ToArray();
        }
    }
}