using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.SlashCommands.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.SlashCommands.Abstractions;

/// <summary>
/// Defines the base behavior and actions for all slash command modules.
/// </summary>
[SlashIsNotBlacklisted]
public class AkkoSlashCommandModule : ApplicationCommandModule
{
    public override Task AfterSlashExecutionAsync(InteractionContext ctx)
        => UpsertUsersInMessageAsync(ctx);

    public override async Task<bool> BeforeSlashExecutionAsync(InteractionContext ctx)
    {
        await LoadGuildAsync(ctx).ConfigureAwait(false);
        return true;
    }

    /// <summary>
    /// Caches the user who has interacted with the bot and up to 3 users mentioned in the message.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    private async Task UpsertUsersInMessageAsync(InteractionContext context)
    {
        var dbCache = context.Services.GetRequiredService<IDbCache>();
        var userService = context.Services.GetRequiredService<DiscordUserService>();
        var mentionedUsers = (context.ResolvedUserMentions ?? Enumerable.Empty<DiscordUser>()) // Why is this null, wtf?
            .Append(context.User)
            .Distinct()
            .Where(x => !dbCache.Users.TryGetValue(x.Id, out var dbUser) || !dbUser.Username.Equals(x.Username, StringComparison.Ordinal))
            .ToArray();

        await userService.SaveUsersAsync(mentionedUsers);
    }

    /// <summary>
    /// Loads the guild the command was executed into the cache.
    /// </summary>
    /// <param name="context">The interaction context.</param>
    private async Task LoadGuildAsync(InteractionContext context)
    {
        if (context.Guild is null)
            return;

        await context.Services.GetRequiredService<IDbCache>().GetDbGuildAsync(context.Guild.Id);
    }
}