using AkkoCore.Common;
using AkkoCore.Extensions;

namespace AkkoCore.Models.Serializable.EmbedParts
{
    /// <summary>
    /// Represents the Title Text and Url properties of an embed.
    /// </summary>
    public class SerializableEmbedTitle
    {
        private string? _text;

        /// <summary>
        /// The URL associated with the <see cref="Text"/>.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// The text to be displayed on the embed's title.
        /// </summary>
        public string? Text
        {
            get => _text;
            set => _text = value?.MaxLength(AkkoConstants.MaxEmbedTitleLength);
        }

        public SerializableEmbedTitle()
        {
        }

        public SerializableEmbedTitle(string? text, string? url)
        {
            Text = text;
            Url = url;
        }
    }
}