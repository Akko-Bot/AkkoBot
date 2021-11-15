using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Abstractions;

/// <summary>
/// Represents an object that manages message interactivity.
/// </summary>
public interface IInteractionEventHandler : IDisposable
{
    /// <summary>
    /// Finalizes an interaction and stops its tracking.
    /// </summary>
    Task EndInteractionAsync(DiscordClient client, ComponentInteractionCreateEventArgs eventArgs);

    /// <summary>
    /// Registers the creation of an interaction for tracking purposes.
    /// </summary>
    Task RegisterNewInteractionAsync(DiscordClient client, InteractionCreateEventArgs eventArgs);

    /// <summary>
    /// Updates an interaction.
    /// </summary>
    Task UpdateInteractionAsync(DiscordClient client, ComponentInteractionCreateEventArgs eventArgs);
}