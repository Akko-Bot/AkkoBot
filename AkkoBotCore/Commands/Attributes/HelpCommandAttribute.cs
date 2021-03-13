using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
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
    AllowMultiple = true,
    Inherited = false)]
    public sealed class HelpCommandAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
        {
            using var scope = context.Services.GetScopedService<IUnitOfWork>(out var db);
            return Task.FromResult(db.BotConfig.Cache.EnableHelp);
        }
    }
}