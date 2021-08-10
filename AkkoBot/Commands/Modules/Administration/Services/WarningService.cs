using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Database.Queries;
using AkkoDatabase;
using AkkoDatabase.Entities;
using AkkoDatabase.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration.Services
{
    public class WarningService : ICommandService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IAkkoCache _akkoCache;
        private readonly IDbCache _dbCache;
        private readonly RoleService _roleService;
        private readonly UserPunishmentService _punishmentService;

        public WarningService(IServiceScopeFactory scopeFactory, IAkkoCache akkoCache, IDbCache dbCache, RoleService roleService, UserPunishmentService punishmentService)
        {
            _scopeFactory = scopeFactory;
            _akkoCache = akkoCache;
            _dbCache = dbCache;
            _roleService = roleService;
            _punishmentService = punishmentService;
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
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var dbGuild = await _dbCache.GetDbGuildAsync(context.Guild.Id);
            var dbOccurrence = await db.Occurrences
                .FirstOrDefaultAsyncEF(x => x.UserId == user.Id && x.GuildIdFK == context.Guild.Id);

            // Make sure the guild has warning punishments set up
            if (!await db.WarnPunishments.AnyAsyncEF(x => x.GuildIdFK == context.Guild.Id))
                await db.BulkCopyAsync(CreateDefaultPunishments(context.Guild.Id));

            // Create the warn entry
            var newWarning = new WarnEntity()
            {
                GuildIdFK = context.Guild.Id,
                UserIdFK = user.Id,
                AuthorId = context.Member.Id,
                Type = type,
                WarningText = note
            };

            // If the guild has temporary warnings, create the timer for it
            if (type is WarnType.Warning)
                newWarning.TimerIdFK = (await CreateWarnTimerAsync(context, newWarning)).Id;

            db.Warnings.Add(newWarning);

            // Create or update the occurrence
            if (dbOccurrence is null)
            {
                db.Occurrences.Add(
                    new OccurrenceEntity()
                    {
                        GuildIdFK = context.Guild.Id,
                        UserId = user.Id,
                        Notices = (type is WarnType.Notice) ? 1 : 0,
                        Warnings = (type is WarnType.Warning) ? 1 : 0
                    }
                );
            }
            else
            {
                await db.Occurrences.UpdateAsync(
                    x => x.Id == dbOccurrence.Id,
                    (type == WarnType.Notice)
                        ? y => new OccurrenceEntity() { Notices = y.Notices + 1 }
                        : y => new OccurrenceEntity() { Warnings = y.Warnings + 1 }
                );
            }

            // Add the entries
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
        public async Task<PunishmentType?> SaveWarnAsync(CommandContext context, DiscordUser user, string warn)
        {
            var guildSettings = await SaveInfractionAsync(context, user, warn, WarnType.Warning);

            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var punishment = await db.WarnPunishments
                .Where(x => x.WarnAmount == db.Warnings.Count(y => y.GuildIdFK == context.Guild.Id && y.UserIdFK == user.Id && y.Type == WarnType.Warning))
                .Select(x => new WarnPunishEntity() { Id = x.Id, Type = x.Type, PunishRoleId = x.PunishRoleId, Interval = x.Interval })
                .FirstOrDefaultAsyncEF();

            context.Guild.Roles.TryGetValue(punishment?.PunishRoleId ?? default, out var punishRole);

            // If punishment role doesn't exist anymore, delete the punishment from the database
            if (punishment?.PunishRoleId is not null && punishRole is null)
            {
                await db.WarnPunishments.DeleteAsync(punishment);
                return null;
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
        public async Task<IReadOnlyCollection<WarnPunishEntity>> GetServerPunishmentsAsync(DiscordGuild server)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);
            var punishments = await db.WarnPunishments
                .Where(x => x.GuildIdFK == server.Id)
                .ToArrayAsyncEF();

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
        public async Task<bool> SaveWarnPunishmentAsync(DiscordGuild server, int amount, PunishmentType type, DiscordRole role, TimeSpan? interval)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            var punishment = await db.WarnPunishments.FirstOrDefaultAsyncEF(x => x.GuildIdFK == server.Id && x.WarnAmount == amount);

            var newPunishment = new WarnPunishEntity()
            {
                GuildIdFK = server.Id,
                WarnAmount = amount,
                Type = type,
                Interval =
                    (type is PunishmentType.Mute
                    or PunishmentType.Ban
                    or PunishmentType.AddRole
                    or PunishmentType.RemoveRole)
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
        /// <returns><see langword="true"/> if the expire time has been updates, <see langword="false"/> otherwise.</returns>
        public async Task<bool> SaveWarnExpireAsync(CommandContext context, TimeSpan time)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            if (time < TimeSpan.Zero)
                time = TimeSpan.Zero;

            _dbCache.Guilds.TryGetValue(context.Guild.Id, out var dbGuild);
            dbGuild.WarnExpire = time;

            // Remove Enable or disable the timers
            await UpdateWarnTimersAsync(context.Client, dbGuild);

            return await db.GuildConfig
                .UpdateAsync(
                    x => x.Id == dbGuild.Id,
                    _ => new GuildConfigEntity() { WarnExpire = time }
                ) is not 0;
        }

        /// <summary>
        /// Removes a server punishment from the database.
        /// </summary>
        /// <param name="server">Discord guild to remove the punishment from.</param>
        /// <param name="amount">The amount required by the punishment.</param>
        /// <returns><see langword="true"/> if the punishment was removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveWarnPunishmentAsync(DiscordGuild server, int amount)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);
            return await db.WarnPunishments.DeleteAsync(x => x.GuildIdFK == server.Id && x.WarnAmount == amount) is not 0;
        }

        /// <summary>
        /// Removes an infraction from the specified user at a given position.
        /// </summary>
        /// <param name="server">The Discord the infraction is associated with.</param>
        /// <param name="user">The user whose infractions need to be removed.</param>
        /// <param name="id">The database ID of the infraction.</param>
        /// <returns>The amount of infractions removed, 0 if no infraction was removed.</returns>
        public async Task<int> RemoveInfractionAsync(DiscordGuild server, DiscordUser user, int? id)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);
            int result;

            if (id.HasValue)
            {
                var timerId = await db.Warnings
                    .Where(x => x.Id == id.Value)
                    .Select(x => x.TimerIdFK)
                    .FirstOrDefaultAsyncEF();

                result = await db.Warnings.DeleteAsync(x => x.Id == id);

                await db.Timers.DeleteAsync(x => x.Id == timerId);
            }
            else
            {
                result = await db.Warnings.DeleteAsync(x => x.GuildIdFK == server.Id && x.UserIdFK == user.Id);

                await db.Timers.DeleteAsync(x => x.GuildIdFK == server.Id && x.UserIdFK == user.Id && x.Type == TimerType.TimedWarn);
            }

            return result;
        }

        /// <summary>
        /// Gets the notices and warnings from the specified user and the users who have issued the infractions.
        /// </summary>
        /// <param name="server">The Discord the infraction is associated with.</param>
        /// <param name="user">The user whose infractions are going to be listed.</param>
        /// <returns>A collection of infractions and the name of its authors.</returns>
        public async Task<IReadOnlyCollection<(string, WarnEntity)>> GetInfractionsAsync(DiscordGuild server, DiscordUser user)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            // Join warnings with user who issued them
            var infractions = await db.Warnings
                .FullJoin(
                    db.DiscordUsers,
                    (infraction, dbUser) => infraction.AuthorId == dbUser.UserId,   // Predicate
                    (infraction, dbUser) => new                                     // Selector
                    {
                        User = new DiscordUserEntity() { UserId = dbUser.UserId, Username = dbUser.Username, Discriminator = dbUser.Discriminator },
                        Infraction = infraction
                    }
                )
                .Where(x => x.Infraction.GuildIdFK == server.Id && x.Infraction.UserIdFK == user.Id)
                .OrderByDescending(x => x.Infraction.Id)
                .ToArrayAsyncLinqToDB();

            return infractions
                .Select(x => (infractions.Select(y => y.User).FirstOrDefault(y => y.UserId == x.Infraction.AuthorId)?.FullName ?? "Unknown", x.Infraction))
                .ToArray();
        }

        /// <summary>
        /// Gets the notices or warnings from the specified user and the users who have issued the infractions.
        /// </summary>
        /// <param name="server">The Discord the infraction is associated with.</param>
        /// <param name="user">The user whose infractions are going to be listed.</param>
        /// <param name="type">The type of records to get.</param>
        /// <returns>A collection of infractions and the name of its authors.</returns>
        public async Task<IReadOnlyCollection<(string, WarnEntity)>> GetInfractionsAsync(DiscordGuild server, DiscordUser user, WarnType type)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            // Join warnings with user who issued them
            var infractions = await db.Warnings
                .FullJoin(
                    db.DiscordUsers,
                    (infraction, dbUser) => infraction.AuthorId == dbUser.UserId,   // Predicate
                    (infraction, dbUser) => new                                     // Selector
                    {
                        User = new DiscordUserEntity() { UserId = dbUser.UserId, Username = dbUser.Username, Discriminator = dbUser.Discriminator },
                        Infraction = infraction
                    }
                )
                .Where(x => x.Infraction.GuildIdFK == server.Id && x.Infraction.UserIdFK == user.Id && x.Infraction.Type == type)
                .OrderByDescending(x => x.Infraction.Id)
                .ToArrayAsyncLinqToDB();

            return infractions
                .Select(x => (infractions.Select(y => y.User).FirstOrDefault(y => y.UserId == x.Infraction.AuthorId)?.FullName ?? "Unknown", x.Infraction))
                .ToArray();
        }

        /// <summary>
        /// Gets the occurrences of the specified user in a given Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild where the occurrences took place.</param>
        /// <param name="user">The user whose occurrences are going to be listed.</param>
        /// <returns>The occurrences of the user.</returns>
        public async Task<OccurrenceEntity> GetUserOccurrencesAsync(DiscordGuild server, DiscordUser user)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            return await db.Occurrences
                .FirstOrDefaultAsyncEF(x => x.GuildIdFK == server.Id && x.UserId == user.Id) ?? new();
        }

        /// <summary>
        /// Creates one timer for the given warning and saves it to the database and the cache.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="entry">The warning to create the timer for.</param>
        /// <returns>The created database timer.</returns>
        private async Task<TimerEntity> CreateWarnTimerAsync(CommandContext context, WarnEntity entry)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            _dbCache.Guilds.TryGetValue(context.Guild.Id, out var dbGuild);

            var newTimer = new TimerEntity(entry, dbGuild.WarnExpire);
            db.Timers.Add(newTimer);
            await db.SaveChangesAsync();

            _akkoCache.Timers.AddOrUpdateByEntity(context.Client, newTimer);
            return newTimer;
        }

        /// <summary>
        /// Updates the timers associated with the warnings in the specified guild.
        /// </summary>
        /// <param name="client">The Discord client with access to the guild.</param>
        /// <param name="dbGuild">The guild settings.</param>
        /// <returns>The amount of updated timers.</returns>
        private async Task<int> UpdateWarnTimersAsync(DiscordClient client, GuildConfigEntity dbGuild)
        {
            var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

            //Remove the timers
            var updates = 0;

            var timers = await db.Timers
                .Include(x => x.WarnRel)
                .Where(x => x.GuildIdFK == dbGuild.GuildId && x.Type == TimerType.TimedWarn)
                .Select(x => new TimerEntity(x) { WarnRel = new() { DateAdded = x.WarnRel.DateAdded } })
                .ToArrayAsyncEF();

            foreach (var timer in timers)
            {
                timer.IsActive = dbGuild.WarnExpire != TimeSpan.Zero;

                if (!timer.IsActive)
                    _akkoCache.Timers.TryRemove(timer.Id);
                else
                {
                    timer.ElapseAt = timer.WarnRel.DateAdded.Add(dbGuild.WarnExpire);
                    timer.Interval = dbGuild.WarnExpire;

                    _akkoCache.Timers.AddOrUpdateByEntity(client, timer);
                }

                updates += await db.Timers.UpdateAsync(
                    x => x.Id == timer.Id,
                    _ => new TimerEntity() { Interval = timer.Interval, ElapseAt = timer.ElapseAt, IsActive = timer.IsActive }
                );
            }

            return updates;
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
            using var scope = _scopeFactory.CreateScope();
            var warnString = context.FormatLocalized("infraction");
            var member = user as DiscordMember;

            switch (punishment.Type)
            {
                case PunishmentType.Mute:
                    if (member is null) break;

                    var muteRole = await _roleService.FetchMuteRoleAsync(context.Guild);
                    await _roleService.MuteUserAsync(context, muteRole, member, punishment.Interval ?? TimeSpan.Zero, warnString + " | " + reason);
                    break;

                case PunishmentType.Kick:
                    if (member is null) break;

                    await _punishmentService.KickUserAsync(context, member, warnString + " | " + reason);
                    break;

                case PunishmentType.Softban:
                    await _punishmentService.SoftbanUserAsync(context, user.Id, 1, warnString + " | " + reason);
                    break;

                case PunishmentType.Ban:
                    if (punishment.Interval.HasValue)
                        await _punishmentService.TimedBanAsync(context, punishment.Interval.Value, user.Id, warnString + " | " + reason);
                    else
                        await _punishmentService.BanUserAsync(context, user.Id, 1, warnString + " | " + reason);

                    break;

                case PunishmentType.AddRole:
                case PunishmentType.RemoveRole:
                    if (member is null || !context.Guild.Roles.TryGetValue(punishment.PunishRoleId ?? default, out var punishRole))
                        break;

                    if (punishment.Interval.HasValue)
                        await _punishmentService.TimedRolePunishAsync(context, punishment.Type, punishment.Interval.Value, member, punishRole, warnString + " | " + reason);
                    else if (punishment.Type == PunishmentType.AddRole)
                        await member.GrantRoleAsync(punishRole, warnString + " | " + reason);
                    else
                        await member.RevokeRoleAsync(punishRole, warnString + " | " + reason);

                    break;

                default:
                    throw new NotImplementedException($"No punishment of type \"{punishment.Type}\" has been implemented.");
            }
        }

        /// <summary>
        /// Creates the default server punishments for a given Discord guild.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <remarks>Kick at 3 warnings, ban at 5 warnings.</remarks>
        /// <returns>A collection of punishments.</returns>
        private IReadOnlyCollection<WarnPunishEntity> CreateDefaultPunishments(ulong sid)
        {
            return new WarnPunishEntity[]
            {
                new WarnPunishEntity()
                {
                    GuildIdFK = sid,
                    WarnAmount = 3,
                    Type = PunishmentType.Kick
                },
                new WarnPunishEntity()
                {
                    GuildIdFK = sid,
                    WarnAmount = 5,
                    Type = PunishmentType.Ban
                }
            };
        }
    }
}