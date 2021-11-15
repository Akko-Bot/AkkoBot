using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Abstractions;

/// <summary>
/// Represents an object that handles role assignment or removal voice state changes.
/// </summary>
public interface IVoiceRoleConnectionHandler
{
    /// <summary>
    /// Assigns or revokes a role upon voice channel connection/disconnection.
    /// </summary>
    Task VoiceRoleAsync(DiscordClient client, VoiceStateUpdateEventArgs eventArgs);
}