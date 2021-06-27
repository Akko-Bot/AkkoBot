using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="GuildConfigEntity"/> objects.
    /// </summary>
    public class GuildConfigService : ICommandService
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
        /// Gets all registered localed.
        /// </summary>
        /// <returns>A collection of registered locales.</returns>
        public IReadOnlyCollection<string> GetLocales()
            => _localizer.Locales;

        /// <summary>
        /// Gets or sets the specified guild setting.
        /// </summary>
        /// <typeparam name="T">The type of the setting to be returned.</typeparam>
        /// <param name="server">The target guild.</param>
        /// <param name="selector">A method to set the property.</param>
        /// <returns>The modified setting.</returns>
        public async Task<T> SetPropertyAsync<T>(DiscordGuild server, Func<GuildConfigEntity, T> selector)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);
            var dbGuild = _dbCache.Guilds.GetOrAdd(server.Id, sid => new GuildConfigEntity() { GuildId = sid });
            var result = selector(dbGuild);

            db.GuildConfig.Update(dbGuild);
            await db.SaveChangesAsync();

            return result;
        }

        /// <summary>
        /// Returns the settings of the specified Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild.</param>
        /// <returns>The guild settings.</returns>
        public GuildConfigEntity GetGuildSettings(DiscordGuild server)
        {
            _dbCache.Guilds.TryGetValue(server.Id, out var dbGuild);
            return dbGuild;
        }
    }
}