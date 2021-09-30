using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Timers;

namespace AkkoCore.Services.Events
{
    /// <summary>
    /// Handles interactive Discord messages.
    /// </summary>
    /// <remarks>This implementation only processes interactions whose ID are suffixed with either "_update" or "_end".</remarks>
    internal class InteractionEventHandler : IInteractionEventHandler
    {
        private const string _updateInteractionSuffix = "_update";
        private const string _endInteractionSuffix = "_end";

        // Message ID, (User ID, Date Added)
        private readonly ConcurrentDictionary<ulong, (ulong, DateTimeOffset)> _userButtonPendingAction = new();

        private readonly EventId _componentEvent = new(LoggerEvents.EventHandlerException.Id, "ComponentEvent");
        private readonly TimeSpan _cleanupTime = TimeSpan.FromMinutes(15);
        private readonly Timer _cleanupTimer;

        private readonly IInteractionResponseManager _responseGenerator;

        public InteractionEventHandler(IInteractionResponseManager responseGenerator)
        {
            _responseGenerator = responseGenerator;

            // Setup cleanup timer
            // The timer is needed because this handler registers ALL interactions
            _cleanupTimer = new(_cleanupTime.TotalMilliseconds);
            _cleanupTimer.Elapsed += CleanupUserInteractions;
        }

        public async Task RegisterNewInteractionAsync(DiscordClient client, InteractionCreateEventArgs eventArgs)
        {
            var message = await eventArgs.Interaction.GetOriginalResponseAsync().ConfigureAwait(false);   // wtf, Discord?

            if (message.Components.Count is not 0 && message.Author.Id == client.CurrentUser.Id) // Check if interaction is not from another bot
                _userButtonPendingAction.TryAdd(message.Id, (eventArgs.Interaction.User.Id, DateTimeOffset.Now));
        }

        public async Task UpdateInteractionAsync(DiscordClient client, ComponentInteractionCreateEventArgs eventArgs)
        {
            if (!_userButtonPendingAction.TryGetValue(eventArgs.Message.Id, out var userId) || eventArgs.User.Id != userId.Item1
                || !eventArgs.Id.EndsWith(_updateInteractionSuffix, StringComparison.Ordinal))
                return;

            eventArgs.Handled = true;
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
            return (!_userButtonPendingAction.ContainsKey(eventArgs.Message.Id)
                || (_userButtonPendingAction.TryRemove(eventArgs.Message.Id, out var userId)
                && eventArgs.User.Id == userId.Item1
                && eventArgs.Id.EndsWith(_endInteractionSuffix, StringComparison.Ordinal)))
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
            eventArgs.Handled = true;

            if (!eventArgs.Message.Flags.HasValue || !eventArgs.Message.Flags.Value.HasMessageFlag(MessageFlags.Ephemeral))
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
        private void CleanupUserInteractions(object sender, ElapsedEventArgs eventArgs)
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
}