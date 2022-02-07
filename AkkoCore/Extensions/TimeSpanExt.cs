using AkkoCore.Services.Localization.Abstractions;
using System;

namespace AkkoCore.Extensions;

public static class TimeSpanExt
{
    /// <summary>
    /// Returns the smallest time string for the specified time span.
    /// </summary>
    /// <param name="time">This time span.</param>
    /// <param name="localizer">The localizer.</param>
    /// <param name="locale">Locale to translate the time to.</param>
    /// <returns>The time string.</returns>
    public static string GetLocalizedTimeString(this TimeSpan time, ILocalizer localizer, string locale)
    {
        return (time.TotalDays >= 1.0)
            ? $"{time.TotalDays:0.00} {localizer.FormatLocalized(locale, "days")}"
            : (time.TotalHours >= 1.0)
                ? $"{time.TotalHours:0.00} {localizer.FormatLocalized(locale, "hours")}"
                : (time.TotalMinutes >= 1.0)
                    ? $"{time.TotalMinutes:0.00} {localizer.FormatLocalized(locale, "minutes")}"
                    : $"{time.TotalSeconds:0.00} {localizer.FormatLocalized(locale, "seconds")}";
    }
}