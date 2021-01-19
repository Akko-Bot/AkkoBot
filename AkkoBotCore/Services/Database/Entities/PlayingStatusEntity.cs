using System;
using System.ComponentModel.DataAnnotations;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;

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
            set => _message = value?.MaxLength(128);
        }
        public ActivityType Type { get; set; }
        public TimeSpan RotationTime { get; set; } // Move this to BotConfigs?
    }
}
