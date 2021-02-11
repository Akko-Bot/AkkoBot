using System;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Abstractions
{
    /// <summary>
    /// Represents a database table.
    /// </summary>
    public abstract class DbEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTimeOffset DateAdded { get; init; } = DateTimeOffset.Now;
    }
}
