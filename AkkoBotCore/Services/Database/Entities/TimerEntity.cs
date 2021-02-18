using System;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    /*
     * TimedBan: Absolute, Non-repeatable
     * TimedMute: Absolute, Non-repeatable
     * TimedWarn: Absolute, Non-repeatable
     * TimedRole: Absolute, Non-repeatable
     * TimedUnrole: Absolute, Non-repeatable
     * Reminder: Relative, Non-repeatable
     * Repeater: Relative, Repeatable
     * Daily Repeater: Absolute, Repeatable
     */

    public enum TimerType { TimedMute, TimedBan, TimedWarn, TimedRole, TimedUnrole, Reminder, Repeater }

    [Comment("Stores actions that need to be performed at some point in the future.")]
    public class TimerEntity : DbEntity
    {
        public ulong? UserId { get; init; }
        public ulong? GuildId { get; init; }
        public ulong? ChannelId { get; init; }
        public ulong? RoleId { get; init; }
        public TimeSpan Interval { get; init; }
        public bool IsRepeatable { get; init; }
        public bool IsAbsolute { get; init; } // Might want to remove this
        public TimerType Type { get; init; }
        public DateTimeOffset ElapseAt { get; init; }

        public TimerEntity() { }

        public TimerEntity(MutedUserEntity muteUser, TimeSpan time)
        {
            UserId = muteUser.UserId;
            GuildId = muteUser.GuildIdFK;
            ChannelId = null;
            RoleId = null;
            Interval = time;
            IsRepeatable = false;
            IsAbsolute = true;
            Type = TimerType.TimedMute;
            ElapseAt = muteUser.DateAdded.Add(time);
        }

        public TimerEntity(WarnEntity warning, TimeSpan time)
        {
            UserId = warning.UserId;
            GuildId = warning.GuildIdFK;
            ChannelId = null;
            RoleId = null;
            Interval = time;
            IsRepeatable = false;
            IsAbsolute = true;
            Type = TimerType.TimedWarn;
            ElapseAt = warning.DateAdded.Add(time);
        }
    }
}