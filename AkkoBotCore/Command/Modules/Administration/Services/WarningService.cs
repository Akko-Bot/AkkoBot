using System.Linq;
using System;
using AkkoBot.Command.Abstractions;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using System.Collections.Generic;
using DSharpPlus.CommandsNext;

namespace AkkoBot.Command.Modules.Administration.Services
{
    public class WarningService : ICommandService
    {
        private readonly IServiceProvider _services;
        private readonly RoleService _roleService;
        private readonly UserPunishmentService _punishService;

        public WarningService(IServiceProvider services, RoleService roleService, UserPunishmentService punishService)
        {
            _services = services;
            _roleService = roleService;
            _punishService = punishService;
        }

        /// <summary>
        /// Saves a note to the database.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The user the note will be associated with.</param>
        /// <param name="note">The note to be added.</param>
        /// <param name="type">The type of note to be added.</param>
        /// <returns>The saved guild settings.</returns>
        public async Task<GuildConfigEntity> SaveRecord(CommandContext context, DiscordUser user, string note, WarnType? type = null)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            if (!type.HasValue)
                type = WarnType.Notice;

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
                Type = type.Value,
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
                occurrence.Warnings = 1;

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
        public async Task<(bool, WarnPunishType?)> SaveWarn(CommandContext context, DiscordUser user, string warn)
        {
            var guildSettings = await SaveRecord(context, user, warn, WarnType.Warning);

            var punishment = guildSettings.WarnPunishRel
                .FirstOrDefault(x => x.WarnAmount == guildSettings.WarnRel.Where(x => x.Type == WarnType.Warning).Count());

            if (punishment is not null)
            {
                await ApplyPunishment(context, user, punishment, warn);
                return (true, punishment.Type);
            }

            return (false, null);
        }

        /// <summary>
        /// Removes a warning from the specified user at a given position.
        /// </summary>
        /// <param name="server">The Discord the warning is associated with.</param>
        /// <param name="user">The user whose warning needs to be removed.</param>
        /// <param name="id">The position of the warning, starting at 1.</param>
        /// <returns>The amount of removed entries, 0 if no entry was removed.</returns>
        public async Task<int> RemoveRegister(DiscordGuild server, DiscordUser user, int? id)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

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
        /// Gets the warnings and users who has issued the warnings.
        /// </summary>
        /// <param name="server">The Discord the warning is associated with.</param>
        /// <param name="user">The user whose warning are going to be listed.</param>
        /// <param name="type">The type of records to get.</param>
        /// <returns>A collection of notices or warnings and saved users.</returns>
        public async Task<(GuildConfigEntity, IEnumerable<DiscordUserEntity>)> GetRecords(DiscordGuild server, DiscordUser user, WarnType type)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var guildSettings = await db.GuildConfig.GetGuildWithWarningsAsync(server.Id, user.Id, type);
            var users = await db.DiscordUsers.GetAsync(x => guildSettings.WarnRel.Select(x => x.AuthorId).Contains(x.UserId));

            return (guildSettings, users);
        }

        /// <summary>
        /// Gets the warnings and users who has issued the warnings.
        /// </summary>
        /// <param name="server">The Discord the warning is associated with.</param>
        /// <param name="user">The user whose warning are going to be listed.</param>
        /// <returns>A collection of notice/warnings and saved users.</returns>
        public async Task<(GuildConfigEntity, IEnumerable<DiscordUserEntity>)> GetRecords(DiscordGuild server, DiscordUser user)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var guildSettings = await db.GuildConfig.GetGuildWithWarningsAsync(server.Id, user.Id);
            var users = await db.DiscordUsers.GetAsync(x => guildSettings.WarnRel.Select(x => x.AuthorId).Contains(x.UserId));

            return (guildSettings, users);
        }

        /// <summary>
        /// Applies a punishment to the specified user.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="user">The Discord user to be punished.</param>
        /// <param name="punishment">The punishment to be carried out.</param>
        /// <param name="reason">The reason for the punishment.</param>
        /// <exception cref="NotImplementedException">Occurs when the specified punishment has no implementation.</exception>
        private async Task ApplyPunishment(CommandContext context, DiscordUser user, WarnPunishEntity punishment, string reason)
        {
            var warnString = context.FormatLocalized("infraction");
            var member = (DiscordMember)user;

            switch (punishment.Type)
            {
                case WarnPunishType.Mute:
                    if (member is null) break;
                    var muteRole = await _roleService.FetchMuteRoleAsync(context.Guild);
                    await _roleService.MuteUserAsync(context, muteRole, member, TimeSpan.FromHours(1), warnString + " | " + reason);
                    break;

                case WarnPunishType.Kick:
                    if (member is null) break;
                    await _punishService.KickUser(context.Guild, member, warnString + " | " + reason);
                    break;

                case WarnPunishType.Softban:
                    await _punishService.SoftbanUser(context.Guild, user.Id, 1, warnString + " | " + reason);
                    break;

                case WarnPunishType.Ban:
                    await _punishService.BanUser(context.Guild, user.Id, 1, warnString + " | " + reason);
                    break;

                default:
                    throw new NotImplementedException("Invalid punishment type.");
            }
        }
    }
}