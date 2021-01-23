using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace AkkoBot.Command.Attributes
{
    /// <summary>
    /// Checks if the command was issued by itself and cancels execution if it was.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = false)]
    public sealed class IsNotSelf : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
            => Task.FromResult(context.User.Id != context.Client.CurrentUser.Id);
    }
}