using AkkoBot.Extensions;
using AkkoBot.Models.Serializable.EmbedParts;
using AkkoBot.Services;
using AkkoBot.Services.Localization.Abstractions;
using AkkoEntities.Common;
using AkkoEntities.Extensions;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static DSharpPlus.Entities.DiscordEmbedBuilder;

namespace AkkoBot.Models.Serializable
{
    /// <summary>
    /// Represents a Discord message that can be serialized.
    /// </summary>
    /// <remarks>All its properties are <see langword="null"/> by default.</remarks>
    public class SerializableDiscordMessage
    {
        private string _content;

        /// <summary>
        /// Represents the message content outside of the embed.
        /// </summary>
        public string Content
        {
            get => _content;
            set => _content = value?.MaxLength(AkkoConstants.MaxMessageLength, "[...]");
        }

        /// <summary>
        /// Represents the embed color, in hexadecimal.
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// Contains the embed's Author and ThumbnailUrl properties.
        /// </summary>
        public SerializableEmbedHeader Header { get; set; }

        /// <summary>
        /// Contains the embed's Title, Description and ImageUrl properties.
        /// </summary>
        public SerializableEmbedBody Body { get; set; }

        /// <summary>
        /// Contains a collection of the embed's fields, if any.
        /// </summary>
        /// <remarks>This collection is <see langword="null"/> when there are no fields in the source embed.</remarks>
        public List<SerializableEmbedField> Fields { get; set; } // This needs to be null. If I use an empty list, it shows up in the (de)serialization.

        /// <summary>
        /// Contains the embed's Footer properties.
        /// </summary>
        public SerializableEmbedFooter Footer { get; set; }

        /// <summary>
        /// Contains the embed's timestamp property.
        /// </summary>
        public DateTimeOffset? Timestamp { get; set; }

        /// <summary>
        /// Checks if the embed in this builder is valid.
        /// </summary>
        /// <returns><see langword="true"/> if the embed is valid, <see langword="false"/> otherwise.</returns>
        public bool HasValidEmbed()
            => !string.IsNullOrWhiteSpace(Header?.ThumbnailUrl) || Body is not null || Fields is not null || Footer is not null;

        /// <summary>
        /// Sets the text outside the embed.
        /// </summary>
        /// <param name="content">The text to be displayed.</param>
        /// <returns>This message builder.</returns>
        public SerializableDiscordMessage WithContent(string content)
        {
            Content = content;
            return this;
        }

        /// <summary>
        /// Sets the embed's author.
        /// </summary>
        /// <param name="name">The text to be displayed.</param>
        /// <param name="url">URL to link with the displayed text.</param>
        /// <param name="imageUrl">URL to an image.</param>
        /// <returns>This message builder.</returns>
        public SerializableDiscordMessage WithAuthor(string name, string url = null, string imageUrl = null)
        {
            Header ??= new SerializableEmbedHeader();
            Header.Author = new(name, url, imageUrl);

            return this;
        }

        /// <summary>
        /// Sets the embed's thumbnail.
        /// </summary>
        /// <param name="imageUrl">URL to an image.</param>
        /// <returns>This message builder.</returns>
        public SerializableDiscordMessage WithThumbnail(string imageUrl)
        {
            Header ??= new SerializableEmbedHeader();
            Header.ThumbnailUrl = imageUrl;

            return this;
        }

        /// <summary>
        /// Sets the embed's title.
        /// </summary>
        /// <param name="name">The text to be displayed.</param>
        /// <param name="url">URL to link with the displayed text.</param>
        /// <returns>This message builder.</returns>
        public SerializableDiscordMessage WithTitle(string name, string url = null)
        {
            Body ??= new SerializableEmbedBody();
            Body.Title = new(name, url);

            return this;
        }

        /// <summary>
        /// Sets the embed's description.
        /// </summary>
        /// <param name="text">The text to be displayed.</param>
        /// <returns>This message builder.</returns>
        public SerializableDiscordMessage WithDescription(string text)
        {
            Body ??= new SerializableEmbedBody();
            Body.Description = text;

            return this;
        }

        /// <summary>
        /// Sets the embed's image.
        /// </summary>
        /// <param name="imageUrl">URL to an image.</param>
        /// <returns>This message builder.</returns>
        public SerializableDiscordMessage WithImageUrl(string imageUrl)
        {
            Body ??= new SerializableEmbedBody();
            Body.ImageUrl = imageUrl;

            return this;
        }

        /// <summary>
        /// Sets the embed's footer.
        /// </summary>
        /// <param name="text">The text to be displayed.</param>
        /// <param name="imageUrl">URL to an image.</param>
        /// <returns>This message builder.</returns>
        public SerializableDiscordMessage WithFooter(string text, string imageUrl = null)
        {
            Footer = new SerializableEmbedFooter(text, imageUrl);
            return this;
        }

        /// <summary>
        /// Sets the embed's color.
        /// </summary>
        /// <param name="hexCode">A color code in hexadecimal.</param>
        /// <returns>This message builder.</returns>
        public SerializableDiscordMessage WithColor(string hexCode)
        {
            Color = hexCode;
            return this;
        }

        /// <summary>
        /// Sets the embed's timestamp.
        /// </summary>
        /// <param name="datetime">A datetime.</param>
        /// <returns>This message builder.</returns>
        public SerializableDiscordMessage WithTimestamp(DateTimeOffset datetime)
        {
            Timestamp = datetime;
            return this;
        }

        /// <summary>
        /// Adds a field to this embed.
        /// </summary>
        /// <param name="title">The title of the field.</param>
        /// <param name="body">The body of the field.</param>
        /// <param name="inline">Sets whether the field should be inlined or not.</param>
        /// <returns>This message builder.</returns>
        public SerializableDiscordMessage AddField(string title, string body, bool inline = false)
        {
            Fields ??= new();
            Fields.Add(new(title, body, inline));

            return this;
        }

        /// <summary>
        /// Localizes the content of this message.
        /// </summary>
        /// <param name="localizer">The cache of response strings.</param>
        /// <param name="locale">The locale to be used.</param>
        /// <returns>This message builder.</returns>
        public SerializableDiscordMessage WithLocalization(ILocalizer localizer, string locale)
        {
            Content = localizer.GetResponseString(locale, Content);

            if (Header?.Author?.Text is not null)
                Header.Author.Text = localizer.GetResponseString(locale, Header.Author.Text);

            if (Footer?.Text is not null)
                Footer.Text = localizer.GetResponseString(locale, Footer.Text);

            if (Body is not null)
            {
                if (Body.Title?.Text is not null)
                    Body.Title.Text = localizer.GetResponseString(locale, Body.Title.Text);

                if (Body.Description is not null)
                    Body.Description = localizer.GetResponseString(locale, Body.Description);
            }

            if (Fields is not null)
            {
                foreach (var field in Fields)
                {
                    field.Title = localizer.GetResponseString(locale, field.Title);
                    field.Text = localizer.GetResponseString(locale, field.Text);
                }
            }

            return this;
        }

        /// <summary>
        /// Converts all text content from this message into a string.
        /// </summary>
        /// <param name="embed">Embed to be deconstructed.</param>
        /// <remarks>It ignores image links, except for the one on the image field.</remarks>
        /// <returns>A formatted string with the contents of the embed.</returns>
        public string Deconstruct()
        {
            if (!HasValidEmbed())
                return Content ?? string.Empty;

            var dEmbed = new StringBuilder(
                ((this.Content is null) ? string.Empty : this.Content + "\n\n") +
                ((this.Header?.Author?.Text is null) ? string.Empty : this.Header.Author.Text + "\n\n") +
                ((this.Body?.Title?.Text is null) ? string.Empty : Formatter.Bold(this.Body.Title.Text) + "\n") +
                ((this.Body?.Description is null) ? string.Empty : this.Body.Description + "\n\n")
            );

            if (this.Fields?.Count is not null and not 0)
                dEmbed.Append(Formatter.BlockCode(GeneralService.DeconstructEmbedFields(this.Fields, 3))); // Discord limits embeds to 3 inline fields per line

            dEmbed.Append(
                ((this.Body?.ImageUrl is null) ? string.Empty : $"{this.Body.ImageUrl}\n\n") +
                ((this.Footer?.Text is null) ? string.Empty : this.Footer.Text + "\n") +
                ((this.Timestamp is null) ? string.Empty : this.Timestamp.ToString())
            );

            if (dEmbed.Length > AkkoConstants.MaxMessageLength)
                dEmbed.Remove(AkkoConstants.MaxMessageLength, dEmbed.Length - AkkoConstants.MaxMessageLength);

            return dEmbed.ToString();
        }

        /// <summary>
        /// Constructs the Discord embed represented by this model.
        /// </summary>
        /// <param name="fields">Overrides the fields that should be included. Set it to <see langword="null"/> to use the fields in this builder.</param>
        /// <returns>A <see cref="DiscordEmbedBuilder"/>, <see langword="null"/> if the embed is invalid.</returns>
        /// <exception cref="ArgumentException">Occurs when the embed <see cref="Color"/> is not a valid color.</exception>
        public DiscordEmbedBuilder BuildEmbed(IEnumerable<SerializableEmbedField> fields = null)
        {
            var localFields = (fields is not null)
                ? fields.ToList()
                : Fields;

            if (!HasValidEmbed())
                return null;

            var embed = new DiscordEmbedBuilder()
            {
                Author = new EmbedAuthor()
                {
                    Name = this.Header?.Author?.Text,
                    Url = this.Header?.Author?.Url,
                    IconUrl = this.Header?.Author?.ImageUrl
                },

                Thumbnail = new EmbedThumbnail() { Url = this.Header?.ThumbnailUrl },
                Title = this.Body?.Title?.Text,
                Url = this.Body?.Title?.Url,
                Description = this.Body?.Description,
                ImageUrl = this.Body?.ImageUrl,

                Footer = (Footer is null)
                    ? null
                    : new EmbedFooter()
                    {
                        Text = this.Footer?.Text,
                        IconUrl = this.Footer?.ImageUrl
                    },

                Color = (string.IsNullOrWhiteSpace(this.Color)) ? Optional.FromNoValue<DiscordColor>() : new DiscordColor(Color),
                Timestamp = this.Timestamp
            };

            if (localFields is not null)
            {
                // Embeds can't have more than 25 fields
                if (localFields.Count > 25)
                    localFields.RemoveRange(24, localFields.Count - 25);

                foreach (var field in localFields)
                    embed.AddField(field.Title, field.Text, field.Inline);
            }

            return embed;
        }

        /// <summary>
        /// Constructs the Discord message represented by this model.
        /// </summary>
        /// <returns>A <see cref="DiscordMessageBuilder"/> with the message content and the embed.</returns>
        /// <exception cref="ArgumentException">Occurs when the embed <see cref="Color"/> is not a valid color.</exception>
        public DiscordMessageBuilder BuildMessage()
        {
            return new DiscordMessageBuilder()
            {
                Content = this.Content,
                Embed = BuildEmbed()
            };
        }

        /// <summary>
        /// Constructs the Discord webhook message represented by this model.
        /// </summary>
        /// <returns>A <see cref="DiscordWebhookBuilder"/> with the message content and the embed.</returns>
        /// <exception cref="ArgumentException">Occurs when the embed <see cref="Color"/> is not a valid color.</exception>
        public DiscordWebhookBuilder BuildWebhookMessage()
        {
            return new DiscordWebhookBuilder()
                .WithContent(this.Content)
                .AddEmbed(BuildEmbed());
        }

        /// <summary>
        /// Clears all data in this message.
        /// </summary>
        public void Clear()
        {
            _content = null;
            Color = null;
            Header = null;
            Body = null;
            Fields?.Clear();
            Fields = null;
            Footer = null;
            Timestamp = null;
        }

        /* Operator Overloads */

        public static implicit operator DiscordEmbedBuilder(SerializableDiscordMessage x) => x?.BuildEmbed();

        public static implicit operator DiscordMessageBuilder(SerializableDiscordMessage x) => x?.BuildMessage();

        public static implicit operator SerializableDiscordMessage(DiscordEmbedBuilder x) => x?.BuildSerializableEmbed();
    }
}