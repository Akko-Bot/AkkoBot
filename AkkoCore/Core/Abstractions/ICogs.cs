using AkkoCore.Config.Abstractions;
using AkkoCore.Models.Cogs;
using System.Collections.Generic;

namespace AkkoCore.Core.Abstractions;

/// <summary>
/// Contains metadata for loaded cogs with an <see cref="ICogSetup"/>.
/// </summary>
public interface ICogs
{
    /// <summary>
    /// Collection of cog names and their headers.
    /// </summary>
    public IReadOnlyDictionary<string, CogHeader> Headers { get; }
}