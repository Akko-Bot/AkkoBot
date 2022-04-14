using AkkoCore.Common;
using Kotz.Extensions;
using DSharpPlus.Entities;

namespace AkkoCore.Models.Serializable.EmbedParts;

/// <summary>
/// Represents the Title, Text and Inline properties of an embed field.
/// </summary>
public class SerializableEmbedField
{
    private string _title = string.Empty;
    private string _text = string.Empty;

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
        set => _title = value.MaxLength(AkkoConstants.MaxEmbedTitleLength);
    }

    /// <summary>
    /// Defines the body of this field.
    /// </summary>
    public string Text
    {
        get => _text;
        set => _text = value.MaxLength(AkkoConstants.MaxEmbedFieldLength, AkkoConstants.EllipsisTerminator);
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