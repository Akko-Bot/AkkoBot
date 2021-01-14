using System;
using System.Threading.Tasks;
using AkkoBot.Credential;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace AkkoBot.Command.Attributes
{
    /// <summary>
    /// Checks if the command was issued by a bot owner and cancels execution if it wasn't.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = true)]
    public class BotOwner : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
        {
            var creds = context.CommandsNext.Services.GetService<Credentials>();
            return Task.FromResult(creds.OwnerIds.Contains(context.User.Id));
        }
    }
}