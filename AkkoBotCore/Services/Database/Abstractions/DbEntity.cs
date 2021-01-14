using System;
using System.ComponentModel.DataAnnotations;

namespace AkkoBot.Services.Database.Abstractions
{
    /// <summary>
    /// Represents a database table.
    /// </summary>
    public abstract class DbEntity
    {
        [Required]
        public DateTimeOffset DateAdded { get; init; } = DateTimeOffset.Now;
    }
}
