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

        [Required]
        [MaxLength(128)]
        public string Message
        {
            get => _message;
            set => _message = value.MaxLength(128);
        }

        public string StreamUrl { get; set; }

        public ActivityType Type { get; set; }

        public TimeSpan RotationTime { get; set; }

        /// <summary>
        /// Gets the <see cref="DiscordActivity"/> this entity represents.
        /// </summary>
        /// <returns>A <see cref="DiscordActivity"/>.</returns>
        public DiscordActivity GetActivity()
            => new(Message, Type) { StreamUrl = this.StreamUrl };
    }
}