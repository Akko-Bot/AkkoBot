using AkkoCore.Commands.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Queries;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="AutoSlowmodeEntity"/> objects.
    /// </summary>
    public class AutoSlowmodeService : ICommandService
    {
        private readonly IDbCache _dbCache;
        private readonly IServiceScopeFactory _scopeFactory;

        public AutoSlowmodeService(IDbCache dbCache, IServiceScopeFactory scopeFactory)
        {
            _dbCache = dbCache;
            _scopeFactory = scopeFactory;
        }

        /// <summary>
        /// Gets or sets the specified autoslowmode settings.
        /// </summary>
        /// <typeparam name="T">The type of the setting to be returned.</typeparam>
        /// <param name="server">The target guild.</param>
        /// <param name="selector">A method to set the property.</param>
        /// <returns>The modified setting.</returns>
        public async Task<T> SetPropertyAsync<T>(DiscordGuild server, Func<AutoSlowmodeEntity, T> selector)
        {
            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);
            if (!_dbCache.AutoSlowmode.TryGetValue(server.Id, out var slowmode))
                slowmode = await db.AutoSlowmode.FirstOrDefaultAsync(x => x.GuildIdFK == server.Id) ?? new() { GuildIdFK = server.Id };

            db.AutoSlowmode.Upsert(slowmode);
            var result = selector(slowmode);
            await db.SaveChangesAsync();

            if (slowmode.IsActive)
                _dbCache.AutoSlowmode.TryAdd(server.Id, slowmode);
            else
                _dbCache.AutoSlowmode.TryRemove(server.Id, out _);

            return result;
        }

        /// <summary>
        /// Gets the autoslowmode settings associated with the specified Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild.</param>
        /// <returns>The guild's autoslowmode settings or <see langword="null"/> if it doesn't exist.</returns>
        public AutoSlowmodeEntity GetAutoSlowmodeSettings(DiscordGuild server)
        {
            if (_dbCache.AutoSlowmode.TryGetValue(server.Id, out var gatekeeper))
                return gatekeeper;

            _dbCache.Guilds.TryGetValue(server.Id, out var dbGuild);
            return dbGuild.AutoSlowmodeRel;
        }
    }
}