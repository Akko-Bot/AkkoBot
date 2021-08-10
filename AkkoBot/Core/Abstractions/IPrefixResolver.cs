using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Core.Abstractions
{
    /// <summary>
    /// Represents an object that calculates the length of the command
    /// prefix in a Discord message, if it is present.
    /// </summary>
    public interface IPrefixResolver
    {
        /// <summary>
        /// Defines whether a Discord message starts with a command prefix.
        /// </summary>
        /// <param name="msg">The message to be processed.</param>
        /// <returns>Length of the prefix if it is present, -1 otherwise.</returns>
        Task<int> ResolvePrefixAsync(DiscordMessage msg);
    }
}