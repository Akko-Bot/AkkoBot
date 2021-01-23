using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace AkkoBot.Command.Attributes
{
    /// <summary>
    /// Checks if the command was issued in DM with the bot and cancels execution if it was.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = false)]
    public sealed class IsNotPrivate : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
            => Task.FromResult(!context.Channel.IsPrivate);
    }
}