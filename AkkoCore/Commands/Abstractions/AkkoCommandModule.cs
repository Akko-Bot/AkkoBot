using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using LinqToDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Abstractions;

/// <summary>
/// Defines the base behavior and actions for all command modules.
/// </summary>
[IsNotBlacklisted, GlobalCooldown, BaseBotPermissions(Permissions.SendMessages | Permissions.AddReactions | Permissions.SendMessagesInThreads)]
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
        var dbCache = context.Services.GetRequiredService<IDbCache>();
        var userService = context.Services.GetRequiredService<DiscordUserService>();
        var mentionedUsers = context.Message.MentionedUsers
            .Concat(await GetUnmentionedUsersAsync(context, dbCache))
            .Append(context.User)
            .Distinct()
            .Where(x => !dbCache.Users.TryGetValue(x.Id, out var dbUser) || !dbUser.FullName.Equals(x.GetFullname(), StringComparison.Ordinal))
            .ToArray();

        await userService.SaveUsersAsync(mentionedUsers);
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