namespace AkkoCore.Models.Serializable.EmbedParts
{
    /// <summary>
    /// Represents the Author and ThumbnailUrl properties of an embed.
    /// </summary>
    public class SerializableEmbedHeader
    {
        /// <summary>
        /// The header's author field.
        /// </summary>
        public SerializableEmbedAuthor? Author { get; set; }

        /// <summary>
        /// The URL for the thumbnail image.
        /// </summary>
        public string? ThumbnailUrl { get; set; }

        public SerializableEmbedHeader()
        {
        }

        public SerializableEmbedHeader(SerializableEmbedAuthor? author, string? thumbnailUrl)
        {
            Author = author;
            ThumbnailUrl = thumbnailUrl;
        }
    }
}