using ConcurrentCollections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

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
        /// Saves an <see cref="IEnumerable{T}"/> collection to a concurrent hashset.
        /// </summary>
        /// <typeparam name="T">Type of the values.</typeparam>
        /// <param name="collection">This IEnumerable collection.</param>
        /// <returns>A <see cref="ConcurrentHashSet{T}"/> collection.</returns>
        public static ConcurrentHashSet<T> ToConcurrentHashSet<T>(this IEnumerable<T> collection)
        {
            var result = new ConcurrentHashSet<T>();

            foreach (var value in collection)
                result.Add(value);

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

            foreach (var element in collection)
            {
                if (seenKeys.Add(keySelector(element)))
                    yield return element;
            }
        }

        /// <summary>
        /// Filters a sequence of values based on an asynchronous predicate.
        /// </summary>
        /// <param name="collection">This collection.</param>
        /// <param name="predicate"></param>
        /// <typeparam name="T">Data type contained in the collection.</typeparam>
        /// <returns>An awaitable collection of <typeparamref name="T"/>.</returns>
        public static async Task<IEnumerable<T>> Where<T>(this IEnumerable<T> collection, Func<T, Task<bool>> predicate)
        {
            var result = new List<T>();

            foreach (var element in collection)
            {
                if (await predicate(element))
                    result.Add(element);
            }

            return result;
        }

        /// <summary>
        /// Converts a collection of <see cref="Task{T}"/> into a <see cref="List{T}"/>.
        /// </summary>
        /// <param name="collection">This collection.</param>
        /// <typeparam name="T">The data that needs to be awaited.</typeparam>
        /// <returns>A <see cref="List{T}"/>.</returns>
        public static async Task<List<T>> ToListAsync<T>(this IEnumerable<Task<T>> collection)
        {
            var result = new List<T>();

            foreach (var element in collection)
                result.Add(await element);

            return result;
        }

        /// <summary>
        /// Gets the symmetric difference between two collections based on the key defined by <paramref name="keySelector"/>.
        /// </summary>
        /// <param name="collection">This collection.</param>
        /// <param name="secondCollection">The second collection to compare with.</param>
        /// <param name="keySelector">A method that defines the property to filter by.</param>
        /// <typeparam name="T1">Data type contained in the collection.</typeparam>
        /// <typeparam name="T2">Data type of the property to be selected.</typeparam>
        /// <returns>A collection of <typeparamref name="T1"/> with the symmetric difference between this <paramref name="collection"/> and <paramref name="secondCollection"/>.</returns>
        public static IEnumerable<T1> ExceptBy<T1, T2>(this IEnumerable<T1> collection, IEnumerable<T1> secondCollection, Func<T1, T2> keySelector)
        {
            var seenKeys = new HashSet<T2>(collection.Select(x => keySelector(x)));

            foreach (var element in secondCollection)
            {
                if (!seenKeys.Remove(keySelector(element)))
                    yield return element;
            }
        }

        /// <summary>
        /// Gets all elements present in this <paramref name="collection"/> and <paramref name="secondCollection"/>
        /// that share the same property defined by <paramref name="keySelector"/>.
        /// </summary>
        /// <param name="collection">This collection.</param>
        /// <param name="secondCollection">The collection to be intersected with.</param>
        /// <param name="keySelector">A method that defines the property to filter by.</param>
        /// <typeparam name="T1">Data type contained in the collection.</typeparam>
        /// <typeparam name="T2">Data type of the property to be selected.</typeparam>
        /// <returns>A collection of intersected <typeparamref name="T1"/> objects.</returns>
        public static IEnumerable<T1> IntersectBy<T1, T2>(this IEnumerable<T1> collection, IEnumerable<T1> secondCollection, Func<T1, T2> keySelector)
        {
            var seenKeys = new HashSet<T2>(collection.Select(x => keySelector(x)));
            seenKeys.IntersectWith(secondCollection.Select(x => keySelector(x)));

            foreach (var element in collection.Concat(secondCollection).DistinctBy(x => keySelector(x)))
            {
                if (seenKeys.Contains(keySelector(element)))
                    yield return element;
            }
        }

        /// <summary>
        /// Adds the <typeparamref name="T"/> defined in <paramref name="sample"/> to the inner collections
        /// of this <see cref="IEnumerable{T}"/> until all of them reach the same amount of elements.
        /// </summary>
        /// <param name="collection">This collection of collections of <typeparamref name="T"/>.</param>
        /// <param name="sample">The <typeparamref name="T"/> object to be added to the inner collections.</param>
        /// <typeparam name="T">Data type contained in the inner collections.</typeparam>
        /// <returns>An <see cref="IEnumerable{T}"/> with collections of the same size.</returns>
        /// <exception cref="NullReferenceException">Occurs when <paramref name="sample"/> is <see langword="null"/>.</exception>
        public static IEnumerable<IEnumerable<T>> Fill<T>(this IEnumerable<IEnumerable<T>> collection, T sample)
        {
            var outerCollection = collection.ToList();

            // Get the max count of the inner collections
            var max = 0;
            foreach (var innerCollection in outerCollection)
                max = Math.Max(max, innerCollection.Count());

            // Fill the collections until they have the same size
            for (var index = 0; index < collection.Count(); index++)
            {
                while (outerCollection[index].Count() != max)
                    outerCollection[index] = outerCollection[index].Append(sample);
            }

            return outerCollection;
        }

        /// <summary>
        /// Splits the elements of this <paramref name="collection"/> into several subcollections by the specified <paramref name="amount"/>.
        /// </summary>
        /// <param name="collection">This collection.</param>
        /// <param name="amount">The maximum amount of elements per subcollection.</param>
        /// <typeparam name="T">Data type of this collection.</typeparam>
        /// <returns>A collection of <see cref="IEnumerable{T}"/>.</returns>
        public static IEnumerable<IEnumerable<T>> SplitInto<T>(this IEnumerable<T> collection, int amount)
        {
            var result = new List<List<T>>() { new List<T>(amount) };
            var index = 0;

            foreach (var element in collection)
            {
                result[index].Add(element);

                if (result[index].Count >= amount)
                {
                    result.Add(new List<T>(amount));
                    index++;
                }
            }

            return result;
        }
    }
}