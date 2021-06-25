using DSharpPlus;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

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

        /// <summary>
        /// Creates a collection of human-readable strings of this <see cref="Permissions"/>.
        /// </summary>
        /// <param name="permissions">This permissions.</param>
        /// <remarks>The strings reflect their actual enum value, not the values returned by <see cref="Utilities.ToPermissionString(Permissions)"/>.</remarks>
        /// <returns>The human-readable strings.</returns>
        public static IEnumerable<string> ToPermissionStrings(this Permissions permissions)
        {
            return Enum.GetValues<Permissions>()
                .Where(x => permissions.HasFlag(x))
                .Select(x => x.ToString())
                .OrderBy(x => x);
        }

        /// <summary>
        /// Creates a collection of localized, human-readable strings of this <see cref="Permissions"/>.
        /// </summary>
        /// <param name="permissions">This permissions.</param>
        /// <param name="context">The command context.</param>
        /// <returns>The localized permission strings.</returns>
        public static IEnumerable<string> ToLocalizedStrings(this Permissions permissions, CommandContext context)
        {
            return Enum.GetValues<Permissions>()
                .Where(x => permissions.HasFlag(x))
                .Select(x => context.FormatLocalized("perm_" + x.ToString().ToSnakeCase()))
                .OrderBy(x => x);
        }
    }
}