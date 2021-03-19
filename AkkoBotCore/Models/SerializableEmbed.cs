using AkkoBot.Extensions;
using AkkoBot.Services;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using static DSharpPlus.Entities.DiscordEmbedBuilder;

namespace AkkoBot.Models
{
    /// <summary>
    /// Represents a Discord message that can be serialized.
    /// </summary>
    /// <remarks>All its properties are <see langword="null"/> by default.</remarks>
    public class SerializableEmbed
    {
        /// <summary>
        /// Represents the message content outside of the embed.
        /// </summary>
        public string Content { get; set; }

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
        /// Constructs the Discord embed represented by this model.
        /// </summary>
        /// <returns>A <see cref="DiscordEmbedBuilder"/>, <see langword="null"/> if the embed is invalid.</returns>
        /// <exception cref="ArgumentException">Occurs when the embed <see cref="Color"/> is not a valid color.</exception>
        public DiscordEmbedBuilder BuildEmbed()
        {
            if (!IsValidEmbed())
                return null;

            var embed = new DiscordEmbedBuilder
            {
                Author = new EmbedAuthor()
                {
                    Name = this.Header?.Author?.Name,
                    Url = this.Header?.Author?.Url,
                    IconUrl = this.Header?.Author?.ImageUrl
                },

                Thumbnail = new EmbedThumbnail() { Url = this.Header?.ThumbnailUrl },
                Title = this.Body?.Title?.Text,
                Url = this.Body?.Title?.Url,
                Description = this.Body?.Description,
                ImageUrl = this.Body?.ImageUrl,

                Footer = new EmbedFooter()
                {
                    Text = this.Footer?.Text,
                    IconUrl = this.Footer?.ImageUrl
                },

                Color = (string.IsNullOrWhiteSpace(this.Color)) ? Optional.FromNoValue<DiscordColor>() : new DiscordColor(Color),
                Timestamp = this.Timestamp
            };

            if (Fields is not null)
            {
                // Embeds can't have more than 25 fields
                if (Fields.Count > 25)
                    Fields.RemoveRange(24, Fields.Count - 25);

                foreach (var field in Fields)
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
        /// Checks if the embed to be serialized is valid.
        /// </summary>
        /// <returns><see langword="true"/> if the embed is valid, <see langword="false"/> otherwise.</returns>
        private bool IsValidEmbed()
            => !string.IsNullOrWhiteSpace(Header?.ThumbnailUrl) || Body is not null || Fields is not null || Footer is not null;
    }

    /// <summary>
    /// Represents the Author Name, Url and ImageUrl properties of an embed.
    /// </summary>
    public class SerializableEmbedAuthor
    {
        private string _author;

        public string Name
        {
            get => _author;
            set => _author = value?.MaxLength(AkkoEntities.EmbedTitleMaxLength);
        }

        public string Url { get; set; }
        public string ImageUrl { get; set; }

        public SerializableEmbedAuthor()
        {
        }

        public SerializableEmbedAuthor(string name, string url, string imageUrl)
        {
            Name = name;
            Url = url;
            ImageUrl = imageUrl;
        }
    }

    /// <summary>
    /// Represents the Author and ThumbnailUrl properties of an embed.
    /// </summary>
    public class SerializableEmbedHeader
    {
        public SerializableEmbedAuthor Author { get; set; }
        public string ThumbnailUrl { get; set; }

        public SerializableEmbedHeader()
        {
        }

        public SerializableEmbedHeader(SerializableEmbedAuthor author, string thumbnailUrl)
        {
            Author = author;
            ThumbnailUrl = thumbnailUrl;
        }
    }

    /// <summary>
    /// Represents the Title Text and Url properties of an embed.
    /// </summary>
    public class SerializableEmbedTitle
    {
        public string Text { get; set; }
        public string Url { get; set; }

        public SerializableEmbedTitle()
        {
        }

        public SerializableEmbedTitle(string text, string url)
        {
            Text = text;
            Url = url;
        }
    }

    /// <summary>
    /// Represents the Title, Description and ImageUrl properties of an embed.
    /// </summary>
    public class SerializableEmbedBody
    {
        private string _description;

        public SerializableEmbedTitle Title { get; set; }

        public string Description
        {
            get => _description;
            set => _description = value?.MaxLength(AkkoEntities.EmbedPropMaxLength, "[...]");
        }

        public string ImageUrl { get; set; }

        public SerializableEmbedBody()
        {
        }

        public SerializableEmbedBody(SerializableEmbedTitle title, string description, string imageUrl)
        {
            Title = title;
            Description = description;
            ImageUrl = imageUrl;
        }
    }

    /// <summary>
    /// Represents the Title, Text and Inline properties of an embed field.
    /// </summary>
    public class SerializableEmbedField
    {
        private string _title;
        private string _text;

        public string Title
        {
            get => _title;
            set => _title = value?.MaxLength(AkkoEntities.EmbedTitleMaxLength);
        }

        public string Text
        {
            get => _text;
            set => _text = value?.MaxLength(1024, "[...]");
        }

        public bool Inline { get; set; }

        public SerializableEmbedField()
        {
        }

        public SerializableEmbedField(string name, string value, bool inline = false)
        {
            Title = name;
            Text = value;
            Inline = inline;
        }
    }

    /// <summary>
    /// Represents the Footer Text and ImageUrl properties of an embed.
    /// </summary>
    public class SerializableEmbedFooter
    {
        private string _text;

        public string Text
        {
            get => _text;
            set => _text = value?.MaxLength(AkkoEntities.EmbedPropMaxLength);
        }

        public string ImageUrl { get; set; }

        public SerializableEmbedFooter()
        {
        }

        public SerializableEmbedFooter(string text, string imageUrl)
        {
            Text = text;
            ImageUrl = imageUrl;
        }
    }
}