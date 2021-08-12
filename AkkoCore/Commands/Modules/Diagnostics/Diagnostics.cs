using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Localization.Abstractions;
using AkkoCore.Services.Timers.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Diagnostics
{
    [BotOwner]
    public class Diagnostics : AkkoCommandModule
    {
        private const double _mb = 1000000.0;    // Byte to Megabyte ratio
        private readonly IAkkoCache _akkoCache;

        public Diagnostics(IAkkoCache akkoCache)
            => _akkoCache = akkoCache;

        [Command("botstats")]
        [Description("cmd_botstats")]
        public async Task GetDbCacheSizeAsync(CommandContext context)
        {
            if (!Directory.Exists(AkkoEnvironment.LogsDirectory))
                Directory.CreateDirectory(AkkoEnvironment.LogsDirectory);

            var timers = await Task.Run(() => _akkoCache.Timers.GetMemoryEstimate(typeof(IDbCache), typeof(IServiceScopeFactory), typeof(ILocalizer)));
            var dbCache = await Task.Run(() => _akkoCache.GetMemoryEstimate(typeof(ITimerManager), typeof(IServiceScopeFactory)));
            var logFolderSize = new DirectoryInfo(AkkoEnvironment.LogsDirectory)
                .EnumerateFiles("*.txt", SearchOption.AllDirectories)
                .Sum(x => x.Length);

            var embed = new SerializableDiscordMessage()
                .AddField("db_cache", $"{dbCache / _mb:0.00} MB", true)
                .AddField("timers", $"{timers / _mb:0.00} MB", true)
                .AddField("total", $"{GC.GetTotalMemory(false) / _mb:0.00} MB", true)
                .AddField("saved_logs", $"{logFolderSize / _mb:0.00} MB", true);

            await context.RespondLocalizedAsync(embed);
        }
    }
}