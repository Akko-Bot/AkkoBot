using AkkoBot.Services.Database;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Command.Attributes
{
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = true)]
    public class DbBlacklist : CheckBaseAttribute
    {
        // TODO: make this work with users, channels and servers
        public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
        {
            var db = context.CommandsNext.Services.GetService(typeof(AkkoDbContext)) as AkkoDbContext;

            if (context.Member.IsBot || db.GlobalBlacklist.Find(context.Member.Id) is not null)
                return Task.FromResult(false);
            else
                return Task.FromResult(true);
        }
    }
}
