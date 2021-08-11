using AkkoEntities.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AkkoBot.Extensions
{
    public static class MemoryUtilsExt
    {
        private static readonly int _pointerSize = Unsafe.SizeOf<IntPtr>();
        private static readonly string[] _excludedNamespaces = new string[] { "System.Reflection", "System.Threading" };

        //private static readonly string[] _excludedNamespaces = new string[] { "System" };
        private static readonly HashSet<object> _gmeCache = new();

        /// <summary>
        /// Gets the estimated memory consumption of this object.
        /// </summary>
        /// <param name="obj">This object.</param>
        /// <param name="excludedTypes">A collection of types that should be excluded from the counting.</param>
        /// <remarks>
        /// Pointers to fields that are reference types are counted, but pointers to functions are not.
        /// Anything from the "System.Reflection" and "System.Threading" namespace is also not counted.
        /// </remarks>
        /// <returns>The estimated amount of memory allocated to this object, in bytes.</returns>
        /// <exception cref="ArgumentNullException">Occurs when <paramref name="excludedTypes"/> is <see langword="null"/>.</exception>
        /// <exception cref="StackOverflowException">Occurs when the dependencies of the original object have a circular dependency amongst themselves.</exception>
        public static long GetMemoryEstimate(this object obj, params Type[] excludedTypes)
            => GetMemoryEstimate(obj, excludedTypes?.AsEnumerable());

        /// <summary>
        /// Gets the estimated memory consumption of this object.
        /// </summary>
        /// <param name="obj">This object.</param>
        /// <param name="excludedTypes">A collection of types that should be excluded from the counting.</param>
        /// <remarks>
        /// Pointers to fields that are reference types are counted, but pointers to functions are not.
        /// Anything from the "System.Reflection" and "System.Threading" namespace is also not counted.
        /// </remarks>
        /// <returns>The estimated amount of memory allocated to this object, in bytes.</returns>
        /// <exception cref="ArgumentNullException">Occurs when <paramref name="excludedTypes"/> is <see langword="null"/>.</exception>
        /// <exception cref="StackOverflowException">Occurs when the dependencies of the original object have a circular dependency amongst themselves.</exception>
        public static long GetMemoryEstimate(this object obj, IEnumerable<Type> excludedTypes)
        {
            // If object is null, just return its pointer reference
            if (obj is null)
                return _pointerSize;

            excludedTypes ??= Enumerable.Empty<Type>();
            var result = 0L;
            var objType = obj.GetType();

            if (objType.IsPrimitive)
            {
                // If it is a primitive type, use the Marshal class
                result = Marshal.SizeOf(obj);
            }
            else if (obj is string text)
            {
                // Strings have size of 2 * (length + 1) in C#
                result = 2 * (text.Length + 1);
            }
            else if (obj is ICollection collection)
            {
                // If it is a collection, get the size of each of its elements
                foreach (var element in collection)
                    result += element.GetMemoryEstimate(excludedTypes);
            }
            else if (objType.FullName.Contains(_excludedNamespaces))
            {
                // Types to be ignored
                result += _pointerSize;
            }
            else
            {
                // If it is a class or struct, loop through its members
                var fields = objType.GetRuntimeFields();

                _gmeCache.Add(obj);

                foreach (var field in fields)
                {
                    var value = field.GetValue(obj);

                    if (value is null || field.IsStatic || field.FieldType == objType || _gmeCache.Contains(value))
                    {
                        result += _pointerSize;
                        continue;
                    }

                    var valueType = value.GetType();

                    if (!excludedTypes.Any(x => x.IsAssignableFrom(valueType) || x.IsAssignableTo(valueType)))
                        result += value.GetMemoryEstimate(excludedTypes);

                    // If field is a reference type, include its pointer
                    if (value is not null && !valueType.IsValueType)
                        result += _pointerSize;

                    // If field is a collection of strings, include the pointers of each index
                    if (value is ICollection<string> strings)
                        result += _pointerSize * strings.Count;
                }

                _gmeCache.Remove(obj);

                if (objType.IsValueType)                        // Account bit padding for structs
                    result = (result + 7) & (-_pointerSize);    // Sets result to the next multiple of _pointerSize
            }

            return result;
        }
    }
}