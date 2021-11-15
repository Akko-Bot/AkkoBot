using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Services.Database.Abstractions;
using AkkoCore.Services.Database.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AkkoCore.Services.Database.Entities;

/// <summary>
/// Stores command data and the context it should be automatically sent to.
/// </summary>
[Comment("Stores command data and the context it should be automatically sent to.")]
public class AutoCommandEntity : DbEntity
{
    private readonly string _commandString = null!;

    /// <summary>
    /// The timer this autocommand is associated with.
    /// </summary>
    /// <remarks>This property is <see langword="null"/> if this autocommand is of type <see cref="AutoCommandType.Startup"/>.</remarks>
    public TimerEntity? TimerRel { get; init; }

    /// <summary>
    /// The database ID of the timer associated with this autocommand.
    /// </summary>
    /// <remarks>This property is <see langword="null"/> if this autocommand is of type <see cref="AutoCommandType.Startup"/>.</remarks>
    public int? TimerIdFK { get; init; }

    /// <summary>
    /// The qualified command name with its arguments.
    /// </summary>
    [Required]
    [MaxLength(AkkoConstants.MaxMessageLength)]
    public string CommandString
    {
        get => _commandString;
        init => _commandString = value.MaxLength(AkkoConstants.MaxMessageLength);
    }

    /// <summary>
    /// The ID of the Discord guild associated with this autocommand.
    /// </summary>
    public ulong GuildId { get; init; }

    /// <summary>
    /// The ID of the Discord user who created this autocommand.
    /// </summary>
    public ulong AuthorId { get; init; }

    /// <summary>
    /// The ID of the Discord channel where the autocommand is supposed to execute.
    /// </summary>
    public ulong ChannelId { get; init; }

    /// <summary>
    /// The type of this autocommand.
    /// </summary>
    public AutoCommandType Type { get; init; }
}