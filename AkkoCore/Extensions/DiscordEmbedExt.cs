using AkkoCore.Models.Serializable.EmbedParts;
using AkkoCore.Services;
using DSharpPlus;
using DSharpPlus.Entities;
using System.Linq;
using System.Text;

namespace AkkoCore.Extensions
{
    public static class DiscordEmbedExt
    {
        /// <summary>
        /// Converts all text content from an embed into a string.
        /// </summary>
        /// <param name="embed">Embed to be deconstructed.</param>
        /// <remarks>It ignores image links, except for the one on the image field.</remarks>
        /// <returns>A formatted string with the contents of the embed.</returns>
        public static string Deconstruct(this DiscordEmbed embed)
        {
            var dEmbed = new StringBuilder(
                ((embed.Author?.Name is null) ? string.Empty : embed.Author.Name + "\n\n") +
                ((embed.Title is null) ? string.Empty : Formatter.Bold(embed.Title) + "\n") +
                ((embed.Description is null) ? string.Empty : embed.Description + "\n\n")
            );

            if (embed.Fields.Count is not 0)
                dEmbed.Append(Formatter.BlockCode(GeneralService.DeconstructEmbedFields(embed.Fields.Select(x => new SerializableEmbedField(x)), 3))); // Discord limits embeds to 3 inline fields per line

            dEmbed.Append(
                ((embed.Image?.Url is null) ? string.Empty : $"{embed.Image.Url}\n\n") +
                ((embed.Footer?.Text is null) ? string.Empty : embed.Footer.Text + "\n") +
                ((embed.Timestamp is null) ? string.Empty : embed.Timestamp.ToString())
            );

            return dEmbed.ToString();
        }
    }
}