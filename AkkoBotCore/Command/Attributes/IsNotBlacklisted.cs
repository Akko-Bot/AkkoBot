using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace AkkoBot.Command.Attributes
{
    /// <summary>
    /// Checks if the command was issued in a blacklisted context and cancels execution if it was.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = true,
    Inherited = false)]
    public sealed class IsNotBlacklisted : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
        {
            using var scope = context.CommandsNext.Services.CreateScope();
            var db = scope.ServiceProvider.GetService<IUnitOfWork>();
            return Task.FromResult(!db.Blacklist.IsBlacklisted(context));
        }
    }
}
