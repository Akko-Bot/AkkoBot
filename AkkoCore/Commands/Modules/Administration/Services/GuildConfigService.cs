using AkkoCore.Commands.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Queries;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="GuildConfigEntity"/> objects.
    /// </summary>
    public sealed class GuildConfigService : ICommandService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;
        private readonly ILocalizer _localizer;

        public GuildConfigService(IServiceScopeFactory scopeFactory, IDbCache dbCacher, ILocalizer localizer)
        {
            _scopeFactory = scopeFactory;
            _dbCache = dbCacher;
            _localizer = localizer;
        }

        /// <summary>
        /// Checks if the specified locale is available and returns it if so.
        /// </summary>
        /// <param name="locale">The locale to check for.</param>
        /// <param name="match">The locale if found, <see langword="null"/> otherwise.</param>
        /// <returns><see langword="true"/> if a match is found, <see langword="false"/> otherwise.</returns>
        public bool IsLocaleRegistered(string locale, out string match)
            => _localizer.Locales.Equals(locale, StringComparison.InvariantCultureIgnoreCase, out match);

        /// <summary>
        /// Gets all registered locales.
        /// </summary>
        /// <returns>A collection of registered locales.</returns>
        public IReadOnlyCollection<string> GetLocales()
            => _localizer.Locales;

        /// <summary>
        /// Sets the specified guild setting.
        /// </summary>
        /// <typeparam name="T">The type of the setting to be returned.</typeparam>
        /// <param name="server">The target guild.</param>
        /// <param name="selector">A method to set the property.</param>
        /// <returns>The modified setting.</returns>
        /// <exception cref="ArgumentNullException">Occurs when one of the arguments is <see langword="null"/>.</exception>
        public async Task<T> SetPropertyAsync<T>(DiscordGuild server, Func<GuildConfigEntity, T> selector)
        {
            if (server is null || selector is null)
                throw new ArgumentNullException(server is null ? nameof(server) : nameof(selector), "Argument cannot be null.");

            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            if (!_dbCache.Guilds.TryGetValue(server.Id, out var dbGuild))
                dbGuild = await db.GuildConfig.IncludeCacheable().FirstOrDefaultAsync(x => x.GuildId == server.Id) ?? new GuildConfigEntity() { GuildId = server.Id };

            db.GuildConfig.Upsert(dbGuild);
            var result = selector(dbGuild);
            await db.SaveChangesAsync();

            // Update the cache
            _dbCache.Guilds.AddOrUpdate(server.Id, dbGuild, (_, _) => dbGuild);

            return result;
        }

        /// <summary>
        /// Returns the settings of the specified Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild.</param>
        /// <returns>The guild settings.</returns>
        /// <exception cref="ArgumentNullException">Occurs when <paramref name="server"/> is <see langword="null"/>.</exception>
        public GuildConfigEntity GetGuildSettings(DiscordGuild server)
        {
            if (server is null)
                throw new ArgumentNullException(nameof(server), "Discord guild cannot be null.");

            _dbCache.Guilds.TryGetValue(server.Id, out var dbGuild);
            return dbGuild;
        }
    }
}