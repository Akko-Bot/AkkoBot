using AkkoCore.Commands.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Queries;
using DSharpPlus.Entities;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for manipulating <see cref="DiscordUserEntity"/> objects.
    /// </summary>
    public class DiscordUserService : ICommandService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IDbCache _dbCache;

        public DiscordUserService(IServiceScopeFactory scopeFactory, IDbCache dbCache)
        {
            _scopeFactory = scopeFactory;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Saves the specified Discord users to the database and cache.
        /// </summary>
        /// <param name="mentionedUsers">The users to be saved.</param>
        /// <returns>The amount of inserted users and updated users, respectively.</returns>
        public async ValueTask<(int, int)> SaveUsersAsync(IReadOnlyCollection<DiscordUser> mentionedUsers)
        {
            if (mentionedUsers.Count is 0)
                return (0, 0);

            using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

            // Update old users
            foreach (var user in mentionedUsers)
            {
                if (!_dbCache.Users.TryGetValue(user.Id, out var dbUser) || dbUser.FullName.Equals(user.GetFullname(), StringComparison.Ordinal))
                    continue;

                db.Upsert(dbUser);

                dbUser.Username = user.Username;
                dbUser.Discriminator = user.Discriminator;
            }

            var updated = await db.SaveChangesAsync();

            // Insert new users
            var newDbUsers = mentionedUsers
                .Where(x => !_dbCache.Users.ContainsKey(x.Id))
                .Select(x => new DiscordUserEntity(x))
                .ToArray();

            if (newDbUsers.Length is 0)
                return (0, updated);

            var inserted = await db.BulkCopyAsync(newDbUsers);

            // Add new users to the cache
            foreach (var dbUser in newDbUsers)
                _dbCache.Users.TryAdd(dbUser.UserId, dbUser);

            return ((int)inserted.RowsCopied, updated);
        }
    }
}