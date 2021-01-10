using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AkkoBot.Extensions
{
    public static class LinqExt
    {
        public static ConcurrentDictionary<T1, T2> ToConcurrentDictionary<T1, T2>(this IEnumerable<T2> collection, Func<T2, T1> keySelector)
        {
            var result = new ConcurrentDictionary<T1, T2>();

            foreach (var value in collection)
                result.TryAdd(keySelector(value), value);

            return result;
        }
    }
}