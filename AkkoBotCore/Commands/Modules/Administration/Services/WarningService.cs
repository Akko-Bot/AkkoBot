using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration.Services
{
    public class WarningService : AkkoCommandService
    {
        private readonly IServiceProvider _services;
        private readonly RoleService _roleService;
        private readonly UserPunishmentService _punishService;

        public WarningService(IServiceProvider services, RoleService roleService, UserPunishmentService punishService) : base(services)
        {
            _services = services;
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
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            // Create the entry
            var guildSettings = await db.GuildConfig.GetGuildWithWarningsAsync(context.Guild.Id, user.Id);

            // Make sure the server has warning punishments set up
            if (guildSettings.WarnPunishRel.Count == 0)
                guildSettings.AddDefaultWarnPunishments();

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

                if (guildSettings.WarnExpire > TimeSpan.Zero)
                    await CreateWarnTimerAsync(context, newNote);
            }

            await db.GuildConfig.CreateOccurrenceAsync(context.Guild, user.Id, occurrence);

            // Add the collection
            guildSettings.WarnRel.Add(newNote);

            // Update
            db.GuildConfig.Update(guildSettings);
            await db.SaveChangesAsync();

            return guildSettings;
        }

        /// <summary>
        /// Saves a warning to the database and carries out the server punishment, if applicable.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user to be warned.</param>
        /// <param name="warn">The warning to be added.</param>
        /// <returns><see langword="true"/> and the punishment type if a punishment was applied, <see langword="false"/> and <see langword="null"/> otherwise.</returns>
        public async Task<(bool, WarnPunishType?)> SaveWarnAsync(CommandContext context, DiscordUser user, string warn)
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
                    return (false, null);
                }
            }

            if (punishment is not null)
            {
                await ApplyPunishmentAsync(context, user, punishment, warn);
                return (true, punishment.Type);
            }

            return (false, null);
        }

        /// <summary>
        /// Gets the punishments of a Discord guild.
        /// </summary>
        /// <param name="server">Discord guild to get the punishments from.</param>
        /// <returns>A collection of server punishments.</returns>
        public async Task<List<WarnPunishEntity>> GetServerPunishmentsAsync(DiscordGuild server)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();
            var guildSettings = await db.GuildConfig.GetGuildWithPunishmentsAsync(server.Id);

            return guildSettings.WarnPunishRel;
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
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            var guildSettings = await db.GuildConfig.GetGuildWithPunishmentsAsync(server.Id);
            var punishment = guildSettings.WarnPunishRel.FirstOrDefault(x => x.WarnAmount == amount);
            var isUpdated = false;

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

            if (punishment is not null)
            {
                guildSettings.WarnPunishRel.Remove(punishment);
                isUpdated = true;
            }

            guildSettings.WarnPunishRel.Add(newPunishment);

            db.GuildConfig.Update(guildSettings);
            await db.SaveChangesAsync();

            return isUpdated;
        }

        /// <summary>
        /// Saves the expiration time of warnings for a given server and adds/removes its timers accordingly.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="time">How long the timers should last before being automatically deleted.</param>
        public async Task SaveWarnExpireAsync(CommandContext context, TimeSpan time)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            if (time < TimeSpan.Zero)
                time = TimeSpan.Zero;

            var guildSettings = db.GuildConfig.GetGuild(context.Guild.Id);
            guildSettings.WarnExpire = time;

            // Remove the timers before adding the new ones
            await RemoveWarnTimersAsync(context.Guild);

            if (time != TimeSpan.Zero)
                await CreateWarnTimersAsync(context, time);

            db.GuildConfig.Update(guildSettings);
            await db.SaveChangesAsync();
        }

        /// <summary>
        /// Removes a server punishment from the database.
        /// </summary>
        /// <param name="server">Discord guild to remove the punishment from.</param>
        /// <param name="amount">The amount required by the punishment.</param>
        /// <returns><see langword="true"/> if the punishment was removed, <see langword="false"/> if it didn't exist.</returns>
        public async Task<bool> RemoveWarnPunishmentAsync(DiscordGuild server, int amount)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            var guildSettings = await db.GuildConfig.GetGuildWithPunishmentsAsync(server.Id);
            var punishment = guildSettings.WarnPunishRel.FirstOrDefault(x => x.WarnAmount == amount);
            var isRemoved = false;

            if (punishment is not null)
            {
                guildSettings.WarnPunishRel.Remove(punishment);
                isRemoved = true;
            }

            db.GuildConfig.Update(guildSettings);
            await db.SaveChangesAsync();

            return isRemoved;
        }

        /// <summary>
        /// Removes a server punishment from the database.
        /// </summary>
        /// <param name="entity">The punishment to be removed.</param>
        /// <returns><see langword="true"/> if the punishment was removed, <see langword="false"/> otherwise.</returns>
        public async Task<bool> RemoveWarnPunishmentAsync(WarnPunishEntity entity)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            var guildSettings = await db.GuildConfig.GetGuildWithPunishmentsAsync(entity.GuildIdFK);
            var toRemove = guildSettings.WarnPunishRel.FirstOrDefault(x => x.WarnAmount == entity.WarnAmount);
            var isRemoved = guildSettings.WarnPunishRel.Remove(toRemove);

            db.GuildConfig.Update(guildSettings);
            await db.SaveChangesAsync();

            return isRemoved;
        }

        /// <summary>
        /// Removes a warning from the specified user at a given position.
        /// </summary>
        /// <param name="server">The Discord the warning is associated with.</param>
        /// <param name="user">The user whose warning needs to be removed.</param>
        /// <param name="id">The position of the warning, starting at 1.</param>
        /// <returns>The amount of removed entries, 0 if no entry was removed.</returns>
        public async Task<int> RemoveInfractionAsync(DiscordGuild server, DiscordUser user, int? id)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            var guildSettings = await db.GuildConfig.GetGuildWithWarningsAsync(server.Id, user.Id);

            if (id.HasValue)
            {
                var toRemove = guildSettings.WarnRel.FirstOrDefault(x => x.Id == id.Value);

                if (toRemove is null)
                    return 0;
                else
                    guildSettings.WarnRel.Remove(toRemove);
            }
            else
                guildSettings.WarnRel.Clear();

            db.GuildConfig.Update(guildSettings);
            return await db.SaveChangesAsync() - 1; // Remove "guild_config" itself.
        }

        /// <summary>
        /// Gets the notices or warnings from the specified user and the users who have issued the infractions.
        /// </summary>
        /// <param name="server">The Discord the warning is associated with.</param>
        /// <param name="user">The user whose warning are going to be listed.</param>
        /// <param name="type">The type of records to get.</param>
        /// <returns>A collection of notices or warnings and saved users.</returns>
        public async Task<(GuildConfigEntity, IEnumerable<DiscordUserEntity>)> GetInfractionsAsync(DiscordGuild server, DiscordUser user, WarnType type)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            var guildSettings = await db.GuildConfig.GetGuildWithWarningsAsync(server.Id, user.Id, type);
            var users = await db.DiscordUsers.GetAsync(x => guildSettings.WarnRel.Select(x => x.AuthorId).Contains(x.UserId));

            return (guildSettings, users);
        }

        /// <summary>
        /// Gets the notices and warnings from the specified user and the users who have issued the infractions.
        /// </summary>
        /// <param name="server">The Discord the warning is associated with.</param>
        /// <param name="user">The user whose warning are going to be listed.</param>
        /// <returns>A collection of notice/warnings and saved users.</returns>
        public async Task<(GuildConfigEntity, IEnumerable<DiscordUserEntity>)> GetInfractionsAsync(DiscordGuild server, DiscordUser user)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            var guildSettings = await db.GuildConfig.GetGuildWithWarningsAsync(server.Id, user.Id);
            var users = await db.DiscordUsers.GetAsync(x => guildSettings.WarnRel.Select(x => x.AuthorId).Contains(x.UserId));

            return (guildSettings, users);
        }

        /// <summary>
        /// Creates one timer for the given warning and saves it to the database and the cache.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="entry">The warning to create the timer for.</param>
        private async Task CreateWarnTimerAsync(CommandContext context, WarnEntity entry)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();
            var guildSettings = db.GuildConfig.GetGuild(context.Guild.Id);

            var newTimer = new TimerEntity(entry, guildSettings.WarnExpire);
            db.Timers.Update(newTimer);
            await db.SaveChangesAsync();

            db.Timers.Cache.AddOrUpdateByEntity(context.Client, newTimer);
        }

        /// <summary>
        /// Creates timers for each warning in a given Discord guild and saves them to the database and to the cache.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="time">The time the warnings should be removed after their creation.</param>
        private async Task CreateWarnTimersAsync(CommandContext context, TimeSpan time)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            var guildSettings = await db.GuildConfig.GetGuildWithWarningsAsync(context.Guild.Id);
            var toCreate = guildSettings.WarnRel.Select(warning => new TimerEntity(warning, time));

            foreach (var timer in toCreate)
            {
                db.Timers.Update(timer);
                await db.SaveChangesAsync();

                db.Timers.Cache.AddOrUpdateByEntity(context.Client, timer);
            }
        }

        /// <summary>
        /// Removes all warn timers from the database and the cache for a given Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild the timers are associated with.</param>
        /// <returns>The amount of timers removed from the database.</returns>
        private async Task<int> RemoveWarnTimersAsync(DiscordGuild server)
        {
            var db = base.Scope.ServiceProvider.GetService<IUnitOfWork>();

            //Remove the timers
            var toRemove = db.Timers.Table
                .Where(x => x.Type == TimerType.TimedWarn && x.GuildId == server.Id)
                .ToArray();

            foreach (var timer in toRemove)
                db.Timers.Cache.TryRemove(timer.Id);

            db.Timers.DeleteRange(toRemove);
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
                    throw new NotImplementedException("Invalid punishment type.");
            }
        }
    }
}