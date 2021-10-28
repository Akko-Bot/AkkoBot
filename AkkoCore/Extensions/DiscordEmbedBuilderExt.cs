using AkkoCore.Models.Serializable;
using AkkoCore.Models.Serializable.EmbedParts;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus.Entities;
using System.Collections.Generic;

namespace AkkoCore.Extensions
{
    public static class DiscordEmbedBuilderExt
    {
        /// <summary>
        /// Generates a deep copy of this embed.
        /// </summary>
        /// <param name="embed">This embed.</param>
        /// <param name="fields">A collection of embed fields that will replace the fields from the original embed. If this is <see langword="null"/>, the copy will contain all fields from the original embed.</param>
        /// <returns>A copy of this <paramref name="embed"/>.</returns>
        public static DiscordEmbedBuilder DeepCopy(this DiscordEmbedBuilder embed, IEnumerable<DiscordEmbedField>? fields = default)
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
        /// <returns>A <see cref="SerializableDiscordEmbed"/> of this <paramref name="embed"/>.</returns>
        public static SerializableDiscordEmbed ToSerializableEmbed(this DiscordEmbedBuilder embed)
        {
            var embedAuthor = (embed.Author is null) ? null : new SerializableEmbedAuthor(embed.Author.Name, embed.Author.Url, embed.Author.IconUrl);
            var embedTitle = (embed.Title is null) ? null : new SerializableEmbedTitle(embed.Title, embed.Url);

            var model = new SerializableDiscordEmbed
            {
                Color = (!embed.Color.HasValue) ? null : embed.Color.Value.ToString(),
                Header = (embedAuthor is null && embed.Thumbnail?.Url is null) ? null : new SerializableEmbedHeader(embedAuthor, embed.Thumbnail?.Url),
                Body = (embed.Title is null && embed.Description is null && embed.ImageUrl is null) ? null : new SerializableEmbedBody(embedTitle, embed.Description, embed.ImageUrl),
                Fields = (embed.Fields is null || embed.Fields.Count == 0) ? null : new List<SerializableEmbedField>(embed.Fields.Count),
                Footer = (embed.Footer is null) ? null : new SerializableEmbedFooter(embed.Footer.Text, embed.Footer.IconUrl),
                Timestamp = embed.Timestamp
            };

            if (embed.Fields is not null)
            {
                foreach (var field in embed.Fields)
                    model.AddField(field.Name, field.Value, field.Inline);
            }

            return model;
        }

        /// <summary>
        /// Adds a localized field to this embed.
        /// </summary>
        /// <param name="embed">This embed.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="locale">The locale to translate to.</param>
        /// <param name="name">The title of this field.</param>
        /// <param name="value">The content of this field.</param>
        /// <param name="inline">Whether the field should be inlined or not.</param>
        /// <returns>This embed with a localized field added to it.</returns>
        public static DiscordEmbedBuilder AddLocalizedField(this DiscordEmbedBuilder embed, ILocalizer localizer, string locale, string name, string value, bool inline = false)
        {
            embed.AddField(localizer.GetResponseString(locale, name), localizer.GetResponseString(locale, value), inline);
            return embed;
        }

        /// <summary>
        /// Localizes the content of this embed.
        /// </summary>
        /// <param name="embed">This embed.</param>
        /// <param name="localizer">The cache of response strings.</param>
        /// <param name="locale">The locale to be used.</param>
        /// <param name="color">A hexadecimal color to set the embed if it doesn't have one.</param>
        /// <returns>This embed builder.</returns>
        public static DiscordEmbedBuilder WithLocalization(this DiscordEmbedBuilder embed, ILocalizer localizer, string locale, string? color = default)
        {
            if (!embed.Color.HasValue)
                embed.WithColor(new DiscordColor(color));

            if (!string.IsNullOrWhiteSpace(embed.Author?.Name))
                embed.Author.Name = localizer.GetResponseString(locale, embed.Author.Name);

            if (!string.IsNullOrWhiteSpace(embed.Title))
                embed.Title = localizer.GetResponseString(locale, embed.Title);

            if (!string.IsNullOrWhiteSpace(embed.Description))
                embed.Description = localizer.GetResponseString(locale, embed.Description);

            if (!string.IsNullOrWhiteSpace(embed.Footer?.Text))
                embed.Footer.Text = localizer.GetResponseString(locale, embed.Footer.Text);

            foreach (var field in embed.Fields)
            {
                field.Name = localizer.GetResponseString(locale, field.Name);
                field.Value = localizer.GetResponseString(locale, field.Value);
            }

            return embed;
        }
    }
}