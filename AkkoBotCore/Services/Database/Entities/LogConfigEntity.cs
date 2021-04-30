using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores data and settings related to how the bot logs command usage.")]
    public class LogConfigEntity : DbEntity
    {
        private string _logFormat = "Default";
        private string _logTimeFormat;
        private string _logTimeStamp;

        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string LogFormat
        {
            get => _logFormat;
            set => _logFormat = value?.MaxLength(20);
        }

        [MaxLength(20)]
        [Column(TypeName = "varchar")]
        public string LogTimeFormat
        {
            get => _logTimeFormat;
            set => _logTimeFormat = value?.MaxLength(20);
        }

        [MaxLength(20)]
        [Column(TypeName = "varchar")]
        public string LogTimeStamp
        {
            get => _logTimeStamp;
            set => _logTimeStamp = value?.MaxLength(20);
        }

        public bool IsLoggedToFile { get; set; } = false;

        public double LogSizeMB { get; set; } = 1.0;
    }
}