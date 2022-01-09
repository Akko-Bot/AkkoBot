using AkkoCore.Commands.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AkkoCore.Common;

/// <summary>
/// Defines a comparer for <see cref="DiscordEmbed"/> that compares all its public property values.
/// </summary>
[CommandService<IEqualityComparer<DiscordEmbed>>(ServiceLifetime.Singleton)]
internal sealed class DiscordEmbedComparer : IEqualityComparer<DiscordEmbed>
{
    private readonly IEqualityComparer<DiscordEmbedField> _fieldComparer;

    public DiscordEmbedComparer(IEqualityComparer<DiscordEmbedField> fieldComparer)
        => _fieldComparer = fieldComparer;

    public int GetHashCode([DisallowNull] DiscordEmbed obj)
        => obj.GetHashCode();

    public bool Equals(DiscordEmbed? x, DiscordEmbed? y)
    {
        return ReferenceEquals(x, y)
            || ((x is not null || y is null) && (x is null || y is not null)
            && Equals(x!.Author?.Url?.AbsoluteUri, y!.Author?.Url?.AbsoluteUri)
            && Equals(x.Author?.IconUrl?.ToString(), y.Author?.IconUrl?.ToString())
            && Equals(x.Author?.ProxyIconUrl?.ToString(), y.Author?.ProxyIconUrl?.ToString())
            && Equals(x.Author?.Name, y.Author?.Name)
            && x.Color == y.Color
            && Equals(x.Description, y.Description)
            && Equals(x.Footer?.Text, y.Footer?.Text)
            && Equals(x.Footer?.IconUrl?.ToString(), y.Footer?.IconUrl?.ToString())
            && Equals(x.Footer?.ProxyIconUrl?.ToString(), y.Footer?.ProxyIconUrl?.ToString())
            && x.Image?.Height == y.Image?.Height
            && x.Image?.Width == y.Image?.Width
            && Equals(x.Image?.Url?.ToString(), y.Image?.Url.ToString())
            && Equals(x.Image?.ProxyUrl.ToString(), y.Image?.ProxyUrl?.ToString())
            && Equals(x.Provider?.Url?.AbsoluteUri, y.Provider?.Url?.AbsoluteUri)
            && Equals(x.Provider?.Name, y.Provider?.Name)
            && x.Thumbnail?.Height == y.Thumbnail?.Height
            && x.Thumbnail?.Width == y.Thumbnail?.Width
            && Equals(x.Thumbnail?.Url?.ToString(), y.Thumbnail?.Url?.ToString())
            && Equals(x.Thumbnail?.ProxyUrl?.ToString(), y.Thumbnail?.ProxyUrl?.ToString())
            && x.Timestamp == y.Timestamp // Remove, maybe?
            && Equals(x.Title, y.Title)
            && x.Type == y.Type
            && Equals(x.Url?.AbsoluteUri, y.Url?.AbsoluteUri)
            && ((x.Fields is null && y.Fields is null) || (x.Fields is not null && y.Fields is not null))
            && x.Fields?.All(x => y.Fields?.Contains(x, _fieldComparer) is true) is not false);
    }
}
