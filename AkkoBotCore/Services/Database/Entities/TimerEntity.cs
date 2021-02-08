using System;
using AkkoBot.Services.Database.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Entities
{
    /*
     * TimedBan: Absolute, Non-repeatable
     * TimedMute: Absolute, Non-repeatable
     * TimedWarn: Absolute, Non-repeatable
     * Reminder: Relative, Non-repeatable
     * Repeater: Relative, Repeatable
     * Daily Repeater: Absolute, Repeatable
     */
    public enum TimerType { TimedMute, TimedBan, TimedWarn, Reminder, Repeater }

    public class TimerEntity : DbEntity
    {
        [Key]
        public int Id { get; set; }
        public ulong? UserId { get; init; }
        public ulong? GuildId { get; init; }
        public ulong? ChannelId { get; init; }

        [Required]
        public TimeSpan Interval { get; init; }

        [Required]
        public bool IsRepeatable { get; init; }

        [Required]
        public bool IsAbsolute { get; init; }

        [Required]
        public TimerType Type { get; init; }

        [Required]
        public DateTimeOffset ElapseAt { get; init; }
    }
}