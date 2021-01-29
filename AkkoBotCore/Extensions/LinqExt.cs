using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

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
        /// Checks if at least one entry in this collection matches the specified string and returns the match, if it exists.
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

        /// <summary>
        /// Checks if the current collection contains all elements of a given collection.
        /// </summary>
        /// <typeparam name="T">Data type contained in the collection.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="targetCollection">The collection to be compared to.</param>
        /// <returns>
        /// <see langword="true"/> if all elements contained in <paramref name="targetCollection"/> are present
        /// in <paramref name="collection"/>, <see langword="false"/> otherwise.
        /// </returns>
        public static bool ContainsSubcollection<T>(this IEnumerable<T> collection, IEnumerable<T> targetCollection)
        {
            var matches = 0;

            foreach (var element in targetCollection)
            {
                if (collection.Any(x => x.Equals(element)))
                    matches++;
            }

            return matches == targetCollection.Count();
        }

        /// <summary>
        /// Filters a collection so each element has a unique property defined by <paramref name="keySelector"/>.
        /// </summary>
        /// <typeparam name="T1">Data type contained in the collection.</typeparam>
        /// <typeparam name="T2">Data type of the property to be selected.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="keySelector">A method that defines the property to filter by.</param>
        /// <returns>A collection of <typeparamref name="T1"/> with unique properties defined by <paramref name="keySelector"/>.</returns>
        public static IEnumerable<T1> DistinctBy<T1, T2>(this IEnumerable<T1> collection, Func<T1, T2> keySelector)
        {
            var seenKeys = new HashSet<T2>();

            foreach (T1 element in collection)
            {
                if (seenKeys.Add(keySelector(element)))
                    yield return element;
            }
        }
    }
}