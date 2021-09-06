using DSharpPlus;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Abstractions
{
    /// <summary>
    /// Defines an object that manages command execution.
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary>
        /// Handles command execution.
        /// </summary>
        Task HandleCommandAsync(DiscordClient client, MessageCreateEventArgs eventArgs);
    }
}