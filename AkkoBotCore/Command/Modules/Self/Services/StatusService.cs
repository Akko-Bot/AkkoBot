using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.Entities;

namespace AkkoBot.Command.Modules.Self.Services
{
    public class StatusService : ICommandService
    {
        private readonly IServiceProvider _services;

        public StatusService(IServiceProvider services)
            => _services = services;

        public async Task CreateStatus(DiscordActivity activity, TimeSpan time, string streamUrl = null)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var newEntry = new PlayingStatusEntity()
            {
                Message = activity.Name,
                Type = activity.ActivityType,
                RotationTime = time,
                StreamUrl = (activity.ActivityType == ActivityType.Streaming) ? streamUrl : null
            };

            db.PlayingStatuses.Add(newEntry);
            await db.SaveChangesAsync();
        }

        public async Task<bool> RemoveStaticStatus()
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            var entry = await db.PlayingStatuses.GetStatusAsync(x => x.RotationTime == TimeSpan.Zero);

            if (entry is not null)
                db.PlayingStatuses.Remove(entry);

            return await db.SaveChangesAsync() is not 0;
        }

        public async Task<bool> RemoveRotatingStatus(int id)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var entry = await db.PlayingStatuses.GetAsync(id);

            if (entry is not null)
                db.PlayingStatuses.Remove(entry);

            return await db.SaveChangesAsync() is not 0;
        }

        public List<PlayingStatusEntity> GetStatuses()
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            return db.PlayingStatuses.Cache;
        }
    }
}