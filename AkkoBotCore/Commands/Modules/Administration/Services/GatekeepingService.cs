using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.Entities;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="GatekeepEntity"/> objects.
    /// </summary>
    public class GatekeepingService : ICommandService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;

        public GatekeepingService(IServiceScopeFactory scopeFactory, IDbCache dbCache)
        {
            _scopeFactory = scopeFactory;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Gets or sets the specified gatekeeping setting.
        /// </summary>
        /// <typeparam name="T">The type of the setting to be returned.</typeparam>
        /// <param name="server">The target guild.</param>
        /// <param name="selector">A method to set the property.</param>
        /// <returns>The modified setting.</returns>
        public async Task<T> SetPropertyAsync<T>(DiscordGuild server, Func<GatekeepEntity, T> selector)
        {
            using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);
            _dbCache.Gatekeeping.TryGetValue(server.Id, out var gatekeeper);

            gatekeeper ??= await db.Gatekeeping.FirstOrDefaultAsyncEF(x => x.GuildIdFK == server.Id)
                ?? new() { GuildIdFK = server.Id };
            var result = selector(gatekeeper);

            // This will update the whole GuildConfig tree, not just gatekeeping.
            // Optimize this once LinqToDB supports "WithOutput" for PostgreSQL
            db.Update(gatekeeper);
            await db.SaveChangesAsync();

            if (gatekeeper.IsActive)
                _dbCache.Gatekeeping.TryAdd(server.Id, gatekeeper);
            else
                _dbCache.Gatekeeping.TryRemove(server.Id, out _);

            return result;
        }

        /// <summary>
        /// Gets the gatekeeping settings associated with the specified Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild.</param>
        /// <returns>The guild's gatekeeping settings or <see langword="null"/> if it doesn't exist.</returns>
        public GatekeepEntity GetGatekeepSettings(DiscordGuild server)
        {
            if (_dbCache.Gatekeeping.TryGetValue(server.Id, out var gatekeeper))
                return gatekeeper;

            _dbCache.Guilds.TryGetValue(server.Id, out var dbGuild);
            return dbGuild.GatekeepRel;
        }
    }
}