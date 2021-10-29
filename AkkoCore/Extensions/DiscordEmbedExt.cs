using AkkoCore.Models.Serializable;
using AkkoCore.Models.Serializable.EmbedParts;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AkkoCore.Extensions
{
    public static class DiscordEmbedExt
    {
        /// <summary>
        /// Localizes the content of this embed.
        /// </summary>
        /// <param name="embed">This embed.</param>
        /// <param name="localizer">The cache of response strings.</param>
        /// <param name="locale">The locale to be used.</param>
        /// <param name="color">A hexadecimal color to set the embed if it doesn't have one.</param>
        /// <returns>This embed builder.</returns>
        public static DiscordEmbed WithLocalization(this DiscordEmbed embed, ILocalizer localizer, string locale, string? color = default)
            => new DiscordEmbedBuilder(embed).WithLocalization(localizer, locale, color).Build();

        /// <summary>
        /// Converts all text content from this embed into a string.
        /// </summary>
        /// <param name="embed">Embed to be decomposed.</param>
        /// <remarks>It ignores image links, except for the one on the image field.</remarks>
        /// <returns>A formatted string with the contents of the embed.</returns>
        public static string Decompose(this DiscordEmbed embed)
            => Decompose(embed, default).ToString();

        /// <summary>
        /// Converts all text content from this embed into a string.
        /// </summary>
        /// <param name="embed">Embed to be decomposed.</param>
        /// <param name="stringBuilder">String builder to be used in the deconstruction.</param>
        /// <remarks>It ignores image links, except for the one on the image field.</remarks>
        /// <returns>A string builder with the contents of the embed.</returns>
        public static StringBuilder Decompose(this DiscordEmbed embed, StringBuilder? stringBuilder)
        {
            stringBuilder ??= new StringBuilder();

            stringBuilder.AppendLine(
                ((embed.Author?.Name is null) ? string.Empty : embed.Author.Name + "\n\n") +
                ((embed.Title is null) ? string.Empty : Formatter.Bold(embed.Title) + "\n") +
                ((embed.Description is null) ? string.Empty : embed.Description + "\n\n")
            );

            if (embed.Fields.Count is not 0)
                stringBuilder.Append(Formatter.BlockCode(SerializableDiscordEmbed.DecomposeEmbedFields(embed.Fields.Select(x => new SerializableEmbedField(x)), 3))); // Discord limits embeds to 3 inline fields per line

            stringBuilder.Append(
                ((embed.Image?.Url is null) ? string.Empty : $"{embed.Image.Url}\n\n") +
                ((embed.Footer?.Text is null) ? string.Empty : embed.Footer.Text + "\n") +
                ((embed.Timestamp is null) ? string.Empty : embed.Timestamp.ToString())
            );

            return stringBuilder;
        }

        /// <summary>
        /// Constructs a serializable model of this embed.
        /// </summary>
        /// <param name="embed">This embed.</param>
        /// <returns>A <see cref="SerializableDiscordEmbed"/> of this <paramref name="embed"/>.</returns>
        public static SerializableDiscordEmbed ToSerializableEmbed(this DiscordEmbed embed)
        {
            var embedAuthor = (embed.Author is null) ? null : new SerializableEmbedAuthor(embed.Author.Name, embed.Author.Url?.AbsoluteUri, embed.Author.IconUrl?.ToUri().AbsoluteUri);
            var embedTitle = (embed.Title is null) ? null : new SerializableEmbedTitle(embed.Title, embed.Url?.AbsoluteUri);

            var model = new SerializableDiscordEmbed
            {
                Color = (!embed.Color.HasValue) ? null : embed.Color.Value.ToString(),
                Header = (embedAuthor is null && embed.Thumbnail?.Url is null) ? null : new SerializableEmbedHeader(embedAuthor, embed.Thumbnail?.Url?.ToUri().AbsoluteUri),
                Body = (embed.Title is null && embed.Description is null && embed.Image.Url is null) ? null : new SerializableEmbedBody(embedTitle, embed.Description, embed.Image.Url?.ToUri().AbsoluteUri),
                Fields = (embed.Fields is null || embed.Fields.Count == 0) ? null : new List<SerializableEmbedField>(embed.Fields.Count),
                Footer = (embed.Footer is null) ? null : new SerializableEmbedFooter(embed.Footer.Text, embed.Footer.IconUrl?.ToUri().AbsoluteUri),
                Timestamp = embed.Timestamp
            };

            if (embed.Fields is not null)
            {
                foreach (var field in embed.Fields)
                    model.AddField(field.Name, field.Value, field.Inline);
            }

            return model;
        }
    }
}