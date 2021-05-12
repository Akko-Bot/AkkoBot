using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoBot.Services.Database.Entities
{
    /// <summary>
    /// Stores data and settings related to how the bot logs command usage.
    /// </summary>
    [Comment("Stores data and settings related to how the bot logs command usage.")]
    public class LogConfigEntity : DbEntity
    {
        private string _logFormat = "Default";
        private string _logTimeFormat;
        private string _logTimeStamp;

        /// <summary>
        /// The minimum severity level of logs that should be registered.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// The formatting to be used on logs.
        /// </summary>
        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string LogFormat
        {
            get => _logFormat;
            set => _logFormat = value?.MaxLength(20);
        }

        /// <summary>
        /// The time format to be used on timestamps.
        /// </summary>
        [MaxLength(20)]
        [Column(TypeName = "varchar")]
        public string LogTimeFormat
        {
            get => _logTimeFormat;
            set => _logTimeFormat = value?.MaxLength(20);
        }

        /// <summary>
        /// The time format to be used on log file names.
        /// </summary>
        [MaxLength(20)]
        [Column(TypeName = "varchar")]
        public string LogTimeStamp
        {
            get => _logTimeStamp;
            set => _logTimeStamp = value?.MaxLength(20);
        }

        /// <summary>
        /// Determines whether logs should be written to a file.
        /// </summary>
        public bool IsLoggedToFile { get; set; } = false;

        /// <summary>
        /// Determines the maximum size of a log file, in megabytes.
        /// </summary>
        public double LogSizeMB { get; set; } = 1.0;
    }
}