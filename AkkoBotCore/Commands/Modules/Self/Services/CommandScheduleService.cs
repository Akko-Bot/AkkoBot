using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
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

namespace AkkoBot.Commands.Modules.Self.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="CommandEntity"/> objects.
    /// </summary>
    public class CommandScheduleService : AkkoCommandService
    {
        private readonly IServiceProvider _services;

        public CommandScheduleService(IServiceProvider services) : base(services)
            => _services = services;

        /// <summary>
        /// Adds an autocommand to the database and initializes its corresponding timer.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="time">How long until the command triggers.</param>
        /// <param name="cmdType"><see cref="CommandType.Scheduled"/> for a command that triggers only once, <see cref="CommandType.Repeated"/> for a command that triggers multiple times.</param>
        /// <param name="cmd">The command to be executed.</param>
        /// <param name="cmdArgs">The command's arguments, if any.</param>
        /// <remarks>To create startup commands, use <see cref="AddStartupCommandAsync(CommandContext, Command, string)"/> instead.</remarks>
        /// <returns><see langword="true"/> if the autocommand was successfully created, <see langword="false"/> otherwise.</returns>
        public async Task<bool> AddAutoCommandAsync(CommandContext context, TimeSpan time, CommandType cmdType, Command cmd, string cmdArgs = null)
        {
            if (cmd is null || context.Guild is null || time <= TimeSpan.Zero || cmdType is CommandType.Startup)
                return false;

            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var newTimer = new TimerEntity()
            {
                GuildId = context.Guild?.Id,
                ChannelId = context.Channel.Id,
                UserId = context.User.Id,
                IsAbsolute = true,
                IsRepeatable = cmdType is CommandType.Repeated,
                Interval = time,
                Type = TimerType.Command,
                ElapseAt = DateTimeOffset.Now.Add(time)
            };

            db.Timers.Create(newTimer);
            await db.SaveChangesAsync();

            var newCmd = new CommandEntity()
            {
                TimerId = newTimer.Id,
                CommandString = cmd.QualifiedName + ((string.IsNullOrWhiteSpace(cmdArgs)) ? string.Empty : " " + cmdArgs),
                GuildId = context.Guild.Id,
                AuthorId = context.User.Id,
                ChannelId = context.Channel.Id,
                Type = cmdType
            };

            db.AutoCommands.Create(newCmd);
            db.Timers.Cache.AddOrUpdateByEntity(context.Client, newTimer);
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

            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var newCmd = new CommandEntity()
            {
                CommandString = cmd.QualifiedName + ((string.IsNullOrWhiteSpace(cmdArgs)) ? string.Empty : " " + cmdArgs),
                GuildId = context.Guild.Id,
                AuthorId = context.User.Id,
                ChannelId = context.Channel.Id,
                Type = CommandType.Startup
            };

            db.AutoCommands.Create(newCmd);
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
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            var dbCmd = await db.AutoCommands.GetAsync(id);

            if (dbCmd is null || user.Id != dbCmd.AuthorId)
                return false;

            var dbTimer = await db.Timers.GetAsync(dbCmd.TimerId);

            if (dbTimer is not null)
            {
                db.Timers.Delete(dbTimer);
                db.Timers.Cache.TryRemove(dbTimer.Id);
            }

            db.AutoCommands.Delete(dbCmd);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Gets all autocommands under the specified user.
        /// </summary>
        /// <param name="user">The Discord user who created the autocommands.</param>
        /// <returns>A collection of autocommands.</returns>
        public async Task<IEnumerable<CommandEntity>> GetAutoCommandsAsync(DiscordUser user)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            return (await db.AutoCommands.Table
                .AsNoTracking()
                .Where(x => x.AuthorId == user.Id)
                .ToListAsync())
                .OrderBy(x => GetElapseTime(x));
        }

        /// <summary>
        /// Gets the time remaining for the autocommand to trigger.
        /// </summary>
        /// <param name="dbEntry">The autocommand entry.</param>
        /// <returns>The time remaining.</returns>
        public string GetElapseTime(CommandEntity dbEntry)
        {
            switch (dbEntry.Type)
            {
                case CommandType.Startup:
                    return "-";

                case CommandType.Scheduled:
                case CommandType.Repeated:
                    _services.GetService<IDbCacher>().Timers.TryGetValue(dbEntry.TimerId.Value, out var timer);
                    return timer.ElapseAt.Subtract(DateTimeOffset.Now).ToString(@"%d\d\ %h\h\ %m\m");

                default:
                    throw new NotImplementedException($"Command of type {dbEntry.Type} has not been implemented.");
            }
        }
    }
}