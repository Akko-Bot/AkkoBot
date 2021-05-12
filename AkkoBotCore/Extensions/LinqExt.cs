using ConcurrentCollections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
        /// Checks if the current collection contains all elements of a given collection.
        /// </summary>
        /// <typeparam name="T">Data type contained in the collection.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="targetCollection">The collection to compare to.</param>
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
        /// Checks if the current collection contains at least one element of a given collection.
        /// </summary>
        /// <typeparam name="T">Data type contained in the collection.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="targetCollection">The collection to compare to.</param>
        /// <returns>
        /// <see langword="true"/> if at least one element contained in <paramref name="targetCollection"/> is present
        /// in <paramref name="collection"/>, <see langword="false"/> otherwise.
        /// </returns>
        public static bool ContainsOne<T>(this IEnumerable<T> collection, params T[] targetCollection)
            => ContainsOne(collection, targetCollection.AsEnumerable());

        /// <summary>
        /// Checks if the current collection contains at least one element of a given collection.
        /// </summary>
        /// <typeparam name="T">Data type contained in the collection.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="targetCollection">The collection to compare to.</param>
        /// <returns>
        /// <see langword="true"/> if at least one element contained in <paramref name="targetCollection"/> is present
        /// in <paramref name="collection"/>, <see langword="false"/> otherwise.
        /// </returns>
        public static bool ContainsOne<T>(this IEnumerable<T> collection, IEnumerable<T> targetCollection)
        {
            foreach (var element in targetCollection)
            {
                if (collection.Any(x => x.Equals(element)))
                    return true;
            }

            return false;
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
        /// Creates a collection from an asynchronous predicate.
        /// </summary>
        /// <param name="collection">This collection.</param>
        /// <param name="predicate">A method to test each element for a condition.</param>
        /// <typeparam name="T">Data type contained in the collection.</typeparam>
        /// <returns>An awaitable collection of <typeparamref name="T"/>.</returns>
        public static async Task<IEnumerable<T>> ToListAsync<T>(this IEnumerable<T> collection, Func<T, Task<bool>> predicate)
        {
            var result = new List<T>();

            foreach (var element in collection)
            {
                if (await predicate(element).ConfigureAwait(false))
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
                result.Add(await element.ConfigureAwait(false));

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
        /// <returns>A <see cref="List{T}"/> with collections of the same size.</returns>
        /// <exception cref="NullReferenceException">Occurs when <paramref name="sample"/> is <see langword="null"/>.</exception>
        public static List<IEnumerable<T>> Fill<T>(this IEnumerable<IEnumerable<T>> collection, T sample)
        {
            var outerCollection = collection.ToList();

            // Get the max count of the inner collections
            var max = 0;
            foreach (var innerCollection in outerCollection)
                max = Math.Max(max, innerCollection.Count());

            // Fill the collections until they have the same size
            for (var index = 0; index < outerCollection.Count; index++)
            {
                var innerCount = outerCollection[index].Count();

                while (innerCount != max)
                    outerCollection[index] = outerCollection[index].Append(sample);
            }

            return outerCollection;
        }

        /// <summary>
        /// Splits the elements of this <paramref name="collection"/> into several subcollections with maximum length specified by <paramref name="amount"/>.
        /// </summary>
        /// <param name="collection">This collection.</param>
        /// <param name="amount">The maximum amount of elements per subcollection.</param>
        /// <typeparam name="T">Data type of this collection.</typeparam>
        /// <returns>A collection of <see cref="List{T}"/> with maximum length of <paramref name="amount"/>.</returns>
        public static List<List<T>> SplitInto<T>(this IEnumerable<T> collection, int amount)
        {
            int index = 0, count = 0;
            var collectionCount = collection.Count();
            var result = new List<List<T>>() { new List<T>(Math.Min(amount, collectionCount)) };

            foreach (var element in collection)
            {
                result[index].Add(element);

                if (++count != collectionCount && result[index].Count >= amount && ++index <= collectionCount - 1)
                    result.Add(new List<T>(Math.Min(amount, collectionCount - (amount * index))));
            }

            return result;
        }

        /// <summary>
        /// Splits the elements of this <paramref name="collection"/> into several subcollections according to the value of the property defined by <paramref name="selector"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the elements.</typeparam>
        /// <typeparam name="T2">Type of the selected property.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="selector">A method that defines the property to filter the elements.</param>
        /// <returns>An <see cref="IEnumerable{T1}"/> where all <typeparamref name="T1"/> have the same value for the property defined by <paramref name="selector"/>.</returns>
        /// <exception cref="ArgumentNullException">Occurs when the predicate returns a null value.</exception>
        public static IEnumerable<HashSet<T1>> SplitBy<T1, T2>(this IEnumerable<T1> collection, Func<T1, T2> selector)
        {
            var result = new Dictionary<T2, HashSet<T1>>();

            foreach (var element in collection)
            {
                var key = selector(element);

                if (result.ContainsKey(key))
                    result[key].Add(element);
                else
                    result.TryAdd(key, new HashSet<T1>() { element });
            }

            return result.Values;
        }

        /// <summary>
        /// Gets the <typeparamref name="T1"/> with the maximum property value defined by <paramref name="selector"/> in this collection.
        /// </summary>
        /// <typeparam name="T1">Type of the elements.</typeparam>
        /// <typeparam name="T2">Type of the selected property.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="selector">A method that defines the property to compare the elements with.</param>
        /// <param name="comparer">The comparer to compare the objects in the collection.</param>
        /// <returns>The <typeparamref name="T1"/> with the highest value of <typeparamref name="T2"/>.</returns>
        public static T1 MaxBy<T1, T2>(this IEnumerable<T1> collection, Func<T1, T2> selector, IComparer<T2> comparer = default)
        {
            using var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();  // Start iteration

            T1 result = default, previous = enumerator.Current;
            comparer ??= Comparer<T2>.Default;

            while (enumerator.MoveNext())
            {
                if (comparer.Compare(selector(previous), selector(enumerator.Current)) < 0)
                    result = enumerator.Current;
            }

            return result;
        }

        /// <summary>
        /// Gets the <typeparamref name="T1"/> with the minimum property value defined by <paramref name="selector"/> in this collection.
        /// </summary>
        /// <typeparam name="T1">Type of the elements.</typeparam>
        /// <typeparam name="T2">Type of the selected property.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="selector">A method that defines the property to compare the elements with.</param>
        /// <param name="comparer">The comparer to compare the objects in the collection.</param>
        /// <returns>The <typeparamref name="T1"/> with the lowest value of <typeparamref name="T2"/>.</returns>
        public static T1 MinBy<T1, T2>(this IEnumerable<T1> collection, Func<T1, T2> selector, IComparer<T2> comparer = default)
        {
            using var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();  // Start iteration

            T1 result = default, previous = enumerator.Current;
            comparer ??= Comparer<T2>.Default;

            while (enumerator.MoveNext())
            {
                if (comparer.Compare(selector(previous), selector(enumerator.Current)) > 0)
                    result = enumerator.Current;
            }

            return result;
        }

        /// <summary>
        /// Gets the <typeparamref name="T1"/> with the maximum property value defined by <paramref name="selector"/>
        /// in this collection that conforms to the specified <paramref name="predicate"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the elements.</typeparam>
        /// <typeparam name="T2">Type of the selected property.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="selector">A method that defines the property to compare the elements with.</param>
        /// <param name="predicate">A method to test each element for a condition.</param>
        /// <param name="comparer">The comparer to compare the objects in the collection.</param>
        /// <returns>
        /// The <typeparamref name="T1"/> with the highest value of <typeparamref name="T2"/>
        /// or <see langword="default"/> if no element matched the <paramref name="predicate"/>.
        /// </returns>
        public static T1 WhereMax<T1, T2>(this IEnumerable<T1> collection, Func<T1, T2> selector, Predicate<T1> predicate, IComparer<T2> comparer = default)
        {
            using var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();  // Start iteration

            var result = enumerator.Current;
            comparer ??= Comparer<T2>.Default;

            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current) && comparer.Compare(selector(result), selector(enumerator.Current)) < 0)
                    result = enumerator.Current;
            }

            return (predicate(result)) ? result : default;
        }

        /// <summary>
        /// Gets the <typeparamref name="T1"/> with the minimum property value defined by <paramref name="selector"/>
        /// in this collection that conforms to the specified <paramref name="predicate"/>.
        /// </summary>
        /// <typeparam name="T1">Type of the elements.</typeparam>
        /// <typeparam name="T2">Type of the selected property.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="selector">A method that defines the property to compare the elements with.</param>
        /// <param name="predicate">A method to test each element for a condition.</param>
        /// <param name="comparer">The comparer to compare the objects in the collection.</param>
        /// <returns>
        /// The <typeparamref name="T1"/> with the lowest value of <typeparamref name="T2"/>
        /// or <see langword="default"/> if no element matched the <paramref name="predicate"/>.
        /// </returns>
        public static T1 WhereMin<T1, T2>(this IEnumerable<T1> collection, Func<T1, T2> selector, Predicate<T1> predicate, IComparer<T2> comparer = default)
        {
            using var enumerator = collection.GetEnumerator();
            enumerator.MoveNext();  // Start iteration

            var result = enumerator.Current;
            comparer ??= Comparer<T2>.Default;

            while (enumerator.MoveNext())
            {
                if (predicate(enumerator.Current) && comparer.Compare(selector(result), selector(enumerator.Current)) > 0)
                    result = enumerator.Current;
            }

            return (predicate(result)) ? result : default;
        }
    }
}