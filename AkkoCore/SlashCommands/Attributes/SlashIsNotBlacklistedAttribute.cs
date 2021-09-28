using AkkoCore.Services.Caching.Abstractions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AkkoCore.SlashCommands.Attributes
{
    /// <summary>
    /// Checks if the slash command was issued in a blacklisted context and cancels execution if it was.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
    public class SlashIsNotBlacklistedAttribute : SlashCheckBaseAttribute
    {
        public override Task<bool> ExecuteChecksAsync(InteractionContext context)
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