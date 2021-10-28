using AkkoCore.Commands.Attributes;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
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

namespace AkkoCore.Commands.Modules.Self.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="AutoCommandEntity"/> objects.
    /// </summary>
    [CommandService(ServiceLifetime.Singleton)]
    public sealed class CommandScheduleService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAkkoCache _akkoCache;

        public CommandScheduleService(IServiceScopeFactory scopeFactory, IAkkoCache akkoCache)
        {
            _scopeFactory = scopeFactory;
            _akkoCache = akkoCache;
        }

        /// <summary>
        /// Adds an autocommand to the database and initializes its corresponding timer.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="time">How long until the command triggers.</param>
        /// <param name="cmdType"><see cref="AutoCommandType.Scheduled"/> for a command that triggers only once, <see cref="AutoCommandType.Repeated"/> for a command that triggers multiple times.</param>
        /// <param name="cmd">The command to be executed.</param>
        /// <param name="cmdArgs">The command's arguments, if any.</param>
        /// <remarks>To create startup commands, use <see cref="AddStartupCommandAsync(CommandContext, Command, string)"/> instead.</remarks>
        /// <returns><see langword="true"/> if the autocommand was successfully created, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddAutoCommandAsync(CommandContext context, TimeSpan time, AutoCommandType cmdType, Command cmd, string? cmdArgs = default)
        {
            if (cmd is null || context.Guild is null || time <= TimeSpan.Zero || cmdType is AutoCommandType.Startup)
                return false;

            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            var newTimer = new TimerEntity()
            {
                GuildIdFK = context.Guild?.Id,
                ChannelId = context.Channel.Id,
                UserIdFK = context.User.Id,
                IsRepeatable = cmdType is AutoCommandType.Repeated,
                Interval = time,
                Type = TimerType.Command,
                ElapseAt = DateTimeOffset.Now.Add(time)
            };

            db.Add(newTimer);
            await db.SaveChangesAsync();

            var newCmd = new AutoCommandEntity()
            {
                TimerIdFK = newTimer.Id,
                CommandString = cmd.QualifiedName + ((string.IsNullOrWhiteSpace(cmdArgs)) ? string.Empty : " " + cmdArgs),
                GuildId = context.Guild!.Id,
                AuthorId = context.User.Id,
                ChannelId = context.Channel.Id,
                Type = cmdType
            };

            db.Add(newCmd);
            _akkoCache.Timers.AddOrUpdateByEntity(context.Client, newTimer);
            await db.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Adds a startup command to the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="cmd">The command to be executed.</param>
        /// <param name="cmdArgs">The command's arguments, if any.</param>
        /// <returns><see langword="true"/> if the startup command was successfully created, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddStartupCommandAsync(CommandContext context, Command cmd, string cmdArgs)
        {
            if (cmd is null || context.Guild is null)
                return false;

            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            var newCmd = new AutoCommandEntity()
            {
                CommandString = cmd.QualifiedName + ((string.IsNullOrWhiteSpace(cmdArgs)) ? string.Empty : " " + cmdArgs),
                GuildId = context.Guild.Id,
                AuthorId = context.User.Id,
                ChannelId = context.Channel.Id,
                Type = AutoCommandType.Startup
            };

            db.Add(newCmd);
            await db.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Removes an autocommand from the database.
        /// </summary>
        /// <param name="user">The user who has created the autocommand.</param>
        /// <param name="id">The ID of the autocommand to be removed.</param>
        /// <returns><see langword="true"/> if the autocommand was successfully removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveAutoCommandAsync(DiscordUser user, int id)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            var dbCmd = await db.AutoCommands
                .Include(x => x.TimerRel)
                .Select(x =>
                    new AutoCommandEntity()
                    {
                        TimerRel = (x.TimerRel! == null!) ? null : new TimerEntity() { Id = x.TimerRel.Id },
                        Id = x.Id,
                        TimerIdFK = x.TimerIdFK,
                        AuthorId = x.AuthorId
                    }
                )
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dbCmd is null || user.Id != dbCmd.AuthorId)
                return false;

            if (dbCmd.TimerRel is not null)
            {
                db.Remove(dbCmd.TimerRel);
                _akkoCache.Timers.TryRemove(dbCmd.TimerRel.Id);
            }

            db.Remove(dbCmd);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Gets all autocommands created by the specified user.
        /// </summary>
        /// <param name="user">The Discord user who created the autocommands.</param>
        /// <returns>A collection of autocommands.</returns>
        public async Task<IReadOnlyCollection<AutoCommandEntity>> GetAutoCommandsAsync(DiscordUser user)
            => await GetAutoCommandsAsync(user, x => x);

        /// <summary>
        /// Gets all autocommands created by the specified user.
        /// </summary>
        /// <param name="user">The Discord user who created the autocommands.</param>
        /// <param name="selector"></param>
        /// <returns>A collection of <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">Occurs when <paramref name="selector"/> is <see langword="null"/>.</exception>
        public async Task<IReadOnlyCollection<T>> GetAutoCommandsAsync<T>(DiscordUser user, Expression<Func<AutoCommandEntity, T>> selector)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            return await db.AutoCommands
                .Where(x => x.AuthorId == user.Id)
                .Select(selector)
                .ToArrayAsync();
        }

        /// <summary>
        /// Gets the time remaining for the autocommand to trigger.
        /// </summary>
        /// <param name="dbEntry">The autocommand entry.</param>
        /// <returns>The time remaining.</returns>
        public string GetElapseTime(AutoCommandEntity dbEntry)
        {
            return dbEntry.Type switch
            {
                AutoCommandType.Startup => "-",

                AutoCommandType.Scheduled or AutoCommandType.Repeated 
                    => (_akkoCache.Timers.TryGetValue(dbEntry.TimerIdFK!.Value, out var timer))
                        ? DateTimeOffset.Now.Add(timer.ElapseIn).ToDiscordTimestamp(TimestampFormat.RelativeTime)
                        : "-",

                _ => throw new NotImplementedException($"Command of type {dbEntry.Type} has not been implemented."),
            };
        }
    }
}