using System.Collections.Generic;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.CommandsNext;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Repository
{
    public class BlacklistRepo : DbRepository<BlacklistEntity>
    {
        private readonly AkkoDbContext _db;
        private readonly HashSet<ulong> _blacklist;

        public BlacklistRepo(AkkoDbContext db, AkkoDbCacher dbCacher) : base(db)
        {
            _db = db;
            _blacklist = dbCacher.Blacklist;
        }

        /// <summary>
        /// Checks if the provided ID is backlisted.
        /// </summary>
        /// <param name="id">ID of a user, channel or guild.</param>
        /// <returns><see langword="true"/> if the ID is blacklisted, <see langword="false"/> if not.</returns>
        public bool IsBlacklisted(ulong id)
            => _blacklist.Contains(id);

        /// <summary>
        /// Checks if the command comes from a backlisted context.
        /// </summary>
        /// <param name="id">Context of the command.</param>
        /// <returns><see langword="true"/> if it's blacklisted, <see langword="false"/> if not.</returns>
        public bool IsBlacklisted(CommandContext context)
        {
            return _blacklist.Contains(context.User.Id)
                || _blacklist.Contains(context.Channel.Id)
                || _blacklist.Contains(context.Guild?.Id ?? default); // This will cause dms to always fail if 0 is in the blacklist.
        }

        /// <summary>
        /// Adds a blacklist entry to the database.
        /// </summary>
        /// <param name="value">The specified blacklist entry.</param>
        /// <returns></returns>
        public async Task AddAsync(BlacklistEntity value)
        {
            await _db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO blacklist(type_id, type, name) " +
                $"VALUES({value.TypeId}, {(int)value.Type}, '{value.Name}') " +
                @"ON CONFLICT (type_id) " +
                @"DO NOTHING;"
            );

            _blacklist.Add(value.TypeId);
        }

        /// <summary>
        /// Removes a blacklist entry from the database.
        /// </summary>
        /// <param name="id">The specified blacklist ID.</param>
        /// <returns></returns>
        public async Task RemoveAsync(ulong id)
        {
            if (!_blacklist.Contains(id))
                return;

            await _db.Database.ExecuteSqlRawAsync($"DELETE FROM blacklist WHERE type_id = {id};");
            _blacklist.Remove(id);
        }

        /// <summary>
        /// Removes all blacklist entries from the database.
        /// </summary>
        /// <returns>The amount of removed rows.</returns>
        public async Task<int> ClearAsync()
        {
            var rows = await _db.Database.ExecuteSqlRawAsync("DELETE FROM blacklist;");
            _blacklist.Clear();

            return rows;
        }
    }
}