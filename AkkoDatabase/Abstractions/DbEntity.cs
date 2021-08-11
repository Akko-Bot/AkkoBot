using AkkoCore.Abstractions;
using AkkoCore.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace AkkoDatabase.Abstractions
{
    /// <summary>
    /// Represents a database table.
    /// </summary>
    public abstract class DbEntity : Settings
    {
        /// <summary>
        /// The primary key of this entity.
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Date and time of when this entity was added to the database.
        /// </summary>
        public DateTimeOffset DateAdded { get; init; } = DateTimeOffset.Now;

        /// <summary>
        /// Gets all properties from this table.
        /// </summary>
        /// <remarks><see cref="Id"/>, <see cref="DateAdded"/>, relationship properties and collections are removed.</remarks>
        /// <returns>A dictionary of setting name/value pairs.</returns>
        public override IReadOnlyDictionary<string, string> GetSettings()
        {
            var props = GetType()
                .GetProperties()
                .Where(x =>
                    !x.PropertyType.IsAssignableTo(typeof(ICollection))
                    && !x.Name.EndsWith("Rel", StringComparison.Ordinal)
                    && x.Name is not (nameof(Id)) and not (nameof(DateAdded))
                );

            var result = new Dictionary<string, string>();

            foreach (var prop in props)
                result.TryAdd(prop.Name.ToSnakeCase(), prop.GetValue(this)?.ToString() ?? "-");

            return result;
        }
    }
}