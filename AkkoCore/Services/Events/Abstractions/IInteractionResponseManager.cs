using AkkoCore.Services.Events.Controllers.Abstractions;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Abstractions;

/// <summary>
/// Represents an object that generates interactive Discord messages.
/// </summary>
public interface IInteractionResponseManager
{
    /// <summary>
    /// Gets the follow-up interactive response for the specified component ID.
    /// </summary>
    /// <param name="message">The interactive message.</param>
    /// <param name="componentId">The ID of the component.</param>
    /// <param name="options">The options of the interaction.</param>
    /// <returns>The response for the given ID or <see langword="null"/> if it was not found.</returns>
    ValueTask<DiscordInteractionResponseBuilder?> RequestAsync(DiscordMessage message, string componentId, string[] options);

    /// <summary>
    /// Adds a controller to this handler.
    /// </summary>
    /// <param name="slashController">The controller to be added.</param>
    public void Add(ISlashController slashController);

    /// <summary>
    /// Adds multiple controllers to this handler.
    /// </summary>
    /// <param name="slashControllers">The controllers to be added.</param>
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="slashControllers"/> is <see langword="null"/>.</exception>
    public void AddRange(IEnumerable<ISlashController> slashControllers);
}