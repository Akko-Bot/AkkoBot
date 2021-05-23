using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using AkkoBot.Services.Timers.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Diagnostics
{
    [BotOwner]
    public class Diagnostics : AkkoCommandModule
    {
        private const double MB = 1000000.0;    // Byte to Megabyte ratio
        private readonly IDbCache _dbCache;

        public Diagnostics(IDbCache dbCache)
            => _dbCache = dbCache;

        [Command("botstats")]
        [Description("cmd_botstats")]
        public async Task GetDbCacheSize(CommandContext context)
        {
            if (!Directory.Exists(AkkoEnvironment.LogsDirectory))
                Directory.CreateDirectory(AkkoEnvironment.LogsDirectory);

            var timers = await Task.Run(() => _dbCache.Timers.GetMemoryEstimate(typeof(IDbCache), typeof(IServiceProvider), typeof(ILocalizer)));
            var dbCache = await Task.Run(() => _dbCache.GetMemoryEstimate(typeof(ITimerManager), typeof(IServiceProvider)));
            var logFolderSize = new DirectoryInfo(AkkoEnvironment.LogsDirectory)
                .EnumerateFiles("*.txt", SearchOption.AllDirectories)
                .Sum(x => x.Length);

            var embed = new DiscordEmbedBuilder()
                .AddField("db_cache", $"{dbCache / MB:0.00} MB", true)
                .AddField("timers", $"{timers / MB:0.00} MB", true)
                .AddField("total", $"{GC.GetTotalMemory(false) / MB:0.00} MB", true)
                .AddField("saved_logs", $"{logFolderSize / MB:0.00} MB", true);

            await context.RespondLocalizedAsync(embed);
        }
    }
}