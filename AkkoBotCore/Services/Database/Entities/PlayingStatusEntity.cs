using System;
using System.ComponentModel.DataAnnotations;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores data related to the bot's Discord status.")]
    public class PlayingStatusEntity : DbEntity
    {
        [Required]
        public string Message { get; set; } // What's the maximum size of this thing?
        public ActivityType Type { get; set; }
        public TimeSpan RotationTime { get; set; } // Move this to BotConfigs?
    }
}
