using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using AkkoBot.Commands.Abstractions;

namespace AkkoBot.Commands.Attributes
{
    /// <summary>
    /// Checks if the command is on a cooldown and stops execution if it is.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
    public sealed class GlobalCooldownAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
        {
            var cmdCooldown = context.Services.GetService<ICommandCooldown>();

            if (!cmdCooldown.ContainsCommand(context.Command, context.Guild))
            {
                // Command has no cooldown
                return Task.FromResult(true);
            }
                
            else if (!cmdCooldown.IsOnCooldown(context.Command, context.User, context.Guild))
            {
                // Command has a cooldown but is not active for this user
                cmdCooldown.AddUser(context.Command, context.User);
                return Task.FromResult(true);
            }
            else
            {
                // Command has a cooldown and is active for this user
                return Task.FromResult(false);
            }
        }
    }
}
