using AkkoCore.Common;
using Kotz.Extensions;

namespace AkkoCore.Models.Serializable.EmbedParts;

/// <summary>
/// Represents the Footer Text and ImageUrl properties of an embed.
/// </summary>
public class SerializableEmbedFooter
{
    private string? _text;

    /// <summary>
    /// The URL of the image icon to be displayed on the embed's footer.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// The text to be displayed on the embed's footer.
    /// </summary>
    public string? Text
    {
        get => _text;
        set => _text = value?.MaxLength(AkkoConstants.MaxEmbedDescriptionLength);
    }

    public SerializableEmbedFooter()
    {
    }

    public SerializableEmbedFooter(string? text, string? imageUrl = default)
    {
        Text = text;
        ImageUrl = imageUrl;
    }
}