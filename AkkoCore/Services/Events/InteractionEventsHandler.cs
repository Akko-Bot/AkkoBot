using AkkoCore.Commands.Attributes;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;

namespace AkkoCore.Services.Events;

/// <summary>
/// Handles interactive Discord messages.
/// </summary>
/// <remarks>This implementation only processes interactions whose ID are suffixed with either "_update" or "_end".</remarks>
[CommandService<IInteractionEventsHandler>(ServiceLifetime.Singleton)]
public sealed class InteractionEventsHandler : IInteractionEventsHandler
{
    public const string StartInteractionPrefix = "akko_";
    public const string UpdateInteractionSuffix = "_update";
    public const string EndInteractionSuffix = "_end";

    // Message ID, (User ID, Date Added)
    private readonly ConcurrentDictionary<ulong, (ulong, DateTimeOffset)> _userButtonPendingAction = new();

    private readonly EventId _componentEvent = new(LoggerEvents.EventHandlerException.Id, "ComponentEvent");
    private readonly TimeSpan _cleanupTime = TimeSpan.FromMinutes(15);
    private readonly Timer _cleanupTimer;

    private readonly IInteractionResponseManager _responseGenerator;

    public InteractionEventsHandler(IInteractionResponseManager responseGenerator)
    {
        _responseGenerator = responseGenerator;

        // Setup cleanup timer
        // The timer is needed because this handler registers ALL "akko_" interactions
        _cleanupTimer = new(_cleanupTime.TotalMilliseconds);
        _cleanupTimer.Elapsed += CleanupUserInteractions;
    }

    public async Task RegisterNewInteractionAsync(DiscordClient client, InteractionCreateEventArgs eventArgs)
    {
        var message = await eventArgs.Interaction.GetOriginalResponseAsync().ConfigureAwait(false);   // wtf, Discord?

        if (eventArgs.Interaction.Data.CustomId?.StartsWith(StartInteractionPrefix, StringComparison.Ordinal) is true    // Check if interaction starts with "akko_"
            && message.Components.Count is not 0 && message.Author.Id == client.CurrentUser.Id)                 // Check if interaction is not from another bot
            _userButtonPendingAction.TryAdd(message.Id, (eventArgs.Interaction.User.Id, DateTimeOffset.Now));   // Track the interaction
    }

    public async Task UpdateInteractionAsync(DiscordClient client, ComponentInteractionCreateEventArgs eventArgs)
    {
        if (!_userButtonPendingAction.TryGetValue(eventArgs.Message.Id, out var userId) || eventArgs.User.Id != userId.Item1
            || !eventArgs.Id.EndsWith(UpdateInteractionSuffix, StringComparison.Ordinal))
            return;

        var response = await _responseGenerator.RequestAsync(eventArgs.Message, eventArgs.Id, eventArgs.Values).ConfigureAwait(false);

        if (response is not null && response.Components.Count is not 0)
        {
            await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, response).ConfigureAwait(false);
            return;
        }

        client.Logger.LogWarning(
            _componentEvent,
            (response is null)
                ? $"Failed to get the response for interaction of ID \"{eventArgs.Id}\"."
                : $"Interaction of ID \"{eventArgs.Id}\" returned a response with no components."
        );

        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate).ConfigureAwait(false);
    }

    public Task EndInteractionAsync(DiscordClient client, ComponentInteractionCreateEventArgs eventArgs)
    {
        // If interaction is not being tracked or if it is being tracked and was triggered
        // by the correct user, delete the message.
        return ((eventArgs.Id.StartsWith(StartInteractionPrefix, StringComparison.Ordinal)
            && !_userButtonPendingAction.ContainsKey(eventArgs.Message.Id))
            || (_userButtonPendingAction.TryRemove(eventArgs.Message.Id, out var userId)
            && eventArgs.User.Id == userId.Item1
            && eventArgs.Id.EndsWith(EndInteractionSuffix, StringComparison.Ordinal)))
                ? TerminateInteractionAsync(eventArgs)
                : Task.CompletedTask;
    }

    /// <summary>
    /// Terminates an interactive message.
    /// </summary>
    /// <param name="eventArgs">The interaction event argument.</param>
    /// <remarks>Regular messages are deleted, whereas ephemeral messages are stripped off their components.</remarks>
    private async Task TerminateInteractionAsync(ComponentInteractionCreateEventArgs eventArgs)
    {
        if (eventArgs.Message.Components.Count is 0 && (!eventArgs.Message.Flags.HasValue || !eventArgs.Message.Flags.Value.HasMessageFlag(MessageFlags.Ephemeral)))
        {
            await eventArgs.Message.DeleteAsync();
            return;
        }

        var response = await _responseGenerator.RequestAsync(eventArgs.Message, eventArgs.Id, eventArgs.Values).ConfigureAwait(false)
            ?? CloneResponse(eventArgs.Message);

        response.ClearComponents();

        await eventArgs.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, response);
    }

    /// <summary>
    /// Creates a response copy of the specified Discord message.
    /// </summary>
    /// <param name="message">The Discord message to be copied.</param>
    /// <remarks>Files are not included.</remarks>
    /// <returns>The copied response.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private DiscordInteractionResponseBuilder CloneResponse(DiscordMessage message)
    {
        return new DiscordInteractionResponseBuilder()
            .WithContent(message.Content)
            .AddEmbeds(message.Embeds)
            .AddComponents(message.Components);
    }

    /// <summary>
    /// Removes old interactions from the cache.
    /// </summary>
    private void CleanupUserInteractions(object? sender, ElapsedEventArgs eventArgs)
    {
        foreach (var entry in _userButtonPendingAction)
        {
            if (DateTimeOffset.Now.Subtract(entry.Value.Item2) > _cleanupTime)
                _userButtonPendingAction.TryRemove(entry);
        }
    }

    public void Dispose()
    {
        _userButtonPendingAction?.Clear();

        _cleanupTimer.Elapsed -= CleanupUserInteractions;
        _cleanupTimer?.Stop();
        _cleanupTimer?.Dispose();

        GC.SuppressFinalize(this);
    }
}