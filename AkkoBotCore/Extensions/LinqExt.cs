using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AkkoBot.Extensions
{
    public static class LinqExt
    {
        /// <summary>
        /// Saves an <see cref="IEnumerable{T2}"/> collection to a concurrent dictionary.
        /// </summary>
        /// <typeparam name="T1">Type of the key.</typeparam>
        /// <typeparam name="T2">Type of the value.</typeparam>
        /// <param name="collection">This IEnumerable collection.</param>
        /// <param name="keySelector">A method that defines the value to be used as the key for the dictionary.</param>
        /// <returns>A <see cref="ConcurrentDictionary{T1, T2}"/> whose key is defined by <paramref name="keySelector"/>.</returns>
        public static ConcurrentDictionary<T1, T2> ToConcurrentDictionary<T1, T2>(this IEnumerable<T2> collection, Func<T2, T1> keySelector)
        {
            var result = new ConcurrentDictionary<T1, T2>();

            foreach (var value in collection)
                result.TryAdd(keySelector(value), value);

            return result;
        }

        /// <summary>
        /// Checks if at least one entry in this collection matches the specified string.
        /// </summary>
        /// <param name="collection">This string collection.</param>
        /// <param name="target">The string to be compared with.</param>
        /// <param name="comparison">The comparison rules.</param>
        /// <param name="match">The resulting match in the collection or <see langword="null"/> if none was found.</param>
        /// <returns><see langword="true"/> if there was one matching entry, <see langword="false"/> otherwise.</returns>
        public static bool Contains(this IEnumerable<string> collection, string target, StringComparison comparison, out string match)
        {
            foreach (var word in collection)
            {
                if (word.Equals(target, comparison))
                {
                    match = word;
                    return true;
                }
            }

            match = null;
            return false;
        }
    }
}