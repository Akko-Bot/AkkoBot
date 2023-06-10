using AkkoCore.Commands.Attributes;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events;

/// <summary>
/// Handles global events.
/// </summary>
[CommandService<IGlobalEventsHandler>(ServiceLifetime.Singleton)]
internal sealed class GlobalEventsHandler : IGlobalEventsHandler
{
    private readonly IGuildEventsHandler _guildEventsHandler;
    private readonly IDbCache _dbCache;

    public uint MessageCount { get; private set; } = 0;

    public GlobalEventsHandler(IGuildEventsHandler guildEventsHandler, IDbCache dbCache)
    {
        _guildEventsHandler = guildEventsHandler;
        _dbCache = dbCache;
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
            if (!(await _guildEventsHandler.FilterWordAsync(client, eventArgs) || await _guildEventsHandler.FilterInviteAsync(client, eventArgs) || await _guildEventsHandler.FilterStickerAsync(client, eventArgs)))
                await _guildEventsHandler.FilterContentAsync(client, eventArgs);
        }
    }
}