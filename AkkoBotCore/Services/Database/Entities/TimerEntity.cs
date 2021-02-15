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
        public ulong? UserId { get; init; }
        public ulong? GuildId { get; init; }
        public ulong? ChannelId { get; init; }
        public TimeSpan Interval { get; init; }
        public bool IsRepeatable { get; init; }
        public bool IsAbsolute { get; init; }
        public TimerType Type { get; init; }
        public DateTimeOffset ElapseAt { get; init; }

        public TimerEntity() { }

        public TimerEntity(MutedUserEntity muteUser, TimeSpan time)
        {
            UserId = muteUser.UserId;
            GuildId = muteUser.GuildIdFK;
            ChannelId = null;
            Interval = time;
            IsRepeatable = false;
            IsAbsolute = true;
            Type = TimerType.TimedMute;
            ElapseAt = DateTimeOffset.Now.Add(time);
        }
    }
}