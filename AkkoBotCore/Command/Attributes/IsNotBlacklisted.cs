using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace AkkoBot.Command.Attributes
{
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = true)]
    public class IsNotBlacklisted : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
        {
            var db = context.CommandsNext.Services.GetService<IUnitOfWork>();
            return Task.FromResult(!db.Blacklist.IsBlacklisted(context));
        }
    }
}
