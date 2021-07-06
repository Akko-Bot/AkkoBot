using System;

namespace AkkoBot.Extensions
{
    public static class DateTimeOffsetExt
    {
        /// <summary>
        /// Gets the beginning of the day of this date.
        /// </summary>
        /// <param name="date">This date time.</param>
        /// <param name="offset">UTC offset.</param>
        /// <returns>This <see cref="DateTimeOffset"/> with its <see cref="DateTimeOffset.TimeOfDay"/> set to zero.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Occurs when <paramref name="offset"/> is less than -14 hours or greater than 14 hours.</exception>
        public static DateTimeOffset StartOfDay(this DateTimeOffset date, TimeSpan offset = default)
            => new(date.Year, date.Month, date.Day, 0, 0, 0, 0, TimeSpan.FromMinutes((long)offset.TotalMinutes));

        /// <summary>
        /// Converts this date time to a Discord markdown timestamp.
        /// </summary>
        /// <param name="date">This date time.</param>
        /// <param name="format">The timestamp format.</param>
        /// <remarks>
        /// Values for <paramref name="format"/>:
        /// <br>f: Short date and time - "July 3, 2021 1:13 AM"</br>
        /// <br>F: Long date and time - "Saturday, July 3, 2021 1:13 AM"</br>
        /// <br>d: Short date - "07/03/2021"</br>
        /// <br>D: Long date - "July 3, 2021"</br>
        /// <br>t: Short time - "1:13 AM"</br>
        /// <br>T: Long time - "1:13:15 AM"</br>
        /// <br>R: Relative time - "3 days ago"</br>
        /// </remarks>
        /// <returns>A Discord markdown timestamp.</returns>
        public static string ToDiscordTimestamp(this DateTimeOffset date, char format = 'f')
            => $"<t:{date.ToUnixTimeSeconds()}:{format}>";
    }
}