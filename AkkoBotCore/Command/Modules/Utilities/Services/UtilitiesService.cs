using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Models;
using DSharpPlus.Entities;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AkkoBot.Command.Modules.Utilities.Services
{
    /// <summary>
    /// Groups utility methods for the Utilities command module.
    /// </summary>
    public class UtilitiesService : ICommandService
    {
        /// <summary>
        /// Serializes a Discord embed to Yaml.
        /// </summary>
        /// <param name="embed">The Discord embed.</param>
        /// <returns>The serialized embed.</returns>
        public string SerializeEmbed(DiscordEmbedBuilder embed)
        {
            var yaml = new SerializerBuilder()
                .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreFields()
                .Build();

            return yaml.Serialize(embed.BuildSerializableEmbed());
        }

        /// <summary>
        /// Deserializes user input in Yaml into a Discord message.
        /// </summary>
        /// <param name="input">The user's input.</param>
        /// <param name="result">The deserialized input, <see langword="null"/> if deserialization fails.</param>
        /// <returns><see langword="true"/> if deserialization was successful, <see langword="false"/> otherwise.</returns>
        public bool DeserializeEmbed(string input, out DiscordMessageBuilder result)
        {
            var yaml = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            try
            {
                result = yaml.Deserialize<SerializableEmbed>(input).BuildMessage();
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }
    }
}
