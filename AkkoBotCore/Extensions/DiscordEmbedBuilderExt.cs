using DSharpPlus.Entities;
using System.Collections.Generic;

namespace AkkoBot.Extensions
{
    public static class DiscordEmbedBuilderExt
    {
        /// <summary>
        /// Generates a deep copy of this embed.
        /// </summary>
        /// <param name="embed">This embed.</param>
        /// <param name="fields">A collection of embed fields that will replace the fields from the original embed. If this is <see langword="null"/>, the copy will contain all fields from the original embed.</param>
        /// <returns>A copy of this <paramref name="embed"/>.</returns>
        public static DiscordEmbedBuilder DeepCopy(this DiscordEmbedBuilder embed, IEnumerable<DiscordEmbedField> fields = null)
        {
            var copy = new DiscordEmbedBuilder()
            {
                Author = embed.Author,
                Url = embed.Url,
                Thumbnail = embed.Thumbnail,
                Title = embed.Title,
                Description = embed.Description,
                ImageUrl = embed.ImageUrl,
                Footer = embed.Footer,
                Timestamp = embed.Timestamp,
                Color = embed.Color
            };

            if (fields is null)
            {
                foreach (var field in embed.Fields)
                    copy.AddField(field.Name, field.Value, field.Inline);
            }
            else
            {
                foreach (var field in fields)
                    copy.AddField(field.Name, field.Value, field.Inline);
            }

            return copy;
        }
    }
}