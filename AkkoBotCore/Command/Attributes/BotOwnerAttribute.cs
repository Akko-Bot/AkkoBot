using System.Linq;
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
    AllowMultiple = true,
    Inherited = false)]
    public sealed class BotOwnerAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
        {
            var creds = context.CommandsNext.Services.GetService<Credentials>();
            return (creds.OwnerIds.Contains(context.User.Id))
                ? Task.FromResult(true)
                : Task.FromResult(context.Client.CurrentApplication.Owners.Any(x => x.Id == context.Member.Id));
        }
    }
}