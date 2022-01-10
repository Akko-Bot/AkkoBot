using AkkoCore.Commands.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AkkoCore.Common;

/// <summary>
/// Defines a comparer for <see cref="DiscordEmbedField"/> that compares all its public property values.
/// </summary>
[CommandService<IEqualityComparer<DiscordEmbedField>>(ServiceLifetime.Singleton)]
internal sealed class DiscordEmbedFieldComparer : IEqualityComparer<DiscordEmbedField>
{
    public int GetHashCode([DisallowNull] DiscordEmbedField obj)
        => obj.GetHashCode();

    public bool Equals(DiscordEmbedField? x, DiscordEmbedField? y)
    {
        return ReferenceEquals(x, y)
            || ((x is not null || y is null) && (x is null || y is not null)
                && x!.Inline == y!.Inline
                && Equals(x.Value, y.Value)
                && Equals(x.Name, y.Name));
    }
}