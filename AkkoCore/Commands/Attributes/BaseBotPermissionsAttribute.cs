using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Attributes;

/// <summary>
/// Checks if the bot has the specified set of permissions and cancels command execution if
/// all of them are not present.
/// </summary>
[AttributeUsage(
AttributeTargets.Class |
AttributeTargets.Method,
AllowMultiple = false,
Inherited = true)]
public sealed class BaseBotPermissionsAttribute : CheckBaseAttribute
{
    public Permissions Perms { get; }

    public BaseBotPermissionsAttribute(Permissions permissions)
        => Perms = permissions;

    public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
    {
        return Task.FromResult(
            context.Channel.IsPrivate
            || context.Guild.CurrentMember.Id == context.Guild.OwnerId
            || context.Channel.PermissionsFor(context.Guild.CurrentMember).HasFlag(Perms)
        );
    }
}