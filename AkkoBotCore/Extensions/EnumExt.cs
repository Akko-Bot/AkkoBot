using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
        public static bool HasOneFlag<T>(this T value, T flag) where T : struct, Enum
        {
            var newValue = (value as IConvertible).ToInt64(CultureInfo.InvariantCulture);
            var newFlag = (flag as IConvertible).ToInt64(CultureInfo.InvariantCulture);

            return (newValue & newFlag) > 0;
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
        /// Creates a collection of localized, human-readable strings of this <see cref="Permissions"/>.
        /// </summary>
        /// <param name="permissions">This permissions.</param>
        /// <param name="context">The command context.</param>
        /// <returns>The localized permission strings.</returns>
        public static IEnumerable<string> ToLocalizedStrings(this Permissions permissions, CommandContext context)
        {
            return Enum.GetValues<Permissions>()
                .Where(x => x is not Permissions.None && permissions.HasFlag(x))
                .Select(x => context.FormatLocalized("perm_" + x.ToString().ToSnakeCase()))
                .OrderBy(x => x)
                .DefaultIfEmpty(context.FormatLocalized("perm_" + Permissions.None.ToString().ToSnakeCase()));
        }

        /// <summary>
        /// Creates a collection of localized, human-readable strings of this <see cref="Permissions"/>.
        /// </summary>
        /// <param name="permissions">This permissions.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="locale">The locale.</param>
        /// <returns>The localized permission strings.</returns>
        public static IEnumerable<string> ToLocalizedStrings(this Permissions permissions, ILocalizer localizer, string locale)
        {
            return Enum.GetValues<Permissions>()
                .Where(x => x is not Permissions.None && permissions.HasFlag(x))
                .Select(x => localizer.GetResponseString(locale, "perm_" + x.ToString().ToSnakeCase()))
                .OrderBy(x => x)
                .DefaultIfEmpty(localizer.GetResponseString(locale, "perm_" + Permissions.None.ToString().ToSnakeCase()));
        }
    }
}