using AkkoCore.Commands.Abstractions;
using AkkoCore.Common;
using AkkoCore.Config.Models;
using AkkoCore.Core.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Basic;

public sealed class BasicCommands : AkkoCommandModule, IDisposable
{
    private const string _botAuthor = "Kotz#7922";
    private const string _versionString = "AkkoBot v0.3.1-beta";
    private readonly DateTimeOffset _startup = DateTimeOffset.Now;
    private readonly Process _currentProcess = Process.GetCurrentProcess();

    private readonly ICommandHandler _commandHandler;
    private readonly IGlobalEventsHandler _globalEvents;
    private readonly IDbCache _dbCache;
    private readonly ICogs _cogs;
    private readonly DiscordShardedClient _shardedClient;
    private readonly Credentials _creds;

    public BasicCommands(ICommandHandler commandHandler, IGlobalEventsHandler globalEvents, IDbCache dbCache,
        ICogs cogs, DiscordShardedClient shardedClient, Credentials creds)
    {
        _commandHandler = commandHandler;
        _globalEvents = globalEvents;
        _dbCache = dbCache;
        _cogs = cogs;
        _shardedClient = shardedClient;
        _creds = creds;
    }

    [Command("ping")]
    [Description("cmd_ping")]
    public async Task PingAsync(CommandContext context)
    {
        var embed = new SerializableDiscordEmbed()
            .WithDescription($"{context.Client.Ping} ms");

        await context.RespondLocalizedAsync(embed);
    }

    [Command("stats"), Aliases("uptime")]
    [Description("cmd_stats")]
    public async Task UptimeAsync(CommandContext context)
    {
        _currentProcess.Refresh();

        var elapsed = GetTimeSinceStartup();
        var embed = new SerializableDiscordEmbed()
            .WithAuthor(_versionString, AkkoConstants.RepositoryUrl, context.Client.CurrentUser.AvatarUrl ?? context.Client.CurrentUser.DefaultAvatarUrl)
            .AddField("author", _botAuthor, true)
            .AddField("commands_executed", $"{_commandHandler.CommandsRan} ({_commandHandler.CommandsRan / elapsed.TotalSeconds:F2}/s)", true)
            .AddField("Shards", $"#{context.Client.ShardId}/{context.Client.ShardCount}", true) // Shards is not localized - this is intentional
            .AddField("cogs", _cogs.Headers.Count.ToString(), true)
            .AddField("messages", $"{_globalEvents.MessageCount} ({_globalEvents.MessageCount / elapsed.TotalSeconds:F2}/s)", true)
            .AddField("memory", $"{_currentProcess.PrivateMemorySize64 / 1_000_000.0:F1} MB", true)    // Process needs to be fetched every single time for the value to update
            .AddField("owner_ids", string.Join("\n", GetBotOwnerIds(context.Client, _creds)), true)
            .AddField(
                "uptime",
                context.FormatLocalized("{0}: {1}\n", "days", elapsed.Days) +
                context.FormatLocalized("{0}: {1}\n", "hours", elapsed.Hours) +
                context.FormatLocalized("{0}: {1}", "minutes", elapsed.Minutes),
                inline: true
            )
            .AddField(
                "presence",
                context.FormatLocalized("{0}: {1}\n", "servers", _shardedClient.ShardClients.Values.Sum(client => client.Guilds.Count)) +
                context.FormatLocalized("{0}: {1}\n", "channels", _shardedClient.ShardClients.Values.Sum(client => client.Guilds.Values.Sum(y => y.Channels.Count))) +
                context.FormatLocalized("{0}: {1}", "users", _shardedClient.ShardClients.Values.Sum(client => client.Guilds.Values.Sum(y => y.MemberCount))),
                inline: true
            );

        await context.RespondLocalizedAsync(embed, false);
    }

    [Command("shardstats")]
    [Description("cmd_shardstats")]
    public async Task ShardStatsAsync(CommandContext context)
    {
        var list = _shardedClient.ShardClients.Values
            .OrderBy(x => x.ShardId)
            .Select(x => Formatter.InlineCode($" #{x.ShardId,-3}| {x.Guilds.Count,-4}"));

        var embed = new SerializableDiscordEmbed()
            .WithAuthor($"Shard | {context.FormatLocalized("servers")}");

        foreach (var listGroup in list.Chunk(AkkoConstants.LinesPerPage))
            embed.AddField(AkkoConstants.ValidWhitespace, string.Join('\n', listGroup));

        await context.RespondPaginatedByFieldsAsync(embed, 2);
    }

    public void Dispose()
    {
        _currentProcess.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Gets the user IDs of all bot owners.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="creds">The bot's credentials.</param>
    /// <returns>An unique collection of the ID of all bot owners.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEnumerable<ulong> GetBotOwnerIds(DiscordClient client, Credentials creds)
    {
        return creds.OwnerIds
            .Concat(client.CurrentApplication.Owners.Select(x => x.Id))
            .Distinct();
    }

    /// <summary>
    /// Gets the time elapsed since the bot started up.
    /// </summary>
    /// <returns>The time elapsed.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private TimeSpan GetTimeSinceStartup()
        => DateTimeOffset.Now.Subtract(_startup);
}