using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoBot.Services.Events.Abstractions
{
    public interface IVoiceRoleConnectionHandler
    {
        /// <summary>
        /// Assigns or revokes a role upon voice channel connection/disconnection
        /// </summary>
        /// <param name="client">The Discord client.</param>
        /// <param name="eventArgs">The voice state event.</param>
        Task VoiceRoleAsync(DiscordClient client, VoiceStateUpdateEventArgs eventArgs);
    }
}