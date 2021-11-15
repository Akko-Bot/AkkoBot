using AkkoCore.Config.Models;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events;

/// <summary>
/// Handles global events.
/// </summary>
internal sealed class GlobalEventsHandler : IGlobalEventsHandler
{
    private readonly IGuildEventsHandler _guildEventsHandler;
    private readonly IDbCache _dbCache;
    private readonly BotConfig _botConfig;

    public uint MessageCount { get; private set; } = 0;

    public GlobalEventsHandler(IGuildEventsHandler guildEventsHandler, IDbCache dbCache, BotConfig botconfig)
    {
        _guildEventsHandler = guildEventsHandler;
        _dbCache = dbCache;
        _botConfig = botconfig;
    }

    public Task CountMessageAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (eventArgs.Message.Author?.IsBot is false)
            MessageCount++;

        return Task.CompletedTask;
    }

    public async Task BlockBlacklistedAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (_dbCache.Blacklist.Contains(eventArgs.Author.Id)
            || _dbCache.Blacklist.Contains(eventArgs.Channel.Id)
            || _dbCache.Blacklist.Contains(eventArgs.Guild?.Id ?? default))
        {
            eventArgs.Handled = true;

            if (!(await _guildEventsHandler.FilterWordAsync(client, eventArgs) || await _guildEventsHandler.FilterInviteAsync(client, eventArgs) || await _guildEventsHandler.FilterStickerAsync(client, eventArgs)))
                await _guildEventsHandler.FilterContentAsync(client, eventArgs);
        }
    }

    public async Task DefaultPrefixAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (eventArgs.Author.IsBot || !eventArgs.Message.Content.StartsWith("!prefix", StringComparison.InvariantCultureIgnoreCase))
            return;

        var prefix = _dbCache.Guilds.TryGetValue(eventArgs.Guild?.Id ?? default, out var dbGuild)
                ? dbGuild.Prefix
                : _botConfig.Prefix;

        if (eventArgs.Guild is not null && prefix.Equals("!", StringComparison.Ordinal))
            return;

        // Get command handler and prefix command
        var cmdHandler = client.GetCommandsNext();
        var prefixCmd = cmdHandler.FindCommand(eventArgs.Message.Content[prefix.Length..], out var cmdArgs)
            ?? cmdHandler.FindCommand(eventArgs.Message.Content[1..], out cmdArgs);

        // Create the context and execute the command
        if (string.IsNullOrWhiteSpace(cmdArgs) || eventArgs.Guild is not null)
        {
            var context = cmdHandler.CreateContext(eventArgs.Message, prefix, prefixCmd, cmdArgs);
            await prefixCmd.ExecuteAndLogAsync(context);
            eventArgs.Handled = true;
        }
    }
}