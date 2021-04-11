using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Determines the type of the blacklisted entity.
    /// </summary>
    public enum BlacklistType { User, Channel, Server, Unspecified }

    [Comment("Stores users, channels, and servers blacklisted from the bot.")]
    public class BlacklistEntity : DbEntity
    {
        private string _name;
        private string _reason;

        public ulong ContextId { get; init; }

        public BlacklistType Type { get; set; }

        [MaxLength(37)]
        public string Name
        {
            get => _name;
            set => _name = value?.MaxLength(37);
        }

        [MaxLength(200)]
        public string Reason
        {
            get => _reason;
            set => _reason = value?.MaxLength(200);
        }

        /* Overrides */

        public static bool operator ==(BlacklistEntity x, BlacklistEntity y)
            => (x.ContextId == y.ContextId && x.Type == y.Type && x.Name == y.Name && x.Reason == y.Reason);

        public static bool operator !=(BlacklistEntity x, BlacklistEntity y)
            => !(x == y);

        public override bool Equals(object obj)
            => ReferenceEquals(this, obj) || (obj is not null && obj is BlacklistEntity dbAlias && this == dbAlias);

        public override int GetHashCode()
            => base.GetHashCode();
    }
}