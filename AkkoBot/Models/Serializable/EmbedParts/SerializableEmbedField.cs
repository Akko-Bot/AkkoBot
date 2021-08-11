using AkkoCore.Common;
using AkkoCore.Extensions;
using DSharpPlus.Entities;

namespace AkkoBot.Models.Serializable.EmbedParts
{
    /// <summary>
    /// Represents the Title, Text and Inline properties of an embed field.
    /// </summary>
    public class SerializableEmbedField
    {
        private string _title;
        private string _text;

        /// <summary>
        /// Defines whether this field is inlined or not.
        /// </summary>
        public bool Inline { get; set; }

        /// <summary>
        /// Defines the title of this field.
        /// </summary>
        public string Title
        {
            get => _title;
            set => _title = value?.MaxLength(AkkoConstants.MaxEmbedTitleLength);
        }

        /// <summary>
        /// Defines the body of this field.
        /// </summary>
        public string Text
        {
            get => _text;
            set => _text = value?.MaxLength(AkkoConstants.MaxEmbedFieldLength, "[...]");
        }

        public SerializableEmbedField()
        {
        }

        public SerializableEmbedField(DiscordEmbedField field)
        {
            Title = field.Name;
            Text = field.Value;
            Inline = field.Inline;
        }

        public SerializableEmbedField(string name, string value, bool inline = false)
        {
            Title = name;
            Text = value;
            Inline = inline;
        }
    }
}