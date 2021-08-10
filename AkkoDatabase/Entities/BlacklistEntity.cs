using AkkoDatabase.Abstractions;
using AkkoDatabase.Enums;
using AkkoEntities.Extensions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AkkoDatabase.Entities
{
    /// <summary>
    /// Stores a user, channel, or guild blacklisted from the bot.
    /// </summary>
    [Comment("Stores users, channels, and servers blacklisted from the bot.")]
    public class BlacklistEntity : DbEntity
    {
        private string _name;
        private string _reason;

        /// <summary>
        /// The blacklisted ID.
        /// </summary>
        public ulong ContextId { get; init; }

        /// <summary>
        /// The type of ID this entity holds.
        /// </summary>
        public BlacklistType Type { get; set; }

        /// <summary>
        /// The name of the blacklisted entity.
        /// </summary>
        [MaxLength(37)]
        public string Name
        {
            get => _name;
            set => _name = value?.MaxLength(37);
        }

        /// <summary>
        /// The reason for the blacklisting.
        /// </summary>
        [MaxLength(200)]
        public string Reason
        {
            get => _reason;
            set => _reason = value?.MaxLength(200);
        }

        /* Overrides */

        public static bool operator ==(BlacklistEntity x, BlacklistEntity y)
            => x.ContextId == y.ContextId && x.Type == y.Type && x.Name == y.Name && x.Reason == y.Reason;

        public static bool operator !=(BlacklistEntity x, BlacklistEntity y)
            => !(x == y);

        public override bool Equals(object obj)
            => ReferenceEquals(this, obj) || (obj is not null && obj is BlacklistEntity dbAlias && this == dbAlias);

        public override int GetHashCode()
            => base.GetHashCode();
    }
}