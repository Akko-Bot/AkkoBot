using AkkoCore.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Attributes
{
    /// <summary>
    /// Checks if the command was issued in direct message and cancels execution if it nas not issued by a bot owner.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
    public sealed class RequireOwnerInDmAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
            => Task.FromResult(ctx.Guild is not null || GeneralService.IsOwner(ctx, ctx.User.Id));
    }
}