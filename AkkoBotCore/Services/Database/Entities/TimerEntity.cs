using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Represents the type of action this timer runs when it triggers.
    /// </summary>
    public enum TimerType
    {
        /// <summary>
        /// Represents the timer for an autocommand.
        /// </summary>
        Command,

        /// <summary>
        /// Represents the timer for a reminder.
        /// </summary>
        Reminder,

        /// <summary>
        /// Represents the timer for a repeater.
        /// </summary>
        Repeater,

        /// <summary>
        /// Represents the timer for a scheduled unban.
        /// </summary>
        TimedBan,

        /// <summary>
        /// Represents the timer for a scheduled unmute.
        /// </summary>
        TimedMute,

        /// <summary>
        /// Represents the timer for adding a role to a Discord user.
        /// </summary>
        TimedRole,

        /// <summary>
        /// Represents the timer for removing a role from a Discord user.
        /// </summary>
        TimedUnrole,

        /// <summary>
        /// Represents the timer for removing old warnings from the database.
        /// </summary>
        TimedWarn
    }

    /// <summary>
    /// Stores a timer that executes actions at some point in the future.
    /// </summary>
    [Comment("Stores a timer that executes actions at some point in the future.")]
    public class TimerEntity : DbEntity
    {
        /// <summary>
        /// The ID of the Discord user this timer is associated with.
        /// </summary>
        public ulong? UserId { get; init; }

        /// <summary>
        /// The ID of the Discord guild this timer is associated with.
        /// </summary>
        public ulong? GuildId { get; init; }

        /// <summary>
        /// The ID of the Discord channel this timer is associated with.
        /// </summary>
        public ulong? ChannelId { get; init; }

        /// <summary>
        /// The ID of the Discord role this timer is associated with.
        /// </summary>
        public ulong? RoleId { get; init; }

        /// <summary>
        /// Determines whether this timer is supposed to trigger multiple times.
        /// </summary>
        public bool IsRepeatable { get; init; }

        /// <summary>
        /// The type of this timer.
        /// </summary>
        public TimerType Type { get; init; }

        /// <summary>
        /// The time interval for the activation of this timer.
        /// </summary>
        public TimeSpan Interval { get; set; }

        /// <summary>
        /// The time of day this timer is supposed to trigger.
        /// </summary>
        public TimeSpan? TimeOfDay { get; init; }

        /// <summary>
        /// The date and time this timer is supposed to trigger.
        /// </summary>
        public DateTimeOffset ElapseAt { get; set; }

        /// <summary>
        /// Gets the time interval this timer is meant to trigger.
        /// </summary>
        /// <remarks>This property is not mapped.</remarks>
        [NotMapped]
        public TimeSpan ElapseIn
            => ElapseAt.Subtract(DateTimeOffset.Now);

        public TimerEntity()
        {
        }

        public TimerEntity(TimerEntity timer)
        {
            Id = timer.Id;
            DateAdded = timer.DateAdded;
            UserId = timer.UserId;
            GuildId = timer.GuildId;
            ChannelId = timer.ChannelId;
            RoleId = timer.RoleId;
            Interval = timer.Interval;
            IsRepeatable = timer.IsRepeatable;
            Type = timer.Type;
            ElapseAt = timer.ElapseAt;
        }

        public TimerEntity(MutedUserEntity muteUser, TimeSpan time)
        {
            UserId = muteUser.UserId;
            GuildId = muteUser.GuildIdFK;
            ChannelId = null;
            RoleId = null;
            Interval = time;
            IsRepeatable = false;
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
            Type = TimerType.TimedWarn;
            ElapseAt = warning.DateAdded.Add(time);
        }

        /* Overrides */

        public static bool operator ==(TimerEntity x, TimerEntity y)
            => (x.UserId == y.UserId && x.GuildId == y.GuildId && x.ChannelId == y.ChannelId && x.RoleId == y.RoleId && x.Interval == y.Interval)
            && (x.IsRepeatable == y.IsRepeatable && x.Type == y.Type);

        public static bool operator !=(TimerEntity x, TimerEntity y)
            => !(x == y);

        public override bool Equals(object obj)
            => ReferenceEquals(this, obj) || (obj is not null && obj is TimerEntity dbTimer && this == dbTimer);

        public override int GetHashCode()
            => base.GetHashCode();
    }
}