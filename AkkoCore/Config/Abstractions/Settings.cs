using Kotz.Extensions;
using System.Collections.Generic;

namespace AkkoCore.Config.Abstractions;

/// <summary>
/// Represents any data type that holds settings of some sort.
/// </summary>
public abstract class Settings
{
    /// <summary>
    /// Gets all properties from this object.
    /// </summary>
    /// <returns>A dictionary of setting name/value pairs.</returns>
    public virtual IReadOnlyDictionary<string, string> GetSettings()
    {
        var props = GetType().GetProperties();
        var result = new Dictionary<string, string>();

        foreach (var prop in props)
            result.TryAdd(prop.Name.ToSnakeCase(), prop.GetValue(this)?.ToString() ?? "-");

        return result;
    }
}