using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace AkkoCore.SlashCommands.Attributes;

/// <summary>
/// Checks if the user is allowed to execute a slash command and responds with an error message if it isn't.
/// </summary>
[AttributeUsage(
AttributeTargets.Class |
AttributeTargets.Method,
AllowMultiple = false,
Inherited = true)]
public class AkkoSlashRequireOwnerInDmAttribute : SlashCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
    {
        if (ctx.Guild is not null || AkkoUtilities.IsOwner(ctx, ctx.User.Id))
            return Task.FromResult(true);

        var embed = new SerializableDiscordEmbed()
            .WithDescription("slash_user_cmd_error");

        _ = ctx.RespondLocalizedAsync(embed, true, true);

        return Task.FromResult(false);
    }
}