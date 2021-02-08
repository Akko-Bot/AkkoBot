using DSharpPlus.CommandsNext;
using System;

namespace AkkoBot.Extensions
{
    public static class DateTimeOffsetExt
    {
        /// <summary>
        /// Gets the beginning of the day of this date.
        /// </summary>
        /// <param name="date">This date.</param>
        /// <returns>This <see cref="DateTimeOffset"/> with its <see cref="DateTimeOffset.TimeOfDay"/> set to zero.</returns>
        public static DateTimeOffset StartOfDay(this DateTimeOffset date)
            => new(date.Year, date.Month, date.Day, 0, 0, 0, 0, TimeSpan.Zero);
    }
}