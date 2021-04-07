using AkkoBot.Models;
using AkkoBot.Services;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.CommandsNext;
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

        /// <summary>
        /// Constructs a serializable model of this embed.
        /// </summary>
        /// <param name="embed">This embed.</param>
        /// <param name="content">The text content from outside the embed.</param>
        /// <returns>A <see cref="SerializableEmbed"/> of this <paramref name="embed"/>.</returns>
        public static SerializableEmbed BuildSerializableEmbed(this DiscordEmbedBuilder embed, string content = null)
        {
            var embedAuthor = (embed.Author is null) ? null : new SerializableEmbedAuthor(embed.Author.Name, embed.Author.Url, embed.Author.IconUrl);
            var embedTitle = (embed.Title is null) ? null : new SerializableEmbedTitle(embed.Title, embed.Url);

            var model = new SerializableEmbed
            {
                Content = content,
                Color = (!embed.Color.HasValue) ? null : embed.Color.Value.ToString(),
                Header = (embedAuthor is null && embed.Thumbnail?.Url is null) ? null : new SerializableEmbedHeader(embedAuthor, embed.Thumbnail?.Url),
                Body = (embed.Title is null && embed.Description is null && embed.ImageUrl is null) ? null : new SerializableEmbedBody(embedTitle, embed.Description, embed.ImageUrl),
                Fields = (embed.Fields is null || embed.Fields.Count == 0) ? null : new List<SerializableEmbedField>(embed.Fields.Count),
                Footer = (embed.Footer is null) ? null : new SerializableEmbedFooter(embed.Footer.Text, embed.Footer.IconUrl),
                Timestamp = embed.Timestamp
            };
            foreach (var field in embed.Fields)
                model.Fields.Add(new SerializableEmbedField(field.Name, field.Value, field.Inline));

            return model;
        }

        /// <summary>
        /// Adds a localized field to this embed.
        /// </summary>
        /// <param name="embed">This embed.</param>
        /// <param name="context">The command context.</param>
        /// <param name="name">The title of this field.</param>
        /// <param name="value">The content of this field.</param>
        /// <param name="inline">Whether the field should be inlined or not.</param>
        /// <returns>This embed with a localized field added to it.</returns>
        public static DiscordEmbedBuilder AddLocalizedField(this DiscordEmbedBuilder embed, CommandContext context, string name, string value, bool inline = false)
        {
            embed.AddField(context.FormatLocalized(name), context.FormatLocalized(value), inline);
            return embed;
        }
    }
}