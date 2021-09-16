using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AkkoCore.Extensions
{
    public static class EnumExt
    {
        /// <summary>
        /// Determines whether at least one of the provided bit fields is set in the current instance.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        /// <param name="value">This enum.</param>
        /// <param name="flag">An enumeration value.</param>
        /// <returns><see langword="true"/> if at least one bit field set in <paramref name="flag"/> is also set in the current instance, <see langword="false"/> otherwise.</returns>
        public static bool HasOneFlag<T>(this T value, T flag) where T : struct, Enum
        {
            var newValue = (value as IConvertible).ToInt64(CultureInfo.InvariantCulture);
            var newFlag = (flag as IConvertible).ToInt64(CultureInfo.InvariantCulture);

            return (newValue & newFlag) > 0;
        }

        /// <summary>
        /// Gets the bitwise flags of this collection of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        /// <param name="values">This collection of enums.</param>
        /// <returns>A <typeparamref name="T"/> object with its flags set to the collection's enums.</returns>
        public static T ToFlags<T>(this IEnumerable<T> values) where T : struct, Enum
        {
            var result = default(T);

            foreach (var value in values)
                result = CombineFlags(result, value);

            return result;
        }

        /// <summary>
        /// Creates a collection of human-readable strings of this <see cref="Permissions"/>.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        /// <param name="permissions">This permissions.</param>
        /// <remarks>Only works for enums marked with the <see cref="FlagsAttribute"/>.</remarks>
        /// <returns>The human-readable strings.</returns>
        public static IEnumerable<string> ToStrings<T>(this T permissions) where T : struct, Enum
        {
            return Enum.GetValues<T>()
                .Where(x => x.HasOneFlag(permissions))
                .Select(x => x.ToString())
                .OrderBy(x => x);
        }

        /// <summary>
        /// Combines two enum flags.
        /// </summary>
        /// <typeparam name="T">The type of the enum.</typeparam>
        /// <param name="x">The first flag.</param>
        /// <param name="y">The second flag.</param>
        /// <returns>The combined flags.</returns>
        /// <exception cref="NotSupportedException">Occurs when the enum can't be represented by a native CLR integer type.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T CombineFlags<T>(T x, T y) where T : struct, Enum
        {
            if (Unsafe.SizeOf<T>() is sizeof(byte))
            {
                var result = (byte)(Unsafe.As<T, byte>(ref x) | Unsafe.As<T, byte>(ref y));
                return Unsafe.As<byte, T>(ref result);
            }
            else if (Unsafe.SizeOf<T>() is sizeof(short))
            {
                var result = (short)(Unsafe.As<T, short>(ref x) | Unsafe.As<T, short>(ref y));
                return Unsafe.As<short, T>(ref result);
            }
            else if (Unsafe.SizeOf<T>() is sizeof(int))
            {
                var result = Unsafe.As<T, int>(ref x) | Unsafe.As<T, int>(ref y);
                return Unsafe.As<int, T>(ref result);
            }
            else if (Unsafe.SizeOf<T>() is sizeof(long))
            {
                var result = Unsafe.As<T, long>(ref x) | Unsafe.As<T, long>(ref y);
                return Unsafe.As<long, T>(ref result);
            }

            throw new NotSupportedException($"Enum of size {Unsafe.SizeOf<T>()} has no corresponding CLR integer type.");
        }
    }
}