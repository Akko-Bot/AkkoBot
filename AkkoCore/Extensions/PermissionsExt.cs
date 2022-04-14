using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Kotz.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AkkoCore.Extensions;

public static class PermissionsExt
{
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