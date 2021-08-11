using AkkoBot.Services.Caching.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Attributes
{
    /// <summary>
    /// Checks if the command was issued in a blacklisted context and cancels execution if it was.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
    public sealed class IsNotBlacklistedAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext context, bool help)
        {
            var dbCache = context.Services.GetRequiredService<IDbCache>();

            return Task.FromResult(
                !dbCache.Blacklist.Contains(context.Channel.Id)
                && !dbCache.Blacklist.Contains(context.User.Id)
                && !dbCache.Blacklist.Contains(context.Guild?.Id ?? context.User.Id)
            );
        }
    }
}