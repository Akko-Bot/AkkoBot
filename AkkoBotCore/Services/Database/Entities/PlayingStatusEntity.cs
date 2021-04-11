using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores data related to the bot's Discord status.")]
    public class PlayingStatusEntity : DbEntity
    {
        private string _message;
        private string _streamUrl;

        [Required]
        [MaxLength(128)]
        public string Message
        {
            get => _message;
            set => _message = value.MaxLength(128);
        }

        [MaxLength(500)]
        public string StreamUrl
        {
            get => _streamUrl;
            set => _streamUrl = value.MaxLength(500);
        }

        public ActivityType Type { get; set; }

        public TimeSpan RotationTime { get; set; }

        /// <summary>
        /// Gets the <see cref="DiscordActivity"/> this entity represents.
        /// </summary>
        /// <returns>A <see cref="DiscordActivity"/>.</returns>
        public DiscordActivity GetActivity()
            => new(Message, Type) { StreamUrl = this.StreamUrl };

        /* Overrides */

        public static bool operator ==(PlayingStatusEntity x, PlayingStatusEntity y)
            => (x.Message == y.Message && x.StreamUrl == y.StreamUrl && x.Type == y.Type && x.RotationTime == y.RotationTime);

        public static bool operator !=(PlayingStatusEntity x, PlayingStatusEntity y)
            => !(x == y);

        public override bool Equals(object obj)
            => ReferenceEquals(this, obj) || (obj is not null && obj is PlayingStatusEntity pStatus && this == pStatus);

        public override int GetHashCode()
            => base.GetHashCode();
    }
}