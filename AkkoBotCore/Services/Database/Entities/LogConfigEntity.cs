using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Services.Database.Entities
{
    public class LogConfigEntity : DbEntity
    {
        private string _logFormat = "Default";

        public BotConfigEntity BotConfigRel { get; set; }

        [Key]
        public ulong BotIdRef { get; set; }

        [Required]
        [MaxLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string LogFormat
        {
            get => _logFormat;
            set => _logFormat = value?.MaxLength(20);
        }

        [Column(TypeName = "varchar")]
        public string LogTimeFormat { get; set; }

        [Required]
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        [Required]
        public bool IsLoggedToFile { get; set; } = false;

        [Required]
        public double LogSizeMB { get; set; } = 1.0;

        public LogConfigEntity() { }

        public LogConfigEntity(ulong id)
            => BotIdRef = id;

        public IDictionary<string, string> GetSettings()
        {
            var props = this.GetType().GetProperties();
            var result = new Dictionary<string, string>(props.Length);

            // Index should skip undesirable props at the start
            for (int index = 2; index < props.Length - 1; index++)
            {
                result.TryAdd(
                    props[index].Name.ToSnakeCase(),
                    props[index].GetValue(this)?.ToString()
                );
            }

            return result;
        }
    }
}