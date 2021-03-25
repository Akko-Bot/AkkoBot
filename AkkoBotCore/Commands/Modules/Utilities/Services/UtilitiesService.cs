using AkkoBot.Commands.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Models;
using DSharpPlus.Entities;

namespace AkkoBot.Commands.Modules.Utilities.Services
{
    /// <summary>
    /// Groups utility methods for the Utilities command module.
    /// </summary>
    public class UtilitiesService : ICommandService
    {
        /// <summary>
        /// Deserializes user input in Yaml to a Discord message.
        /// </summary>
        /// <param name="input">The user's input.</param>
        /// <param name="result">The deserialized input, <see langword="null"/> if deserialization fails.</param>
        /// <returns><see langword="true"/> if deserialization was successful, <see langword="false"/> otherwise.</returns>
        public bool DeserializeEmbed(string input, out DiscordMessageBuilder result)
        {
            try
            {
                result = input.FromYaml<SerializableEmbed>().BuildMessage();
                return result.Content is not null || result.Embed is not null;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}