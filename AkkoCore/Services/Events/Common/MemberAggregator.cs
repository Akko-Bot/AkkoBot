using AkkoCore.Commands.Common;
using AkkoCore.Commands.Formatters;
using AkkoCore.Services.Events.Abstractions;
using ConcurrentCollections;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace AkkoCore.Services.Events.Common
{
    /// <summary>
    /// Parses gatekeeping messages into individualized or bulk versions.
    /// </summary>
    public sealed class MemberAggregator : IMemberAggregator
    {
        private readonly ConcurrentDictionary<ulong, ConcurrentHashSet<DiscordMember>> _toGreet = new();
        private readonly BulkGreetPlaceholders _greetPlaceholders;

        public MemberAggregator(BulkGreetPlaceholders greetPlaceholders)
            => _greetPlaceholders = greetPlaceholders;

        public bool SendsBulk(ulong sid, TimeSpan time)
            => _toGreet.TryGetValue(sid, out var users) && users.Count > 1 && DateTimeOffset.Now.Subtract(users.Min(x => x.JoinedAt)) >= time;

        public bool Add(DiscordMember user)
        {
            if (_toGreet.TryGetValue(user.Guild.Id, out var users))
            {
                // If queue exists, cache the user
                users.Add(user);
                return true;
            }

            // If user is the first in the queue, create the queue and don't cache the user
            // This user will immediately get an individualized message.
            _toGreet.TryAdd(user.Guild.Id, new());
            return false;
        }

        public SmartString ParseMessage(CommandContext context, string greeting)
        {
            if (_toGreet.TryGetValue(context.Guild.Id, out var group) && group.Count is not 0)
            {
                // If queue exists and is not empty, delete it
                _toGreet.TryRemove(context.Guild.Id, out _);
                _greetPlaceholders.Users = group;

                // If queue has one user, send individualized message, else send bulk message
                return new SmartString(context, greeting, formatter: (group.Count is not 1) ? _greetPlaceholders : default);
            }

            // If queue doesn't exist or is empty, create individualized message
            return new SmartString(context, greeting);
        }

        public void Dispose()
        {
            foreach (var group in _toGreet.Values)
                group.Clear();

            _toGreet.Clear();

            GC.SuppressFinalize(this);
        }
    }
}