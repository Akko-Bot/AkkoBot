using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Attributes
{
    /// <summary>
    /// Checks if this help command or module has permission to run and cancels execution if it doesn't.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
    public sealed class HelpCommandAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
        {
            var dbCache = context.Services.GetService<IDbCacher>();
            return Task.FromResult(dbCache.BotConfig.EnableHelp);
        }
    }
}