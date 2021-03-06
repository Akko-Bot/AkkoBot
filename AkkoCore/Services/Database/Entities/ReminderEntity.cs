using AkkoCore.Common;
using AkkoCore.Services.Database.Abstractions;
using Kotz.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoCore.Services.Database.Entities;

/// <summary>
/// Stores reminder data and the context it should be sent to.
/// </summary>
[Comment("Stores reminder data and the context it should be sent to.")]
public class ReminderEntity : DbEntity
{
    private string _content = null!;
    private readonly DateTimeOffset _elapseAt = DateTimeOffset.Now.ToOffset(TimeSpan.Zero);

    /// <summary>
    /// The timer this reminder is associated with.
    /// </summary>
    public TimerEntity TimerRel { get; init; } = null!;

    /// <summary>
    /// The database ID of the timer this reminder is associated with.
    /// </summary>
    public int TimerIdFK { get; init; }

    /// <summary>
    /// The content of this reminder.
    /// </summary>
    [Required]
    [MaxLength(AkkoConstants.MaxMessageLength)]
    public string Content
    {
        get => _content;
        init => _content = value.MaxLength(AkkoConstants.MaxMessageLength) ?? "-";
    }

    /// <summary>
    /// The ID of the Discord guild associated with this reminder.
    /// </summary>
    /// <remarks>This property is <see langword="null"/> if this reminder is to be sent in direct message to the author.</remarks>
    public ulong? GuildId { get; init; }

    /// <summary>
    /// The ID of the Discord user that created this reminder.
    /// </summary>
    public ulong AuthorId { get; init; }

    /// <summary>
    /// The ID of the Discord channel where this reminder was created.
    /// </summary>
    public ulong ChannelId { get; init; }

    /// <summary>
    /// Determines whether this reminder should be sent in direct message to the author
    /// (<see langword="true"/>) or to a Discord guild (<see langword="false"/>).
    /// </summary>
    public bool IsPrivate { get; init; }

    /// <summary>
    /// The time and date this reminder should be sent.
    /// </summary>
    public DateTimeOffset ElapseAt
    {
        get => _elapseAt;
        init => _elapseAt = value.ToOffset(TimeSpan.Zero);
    }

    /// <summary>
    /// How long until this reminder is triggered.
    /// </summary>
    /// <remarks>This property is not mapped.</remarks>
    [NotMapped]
    public TimeSpan ElapseIn
        => ElapseAt.Subtract(DateTimeOffset.Now);
}