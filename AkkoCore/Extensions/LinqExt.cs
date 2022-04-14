using ConcurrentCollections;
using System;
using System.Collections.Generic;

namespace AkkoCore.Extensions;

public static class LinqExt
{
    /// <summary>
    /// Saves an <see cref="IEnumerable{T}"/> collection to a concurrent hashset.
    /// </summary>
    /// <typeparam name="T">Type of the values.</typeparam>
    /// <param name="collection">This IEnumerable collection.</param>
    /// <returns>A <see cref="ConcurrentHashSet{T}"/> collection.</returns>
    /// <exception cref="ArgumentNullException">Occurs when the collection is <see langword="null"/>.</exception>
    public static ConcurrentHashSet<T> ToConcurrentHashSet<T>(this IEnumerable<T> collection)
    {
        return (collection is null)
            ? throw new ArgumentNullException(nameof(collection), "Collection cannot be null.")
            : new ConcurrentHashSet<T>(collection);
    }
}