using AkkoBot.Commands.Attributes;
using AkkoBot.Extensions;
using AkkoBot.Services.Caching.Abstractions;
using AkkoDatabase;
using AkkoDatabase.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Abstractions
{
    [IsNotBlacklisted, GlobalCooldown, BaseBotPermissions(Permissions.SendMessages | Permissions.AddReactions)]
    public abstract class AkkoCommandModule : BaseCommandModule
    {
        // Executes before command execution
        public override Task BeforeExecutionAsync(CommandContext ctx)
            => UpsertUsersInMessageAsync(ctx);

        /// <summary>
        /// Caches the user who has interacted with the bot and up to 3 users mentioned in the message.
        /// </summary>
        /// <param name="context">The command context.</param>
        private async Task UpsertUsersInMessageAsync(CommandContext context)
        {
            var dbCache = context.Services.GetService<IDbCache>();

            var mentionedUsers = context.Message.MentionedUsers
                .Concat(await GetUnmentionedUsersAsync(context, dbCache))
                .Append(context.User)
                .Distinct()
                .Where(x => !dbCache.Users.TryGetValue(x.Id, out var dbUser) || !dbUser.FullName.Equals(x.GetFullname(), StringComparison.Ordinal))
                .ToList();

            if (mentionedUsers.Count is 0)
                return;

            using var scope = context.Services.GetScopedService<AkkoDbContext>(out var db);

            for (var counter = 0; counter < mentionedUsers.Count; counter++)
            {
                // Skip users that are meant to be inserted
                if (!dbCache.Users.TryGetValue(mentionedUsers[counter].Id, out var dbUser))
                    continue;

                // Update the cache
                dbUser.Username = mentionedUsers[counter].Username;
                dbUser.Discriminator = mentionedUsers[counter].Discriminator;

                // Remove this element so it doesn't get inserted into the database later
                mentionedUsers.Remove(mentionedUsers[counter--]);

                // Update the database
                await db.DiscordUsers.UpdateAsync(
                    x => x.Id == dbUser.Id,
                    _ => new DiscordUserEntity() { Username = dbUser.Username, Discriminator = dbUser.Discriminator }
                );
            }

            // If there are no new users to be inserted, quit
            if (mentionedUsers.Count is 0)
                return;

            // Add new users to the database
            await db.BulkCopyAsync(mentionedUsers.Select(x => new DiscordUserEntity(x)));

            // Add these users to the cache
            var insertedEntries = await db.DiscordUsers
                .Where(x => mentionedUsers.Select(y => y.Id).Contains(x.UserId))
                .ToArrayAsyncEF();

            foreach (var dbUser in insertedEntries)
                dbCache.Users.TryAdd(dbUser.UserId, dbUser);
        }

        /// <summary>
        /// Gets up to 3 unmentioned users from a command string.
        /// </summary>
        /// <param name="context">The command context</param>
        /// <param name="cache">The database cache.</param>
        /// <returns>The unmentioned users.</returns>
        private async Task<IReadOnlyList<DiscordUser>> GetUnmentionedUsersAsync(CommandContext context, IDbCache cache)
        {
            if (context.RawArguments.Count is 0)
                return new List<DiscordUser>(0);

            var result = new List<DiscordUser>(3);

            try
            {
                foreach (var word in context.RawArguments.Where(x => ulong.TryParse(x, out var number) && !cache.Users.ContainsKey(number)).Take(3))
                {
                    // This fails if the ulong is not of a Discord user
                    var user = await context.CommandsNext.ConvertArgument<DiscordUser>(word, context) as DiscordUser;

                    if (user is not null)
                        result.Add(user);
                }
            }
            catch
            {
                // Stop looping to avoid ratelimiting
                return result;
            }

            return result;
        }
    }
}