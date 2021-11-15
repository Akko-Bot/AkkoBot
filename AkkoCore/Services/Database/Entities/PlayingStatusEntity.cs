using AkkoCore.Extensions;
using AkkoCore.Services.Database.Abstractions;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoCore.Services.Database.Entities;

/// <summary>
/// Stores data related to the bot's Discord status.
/// </summary>
[Comment("Stores data related to the bot's Discord status.")]
public class PlayingStatusEntity : DbEntity
{
    private string _message = null!;
    private string? _streamUrl;

    /// <summary>
    /// The message to be displayed in the status.
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Message
    {
        get => _message;
        set => _message = value.MaxLength(128);
    }

    /// <summary>
    /// The stream URL to be displayed in the status.
    /// </summary>
    [MaxLength(512)]
    public string? StreamUrl
    {
        get => _streamUrl;
        init => _streamUrl = value?.MaxLength(512);
    }

    /// <summary>
    /// The type of status.
    /// </summary>
    public ActivityType Type { get; set; }

    /// <summary>
    /// Rotation time of the status.
    /// </summary>
    /// <remarks>This property is set to <see cref="TimeSpan.Zero"/> for static statuses.</remarks>
    public TimeSpan RotationTime { get; init; }

    /// <summary>
    /// Gets the <see cref="DiscordActivity"/> this entity represents.
    /// </summary>
    /// <remarks>This property is not mapped.</remarks>
    /// <returns>A <see cref="DiscordActivity"/>.</returns>
    [NotMapped]
    public DiscordActivity Activity
        => new(Message, Type) { StreamUrl = StreamUrl };
}