using AkkoDatabase.Abstractions;
using AkkoDatabase.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoDatabase.Entities
{
    /// <summary>
    /// Stores a timer that executes actions at some point in the future.
    /// </summary>
    [Comment("Stores a timer that executes actions at some point in the future.")]
    public class TimerEntity : DbEntity
    {
        /// <summary>
        /// The Discord user associated with this timer.
        /// </summary>
        public DiscordUserEntity UserRel { get; init; }

        /// <summary>
        /// The Discord guild this timer is associated with.
        /// </summary>
        /// <remarks>This property is <see langword="null"/> if this timer is not associated with a Discord guild (triggers in direct message).</remarks>
        public GuildConfigEntity GuildConfigRel { get; init; }

        /// <summary>
        /// The infraction this timer is associated with.
        /// </summary>
        public WarnEntity WarnRel { get; init; }

        /// <summary>
        /// The ID of the Discord user this timer is associated with.
        /// </summary>
        public ulong UserIdFK { get; init; }

        /// <summary>
        /// The ID of the Discord guild this timer is associated with.
        /// </summary>
        public ulong? GuildIdFK { get; init; }

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
        /// Determines whether this timer is active or not.
        /// </summary>
        public bool IsActive { get; set; } = true;

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
            UserIdFK = timer.UserIdFK;
            GuildIdFK = timer.GuildIdFK;
            ChannelId = timer.ChannelId;
            RoleId = timer.RoleId;
            Interval = timer.Interval;
            IsRepeatable = timer.IsRepeatable;
            Type = timer.Type;
            ElapseAt = timer.ElapseAt;
        }

        public TimerEntity(MutedUserEntity muteUser, TimeSpan time)
        {
            UserIdFK = muteUser.UserId;
            GuildIdFK = muteUser.GuildIdFK;
            ChannelId = null;
            RoleId = null;
            Interval = time;
            IsRepeatable = false;
            Type = TimerType.TimedMute;
            ElapseAt = muteUser.DateAdded.Add(time);
        }

        public TimerEntity(WarnEntity warning, TimeSpan time)
        {
            UserIdFK = warning.UserIdFK;
            GuildIdFK = warning.GuildIdFK;
            ChannelId = null;
            RoleId = null;
            Interval = time;
            IsRepeatable = false;
            IsActive = time != TimeSpan.Zero;
            Type = TimerType.TimedWarn;
            ElapseAt = warning.DateAdded.Add(time);
        }

        /* Overrides */

        public static bool operator ==(TimerEntity x, TimerEntity y)
            => x.UserIdFK == y.UserIdFK && x.GuildIdFK == y.GuildIdFK && x.ChannelId == y.ChannelId && x.RoleId == y.RoleId && x.Interval == y.Interval
            && x.IsRepeatable == y.IsRepeatable && x.Type == y.Type;

        public static bool operator !=(TimerEntity x, TimerEntity y)
            => !(x == y);

        public override bool Equals(object obj)
            => ReferenceEquals(this, obj) || (obj is not null && obj is TimerEntity dbTimer && this == dbTimer);

        public override int GetHashCode()
            => base.GetHashCode();
    }
}