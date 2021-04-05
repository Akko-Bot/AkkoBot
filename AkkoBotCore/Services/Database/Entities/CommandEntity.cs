using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Entities
{
    public enum CommandType { Startup, Scheduled, Repeated }

    [Comment("Stores command data and the context it should be sent to.")]
    public class CommandEntity : DbEntity
    {
        private string _commandString;

        public int? TimerId { get; init; }

        [Required]
        [MaxLength(2000)]
        public string CommandString
        {
            get => _commandString;
            init => _commandString = value?.MaxLength(2000) ?? "-";
        }

        public ulong GuildId { get; init; }
        public ulong AuthorId { get; init; }
        public ulong ChannelId { get; init; }
        public CommandType Type { get; set; }
    }
}
