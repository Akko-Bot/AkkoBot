using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Controllers.Abstractions;

/// <summary>
/// Represents an object that handles "views" for interactive messages.
/// </summary>
public interface ISlashController
{
    /// <summary>
    /// Handles the request for the specified component ID.
    /// </summary>
    /// <param name="message">The interactive message.</param>
    /// <param name="componentId">The ID of the component.</param>
    /// <param name="options">The options of the interaction.</param>
    /// <returns>A Discord interactive message or <see langword="null"/> if the request was not handled.</returns>
    ValueTask<DiscordInteractionResponseBuilder?> HandleRequestAsync(DiscordMessage message, string componentId, string[] options);
}