using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores reminder data and the context it should be sent to.")]
    public class ReminderEntity : DbEntity
    {
        private string _content;

        public int TimerId { get; init; }

        [Required]
        [MaxLength(2000)]
        public string Content 
        { 
            get => _content; 
            init => _content = value?.MaxLength(2000) ?? "-";
        }

        public ulong? GuildId { get; init; }
        public ulong AuthorId { get; init; }
        public ulong ChannelId { get; init; }
        public bool IsPrivate { get; init; }
        public DateTimeOffset ElapseAt { get; init; }
    }
}
