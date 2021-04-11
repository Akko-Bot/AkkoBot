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

namespace AkkoBot.Commands.Modules.Administration.Services
{
    public class WarningService : ICommandService
    {
        private readonly IServiceProvider _services;
        private readonly IDbCache _dbCache;
        private readonly RoleService _roleService;
        private readonly UserPunishmentService _punishService;

        public WarningService(IServiceProvider services, IDbCache dbCache, RoleService roleService, UserPunishmentService punishService)
        {
            _services = services;
            _dbCache = dbCache;
            _roleService = roleService;
            _punishService = punishService;
        }

        /// <summary>
        /// Saves an infraction to the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user the note will be associated with.</param>
        /// <param name="note">The note to be added.</param>
        /// <param name="type">The type of note to be added.</param>
        /// <returns>The saved guild settings.</returns>
        public async Task<GuildConfigEntity> SaveInfractionAsync(CommandContext context, DiscordUser user, string note, WarnType type = WarnType.Notice)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            // Create the entry
            var dbGuild = await db.GuildConfig.GetGuildWithWarningsAsync(context.Guild.Id, user.Id);

            // Make sure the server has warning punishments set up
            if (dbGuild.WarnPunishRel.Count == 0)
                dbGuild.AddDefaultWarnPunishments();

            // Create the warn entry
            var newNote = new WarnEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserId = user.Id,
                AuthorId = context.Member.Id,
                Type = type,
                WarningText = note
            };

            // Create the occurrence
            var occurrence = new OccurrenceEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserId = user.Id
            };

            if (type == WarnType.Notice)
                occurrence.Notices = 1;
            else
            {
                occurrence.Warnings = 1;

                if (dbGuild.WarnExpire > TimeSpan.Zero)
                    await CreateWarnTimerAsync(context, newNote);
            }

            // Add to the collections
            if (dbGuild.OccurrenceRel.Count == 0)
                dbGuild.OccurrenceRel.Add(occurrence);
            else
                dbGuild.OccurrenceRel[0] += occurrence;

            dbGuild.WarnRel.Add(newNote);

            // Update
            db.GuildConfig.Update(dbGuild);
            await db.SaveChangesAsync();

            return dbGuild;
        }

        /// <summary>
        /// Saves a warning to the database and carries out the server punishment, if applicable.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user to be warned.</param>
        /// <param name="warn">The warning to be added.</param>
        /// <returns>The punishment type if a punishment was applied, <see langword="null"/> otherwise.</returns>
        public async Task<WarnPunishType?> SaveWarnAsync(CommandContext context, DiscordUser user, string warn)
        {
            var guildSettings = await SaveInfractionAsync(context, user, warn, WarnType.Warning);

            var punishment = guildSettings.WarnPunishRel
                .FirstOrDefault(x => x.WarnAmount == guildSettings.WarnRel.Where(x => x.Type == WarnType.Warning).Count());

            if (punishment?.PunishRoleId is not null)
            {
                // If punishment role doesn't exist anymore, delete the punishment from the database
                if (!context.Guild.Roles.TryGetValue(punishment.PunishRoleId.Value, out _))
                {
                    await RemoveWarnPunishmentAsync(punishment);
                    return null;
                }
            }

            if (punishment is not null)
            {
                await ApplyPunishmentAsync(context, user, punishment, warn);
                return punishment.Type;
            }

            return null;
        }

        /// <summary>
        /// Gets the punishments of a Discord guild.
        /// </summary>
        /// <param name="server">Discord guild to get the punishments from.</param>
        /// <returns>A collection of server punishments.</returns>
        public async Task<WarnPunishEntity[]> GetServerPunishmentsAsync(DiscordGuild server)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);
            var punishments = await db.WarnPunishments.AsNoTracking()
                .Where(x => x.GuildIdFK == server.Id)
                .ToArrayAsync();

            return punishments;
        }

        /// <summary>
        /// Saves a new server punishment to the database.
        /// </summary>
        /// <param name="server">The Discord guild the punishment applies for.</param>
        /// <param name="amount">The amount of warnings required to trigger the punishment.</param>
        /// <param name="type">The type of punishment that should be issued.</param>
        /// <param name="interval">If the punishment is temporary, for how long it should be.</param>
        /// <returns><see langword="true"/> if the punishment was saved, <see langword="false"/> if it was updated.</returns>
        public async Task<bool> SaveWarnPunishmentAsync(DiscordGuild server, int amount, WarnPunishType type, DiscordRole role, TimeSpan? interval)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var punishment = await db.WarnPunishments.FirstOrDefaultAsync(x => x.GuildIdFK == server.Id && x.WarnAmount == amount);

            var newPunishment = new WarnPunishEntity()
            {
                GuildIdFK = server.Id,
                WarnAmount = amount,
                Type = type,
                Interval =
                    (type is WarnPunishType.Mute
                    or WarnPunishType.Ban
                    or WarnPunishType.AddRole
                    or WarnPunishType.RemoveRole)
                        ? interval
                        : null,

                PunishRoleId = role?.Id
            };

            var isCreated = db.Upsert(newPunishment).State is EntityState.Added;
            await db.SaveChangesAsync();

            return isCreated;
        }

        /// <summary>
        /// Saves the expiration time of warnings for a given server and adds/removes its timers accordingly.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="time">How long the timers should last before being automatically deleted.</param>
        public async Task SaveWarnExpireAsync(CommandContext context, TimeSpan time)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            if (time < TimeSpan.Zero)
                time = TimeSpan.Zero;

            _dbCache.Guilds.TryGetValue(context.Guild.Id, out var dbGuild);
            dbGuild.WarnExpire = time;

            // Remove the timers before adding the new ones
            await RemoveWarnTimersAsync(context.Guild);

            if (time != TimeSpan.Zero)
                await CreateWarnTimersAsync(context, time);

            db.GuildConfig.Update(dbGuild);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Removes a server punishment from the database.
        /// </summary>
        /// <param name="server">Discord guild to remove the punishment from.</param>
        /// <param name="amount">The amount required by the punishment.</param>
        /// <returns><see langword="true"/> if the punishment was removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveWarnPunishmentAsync(DiscordGuild server, int amount)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var punishment = await db.WarnPunishments.FirstOrDefaultAsync(x => x.GuildIdFK == server.Id && x.WarnAmount == amount);

            if (punishment is not null)
                db.Remove(punishment);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Removes a server punishment from the database.
        /// </summary>
        /// <param name="entity">The punishment to be removed.</param>
        /// <returns><see langword="true"/> if the punishment was removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveWarnPunishmentAsync(WarnPunishEntity entity)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);
            db.Remove(entity);

            return await db.SaveChangesAsync() is not 0;
        }

        /// <summary>
        /// Removes an infraction from the specified user at a given position.
        /// </summary>
        /// <param name="server">The Discord the infraction is associated with.</param>
        /// <param name="user">The user whose infractions need to be removed.</param>
        /// <param name="id">The database ID of the infraction.</param>
        /// <returns>The amount of removed entries, 0 if no entry was removed.</returns>
        public async Task<int> RemoveInfractionAsync(DiscordGuild server, DiscordUser user, int? id)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var warnings = db.Warnings.Where(x => x.GuildIdFK == server.Id && x.UserId == user.Id);

            if (id.HasValue)
            {
                var toRemove = await warnings.FirstOrDefaultAsync(x => x.Id == id.Value);

                if (toRemove is null)
                    return 0;
                else
                    db.Remove(toRemove);
            }
            else
                db.RemoveRange(warnings);

            return await db.SaveChangesAsync();
        }

        /// <summary>
        /// Gets the notices or warnings from the specified user and the users who have issued the infractions.
        /// </summary>
        /// <param name="server">The Discord the warning is associated with.</param>
        /// <param name="user">The user whose warning are going to be listed.</param>
        /// <param name="type">The type of records to get.</param>
        /// <returns>A collection of notices or warnings and saved users.</returns>
        public async Task<(GuildConfigEntity, DiscordUserEntity)> GetInfractionsAsync(DiscordGuild server, DiscordUser user, WarnType type)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await db.GuildConfig.GetGuildWithWarningsAsync(server.Id, user.Id, type);
            var dbUser = await db.DiscordUsers.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            return (dbGuild, dbUser);
        }

        /// <summary>
        /// Gets the notices and warnings from the specified user and the users who have issued the infractions.
        /// </summary>
        /// <param name="server">The Discord the warning is associated with.</param>
        /// <param name="user">The user whose warning are going to be listed.</param>
        /// <returns>A collection of notice/warnings and saved users.</returns>
        public async Task<(GuildConfigEntity, DiscordUserEntity)> GetInfractionsAsync(DiscordGuild server, DiscordUser user)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await db.GuildConfig.GetGuildWithWarningsAsync(server.Id, user.Id);
            var dbUser = await db.DiscordUsers.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            return (dbGuild, dbUser);
        }

        /// <summary>
        /// Creates one timer for the given warning and saves it to the database and the cache.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="entry">The warning to create the timer for.</param>
        private async Task CreateWarnTimerAsync(CommandContext context, WarnEntity entry)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            _dbCache.Guilds.TryGetValue(context.Guild.Id, out var dbGuild);

            var newTimer = new TimerEntity(entry, dbGuild.WarnExpire);
            db.Timers.Update(newTimer);
            await db.SaveChangesAsync();

            _dbCache.Timers.AddOrUpdateByEntity(context.Client, newTimer);
        }

        /// <summary>
        /// Creates timers for each warning in a given Discord guild and saves them to the database and to the cache.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="time">The time the warnings should be removed after their creation.</param>
        private async Task CreateWarnTimersAsync(CommandContext context, TimeSpan time)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            var warnings = await db.Warnings
                .Where(x => x.GuildIdFK == context.Guild.Id)
                .ToArrayAsync();

            foreach (var timer in warnings.Select(warning => new TimerEntity(warning, time)))
            {
                db.Timers.Update(timer);
                await db.SaveChangesAsync();

                _dbCache.Timers.AddOrUpdateByEntity(context.Client, timer);
            }
        }

        /// <summary>
        /// Removes all warn timers from the database and the cache for a given Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild the timers are associated with.</param>
        /// <returns>The amount of timers removed from the database.</returns>
        private async Task<int> RemoveWarnTimersAsync(DiscordGuild server)
        {
            var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            //Remove the timers
            var toRemove = db.Timers
                .Where(x => x.Type == TimerType.TimedWarn && x.GuildId == server.Id)
                .ToArray();

            foreach (var timer in toRemove)
                _dbCache.Timers.TryRemove(timer.Id);

            db.Timers.RemoveRange(toRemove);
            return await db.SaveChangesAsync();
        }

        /// <summary>
        /// Applies a punishment to the specified user.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The Discord user to be punished.</param>
        /// <param name="punishment">The punishment to be carried out.</param>
        /// <param name="reason">The reason for the punishment.</param>
        /// <exception cref="NotImplementedException">Occurs when the specified punishment has no implementation.</exception>
        private async Task ApplyPunishmentAsync(CommandContext context, DiscordUser user, WarnPunishEntity punishment, string reason)
        {
            var warnString = context.FormatLocalized("infraction");
            var member = (DiscordMember)user;

            switch (punishment.Type)
            {
                case WarnPunishType.Mute:
                    if (member is null) break;
                    var muteRole = await _roleService.FetchMuteRoleAsync(context.Guild);
                    await _roleService.MuteUserAsync(context, muteRole, member, punishment.Interval ?? TimeSpan.Zero, warnString + " | " + reason);
                    break;

                case WarnPunishType.Kick:
                    if (member is null) break;
                    await _punishService.KickUser(context.Guild, member, warnString + " | " + reason);
                    break;

                case WarnPunishType.Softban:
                    await _punishService.SoftbanUser(context.Guild, user.Id, 1, warnString + " | " + reason);
                    break;

                case WarnPunishType.Ban:
                    if (punishment.Interval.HasValue)
                        await _punishService.TimedBanAsync(context, punishment.Interval.Value, user.Id, warnString + " | " + reason);
                    else
                        await _punishService.BanUser(context.Guild, user.Id, 1, warnString + " | " + reason);

                    break;

                case WarnPunishType.AddRole:
                case WarnPunishType.RemoveRole:
                    if (member is null || !context.Guild.Roles.TryGetValue(punishment.PunishRoleId ?? default, out var punishRole))
                        break;

                    if (punishment.Interval.HasValue)
                        await _punishService.TimedRolePunish(context, punishment.Type, punishment.Interval.Value, member, punishRole, warnString + " | " + reason);
                    else if (punishment.Type == WarnPunishType.AddRole)
                        await member.GrantRoleAsync(punishRole, warnString + " | " + reason);
                    else
                        await member.RevokeRoleAsync(punishRole, warnString + " | " + reason);

                    break;

                default:
                    throw new NotImplementedException($"No punishment of type \"{punishment.Type}\" has been implemented.");
            }
        }
    }
}