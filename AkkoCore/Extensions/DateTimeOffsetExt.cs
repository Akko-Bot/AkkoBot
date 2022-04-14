using DSharpPlus;
using System;

namespace AkkoCore.Extensions;

public static class DateTimeOffsetExt
{
    /// <summary>
    /// Converts this date time to a Discord markdown timestamp.
    /// </summary>
    /// <param name="date">This date time.</param>
    /// <param name="format">The timestamp format.</param>
    /// <returns>A Discord markdown timestamp.</returns>
    public static string ToDiscordTimestamp(this DateTimeOffset date, TimestampFormat format = TimestampFormat.ShortDateTime)
        => $"<t:{date.ToUnixTimeSeconds()}:{(char)format}>";
}