using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores repeater data and the context it should be sent to.")]
    public class RepeaterEntity : DbEntity
    {
        private string _content;

        public GuildConfigEntity GuildConfigRel { get; set; }

        public int TimerId { get; init; }

        [Required]
        [MaxLength(2000)]
        public string Content
        {
            get => _content;
            init => _content = value?.MaxLength(2000) ?? "-";
        }

        public ulong GuildIdFK { get; init; }
        public ulong AuthorId { get; init; }
        public ulong ChannelId { get; init; }
        public TimeSpan Interval { get; init; }
    }
}
