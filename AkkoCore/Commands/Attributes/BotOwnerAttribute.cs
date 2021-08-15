using AkkoCore.Config.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Attributes
{
    /// <summary>
    /// Checks if the command was issued by a bot owner and cancels execution if it wasn't.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
    public sealed class BotOwnerAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
        {
            return Task.FromResult(
                context.Services.GetRequiredService<Credentials>().OwnerIds.Contains(context.User.Id)
                || context.Client.CurrentApplication.Owners.Any(x => x.Id == context.User.Id)
            );
        }
    }
}