using System;
using System.Globalization;

namespace AkkoBot.Extensions
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
        public static bool HasOneFlag<T>(this T value, T flag) where T : Enum
        {
            var newValue = value as IConvertible;
            var newFlag = flag as IConvertible;

            return (newValue.ToInt64(CultureInfo.InvariantCulture) & newFlag.ToInt64(CultureInfo.InvariantCulture)) > 0;
        }
    }
}