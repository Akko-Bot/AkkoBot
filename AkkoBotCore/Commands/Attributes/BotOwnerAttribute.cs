using AkkoBot.Credential;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Attributes
{
    /// <summary>
    /// Checks if the command was issued by a bot owner and cancels execution if it wasn't.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = false)]
    public sealed class BotOwnerAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
        {
            var creds = context.Services.GetService<Credentials>();
            return Task.FromResult(
                creds.OwnerIds.Contains(context.User.Id)
                || context.Client.CurrentApplication.Owners.Any(x => x.Id == context.Member.Id)
            );
        }
    }
}