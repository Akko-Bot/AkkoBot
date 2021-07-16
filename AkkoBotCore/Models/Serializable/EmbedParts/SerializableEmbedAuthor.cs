using AkkoBot.Common;
using AkkoBot.Extensions;

namespace AkkoBot.Models.Serializable.EmbedParts
{
    /// <summary>
    /// Represents the Author Name, Url and ImageUrl properties of an embed.
    /// </summary>
    public class SerializableEmbedAuthor
    {
        private string _author;

        /// <summary>
        /// The URL associated with the <see cref="Text"/>.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// The URL to the header image icon.
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// The text to be displayed on the embed's header.
        /// </summary>
        public string Text
        {
            get => _author;
            set => _author = value?.MaxLength(AkkoConstants.MaxEmbedTitleLength);
        }

        public SerializableEmbedAuthor()
        {
        }

        public SerializableEmbedAuthor(string text, string url, string imageUrl)
        {
            Text = text;
            Url = url;
            ImageUrl = imageUrl;
        }
    }
}