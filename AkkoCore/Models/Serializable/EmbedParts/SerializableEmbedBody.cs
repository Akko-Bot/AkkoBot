using AkkoCore.Common;
using AkkoCore.Extensions;

namespace AkkoCore.Models.Serializable.EmbedParts;

/// <summary>
/// Represents the Title, Description and ImageUrl properties of an embed.
/// </summary>
public class SerializableEmbedBody
{
    private string? _description;

    /// <summary>
    /// The title of the embed.
    /// </summary>
    public SerializableEmbedTitle? Title { get; set; }

    /// <summary>
    /// The URL for the image on the embed's body.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// The text on the embed's body.
    /// </summary>
    public string? Description
    {
        get => _description;
        set => _description = value?.MaxLength(AkkoConstants.MaxEmbedDescriptionLength, "[...]");
    }

    public SerializableEmbedBody()
    {
    }

    public SerializableEmbedBody(SerializableEmbedTitle? title, string? description, string? imageUrl)
    {
        Title = title;
        Description = description;
        ImageUrl = imageUrl;
    }
}