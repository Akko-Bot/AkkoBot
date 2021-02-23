using AkkoBot.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AkkoBot.Services.Database.Abstractions
{
    /// <summary>
    /// Represents a database table.
    /// </summary>
    public abstract class DbEntity
    {
        [Key]
        public int Id { get; set; }

        public DateTimeOffset DateAdded { get; init; } = DateTimeOffset.Now;

        /// <summary>
        /// Gets all properties from this table.
        /// </summary>
        /// <remarks><see cref="Id"/>, <see cref="DateAdded"/> and relationship properties are removed.</remarks>
        /// <returns>A dictionary of setting name/value pairs.</returns>
        public IReadOnlyDictionary<string, string> GetSettings()
        {
            var props = this.GetType().GetProperties()
                .Where(x => !x.Name.EndsWith("Rel", StringComparison.Ordinal) && x.Name is not (nameof(Id)) and not (nameof(DateAdded)))
                .ToArray();

            var result = new Dictionary<string, string>(props.Length);

            foreach (var prop in props)
                result.TryAdd(prop.Name.ToSnakeCase(), prop.GetValue(this)?.ToString() ?? "-");

            return result;
        }
    }
}
