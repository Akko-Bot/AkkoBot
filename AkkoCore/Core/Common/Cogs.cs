using AkkoCore.Config.Abstractions;
using AkkoCore.Core.Abstractions;
using AkkoCore.Models.Cogs;
using System.Collections.Generic;
using System.Linq;

namespace AkkoCore.Core.Common;

// TODO: include command names
/// <summary>
/// Stores metadata of cogs.
/// </summary>
public sealed class Cogs : ICogs
{
    public IReadOnlyDictionary<string, CogHeader> Headers { get; }

    public Cogs(IEnumerable<ICogSetup> cogSetups)
    {
        Headers = cogSetups
            .Select(x => new CogHeader(x))
            .ToDictionary(x => x.Name);
    }
}