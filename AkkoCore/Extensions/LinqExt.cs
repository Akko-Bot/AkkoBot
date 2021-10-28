using ConcurrentCollections;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Extensions
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
        /// <exception cref="ArgumentNullException">Occurs when either the collection or the key selector are <see langword="null"/>.</exception>
        public static ConcurrentDictionary<T1, T2> ToConcurrentDictionary<T1, T2>(this IEnumerable<T2> collection, Func<T2, T1> keySelector) where T1 : notnull
        {
            if (collection is null || keySelector is null)
                throw new ArgumentNullException(collection is null ? nameof(collection) : nameof(keySelector), "Argument cannot be null.");

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
        /// <exception cref="ArgumentNullException">Occurs when the collection is <see langword="null"/>.</exception>
        public static ConcurrentHashSet<T> ToConcurrentHashSet<T>(this IEnumerable<T> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection), "Collection cannot be null.");

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
        /// <exception cref="ArgumentNullException">Occurs when either collections are <see langword="null"/>.</exception>
        public static bool ContainsSubcollection<T>(this IEnumerable<T> collection, IEnumerable<T> targetCollection)
        {
            if (collection is null || targetCollection is null)
                throw new ArgumentNullException(collection is null ? nameof(collection) : nameof(targetCollection), "Collection cannot be null.");
            else if (!collection.Any() || !targetCollection.Any())
                return false;

            var matches = 0;

            foreach (var element in targetCollection)
            {
                if (collection.Any(x => x?.Equals(element) is true))
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
        /// <exception cref="ArgumentNullException">Occurs when either collections are <see langword="null"/>.</exception>
        public static bool ContainsOne<T>(this IEnumerable<T> collection, params T[] targetCollection)
            => collection.ContainsOne(targetCollection.AsEnumerable());

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
        /// <exception cref="ArgumentNullException">Occurs when either collections are <see langword="null"/>.</exception>
        public static bool ContainsOne<T>(this IEnumerable<T> collection, IEnumerable<T> targetCollection)
        {
            if (collection is null || targetCollection is null)
                throw new ArgumentNullException(collection is null ? nameof(collection) : nameof(targetCollection), "Collection cannot be null.");

            foreach (var element in targetCollection)
            {
                if (collection.Any(x => x?.Equals(element) is true))
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
        /// <exception cref="ArgumentNullException">Occurs when either the collection or the key selector are <see langword="null"/>.</exception>
        public static IEnumerable<T1> DistinctBy<T1, T2>(this IEnumerable<T1> collection, Func<T1, T2> keySelector)
        {
            if (collection is null || keySelector is null)
                throw new ArgumentNullException(collection is null ? nameof(collection) : nameof(keySelector), "Argument cannot be null.");

            var seenKeys = new HashSet<T2>();

            foreach (var element in collection)
            {
                if (seenKeys.Add(keySelector(element)))
                    yield return element;
            }
        }

        /// <summary>
        /// Converts a collection of <see cref="Task{T}"/> into a <see cref="List{T}"/>.
        /// </summary>
        /// <param name="collection">This collection.</param>
        /// <typeparam name="T">The data that needs to be awaited.</typeparam>
        /// <returns>A <see cref="List{T}"/>.</returns>
        /// <exception cref="ArgumentNullException">Occurs when the collection is <see langword="null"/>.</exception>
        public static async Task<List<T>> ToListAsync<T>(this IEnumerable<Task<T>> collection)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection), "Collection cannot be null.");

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
        /// <exception cref="ArgumentNullException">Occurs when either of the parameters is <see langword="null"/>.</exception>
        public static IEnumerable<T1> ExceptBy<T1, T2>(this IEnumerable<T1> collection, IEnumerable<T1> secondCollection, Func<T1, T2> keySelector)
        {
            if (collection is null || secondCollection is null || keySelector is null)
                throw new ArgumentNullException(collection is null ? nameof(collection) : secondCollection is null ? nameof(secondCollection) : nameof(keySelector), "Argument cannot be null.");

            var seenKeys = new HashSet<T2>(collection.Intersect(secondCollection).Select(x => keySelector(x)));

            foreach (var element in collection)
            {
                if (seenKeys.Add(keySelector(element)))
                    yield return element;
            }

            foreach (var element in secondCollection)
            {
                if (seenKeys.Add(keySelector(element)))
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
        /// <exception cref="ArgumentNullException">Occurs when either of the parameters is <see langword="null"/>.</exception>
        public static IEnumerable<T1> IntersectBy<T1, T2>(this IEnumerable<T1> collection, IEnumerable<T1> secondCollection, Func<T1, T2> keySelector)
        {
            if (collection is null || secondCollection is null || keySelector is null)
                throw new ArgumentNullException(collection is null ? nameof(collection) : secondCollection is null ? nameof(secondCollection) : nameof(keySelector), "Argument cannot be null.");

            var seenKeys = new HashSet<T2>(collection.Select(x => keySelector(x)));
            seenKeys.IntersectWith(secondCollection.Select(x => keySelector(x)));

            foreach (var element in collection.Concat(secondCollection).DistinctBy(x => keySelector(x)))
            {
                if (!seenKeys.Add(keySelector(element)))
                    yield return element;
            }
        }

        /// <summary>
        /// Adds the <typeparamref name="T1"/> defined in <paramref name="sample"/> to the inner collections
        /// of this <see cref="IEnumerable{T}"/> until all of them reach the same amount of elements.
        /// </summary>
        /// <param name="collection">This collection of collections of <typeparamref name="T1"/>.</param>
        /// <param name="sample">The <typeparamref name="T1"/> object to be added to the inner collections.</param>
        /// <typeparam name="T1">Data type contained in the inner collections.</typeparam>
        /// <typeparam name="T2">The type of collections stored.</typeparam>
        /// <returns>A <see cref="List"/> with <see cref="IEnumerable{T}"/> collections of the same size.</returns>
        /// <exception cref="ArgumentNullException">Occurs when either of the parameters is <see langword="null"/>.</exception>
        public static List<List<T1>> Fill<T1, T2>(this IEnumerable<T2> collection, T1 sample) where T2 : IEnumerable<T1>
        {
            if (collection is null || sample is null)
                throw new ArgumentNullException(collection is null ? nameof(collection) : nameof(sample), "Argument cannot be null.");

            var outerCollection = collection.Select(x => x.ToList()).ToList();

            // Get the max count of the inner collections
            var max = 0;
            foreach (var innerCollection in outerCollection)
                max = Math.Max(max, innerCollection.Count);

            // Fill the collections until they have the same size
            for (var index = 0; index < outerCollection.Count; index++)
            {
                while (outerCollection[index].Count != max)
                    outerCollection[index].Add(sample);
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
        /// <exception cref="ArgumentNullException">Occurs when the collection is <see langword="null"/>.</exception>
        public static List<List<T>> SplitInto<T>(this IEnumerable<T> collection, int amount)
        {
            if (collection is null)
                throw new ArgumentNullException(nameof(collection), "Collection cannot be null.");
            else if (!collection.Any())
                return new List<List<T>>(0);

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
        public static ICollection<HashSet<T1>> SplitBy<T1, T2>(this IEnumerable<T1> collection, Func<T1, T2> selector) where T2 : notnull
        {
            if (collection is null || selector is null)
                throw new ArgumentNullException(collection is null ? nameof(collection) : nameof(selector), "Argument cannot be null.");

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

#nullable disable
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
                if (comparer.Compare(selector(previous), selector(enumerator.Current)) <= 0)
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
                if (comparer.Compare(selector(previous), selector(enumerator.Current)) >= 0)
                    result = enumerator.Current;
            }

            return result;
        }
#nullable enable

        /// <summary>
        /// Gets a random <typeparamref name="T"/> from the current collection.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="random">A <see cref="Random"/> instance to generate the random index.</param>
        /// <returns>A random <typeparamref name="T"/> element from this collection or <see langword="default"/>(<typeparamref name="T"/>) if the collection is empty.</returns>
        public static T? RandomElementOrDefault<T>(this IEnumerable<T> collection, Random? random = default)
        {
            random ??= new();
            return collection.ElementAtOrDefault(random.Next(collection.Count()));
        }

        /// <summary>
        /// Gets a random <typeparamref name="T"/> from the current collection.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="collection">This collection.</param>
        /// <param name="maxIndex">The maximum index to pick from.</param>
        /// <param name="random">A <see cref="Random"/> instance to generate the random index.</param>
        /// <returns>A random <typeparamref name="T"/> element from this collection or <see langword="default"/>(<typeparamref name="T"/>) if the collection is empty.</returns>
        public static T? RandomElementOrDefault<T>(this IEnumerable<T> collection, int maxIndex, Random? random = default)
        {
            random ??= new();
            return collection.ElementAtOrDefault(random.Next(Math.Min(collection.Count(), Math.Abs(maxIndex))));
        }
    }
}