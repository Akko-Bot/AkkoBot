using System.Collections.Generic;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Entities;
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

        public bool IsBlacklisted(ulong id)
            => _blacklist.Contains(id);

        public async Task AddAsync(BlacklistEntity value)
        {
            await _db.Database.ExecuteSqlRawAsync(
                @"INSERT INTO blacklist(type_id, type, name) " +
                $"VALUES({value.TypeId}, '{value.Type}', '{value.Name}') " +
                @"ON CONFLICT (type_id) " +
                @"DO NOTHING;"
            );

            _blacklist.Add(value.TypeId);
        }

        public async Task RemoveAsync(ulong id)
        {
            if (!_blacklist.Contains(id))
                return;

            await _db.Database.ExecuteSqlRawAsync($"DELETE FROM blacklist WHERE type_id = {id};");
            _blacklist.Remove(id);
        }

        public async Task<int> ClearAsync()
        {
            var rows = await _db.Database.ExecuteSqlRawAsync("DELETE FROM blacklist;");
            _blacklist.Clear();

            return rows;
        }
    }
}