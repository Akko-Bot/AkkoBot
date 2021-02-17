using System;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AkkoBot.Services.Database.Entities
{
    public enum WarnPunishType { Mute, Kick, Softban, Ban, AddRole, RemoveRole }

    [Comment("Stores punishments to be automatically applied once a user reaches a certain amount of warnings.")]
    public class WarnPunishEntity : DbEntity
    {
        public GuildConfigEntity GuildConfigRel { get; set; }

        public ulong GuildIdFK { get; set; }
        public int WarnAmount { get; set; }
        public WarnPunishType Type { get; set; }
        public TimeSpan? Interval { get; set; }
        public ulong? PunishRoleId { get; set; }
    }
}