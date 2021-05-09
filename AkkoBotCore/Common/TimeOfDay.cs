using AkkoBot.Extensions;
using System;

namespace AkkoBot.Common
{
    /// <summary>
    /// Calculates the time interval from <see cref="DateTimeOffset.Now"/> to the next time of day specified.
    /// </summary>
    public class TimeOfDay
    {
        private readonly DateTimeOffset _dateTime;

        /// <summary>
        /// Defines the time of the day represented by this <see cref="TimeOfDay"/>.
        /// </summary>
        public TimeSpan Time => _dateTime.TimeOfDay;

        /// <summary>
        /// Gets this time's offset from Coordinated Universal Time (UTC).
        /// </summary>
        public TimeSpan Offset => _dateTime.Offset;

        /// <summary>
        /// Defines how long it will take for the next <see cref="Time"/> to be reached.
        /// </summary>
        public TimeSpan Interval => GetInterval(_dateTime);

        public TimeOfDay(TimeSpan timeOfDay, TimeZoneInfo timezone)
            => _dateTime = DateTimeOffset.Now.StartOfDay(timezone.BaseUtcOffset).Add(timeOfDay);

        public TimeOfDay(DateTimeOffset dateTime) 
            => _dateTime = dateTime;

        /// <summary>
        /// Gets the time it will take for the <see cref="DateTimeOffset.TimeOfDay"/> of <paramref name="future"/> to be reached.
        /// </summary>
        /// <param name="future">A point of time in the future.</param>
        /// <returns>The interval between <see cref="DateTimeOffset.Now"/> and the <paramref name="future"/>'s TimeOfDay.</returns>
        public static TimeSpan GetInterval(DateTimeOffset future)
        {
            var result = future.Subtract(DateTimeOffset.Now);

            if (result.Days < 0)
            {
                future = future.AddDays(-result.Days);
                result = future.Subtract(DateTimeOffset.Now);
            }

            return (result <= TimeSpan.Zero)
                ? result.Add(TimeSpan.FromDays(1))
                : result;
        }

        /// <inheritdoc />
        public override string ToString() 
            => Time.ToString();

        /// <summary>
        /// Converts the value of the current <see cref="TimeOfDay"/> object to its equivalent string representation by using the specified <paramref name="format"/>.
        /// </summary>
        /// <param name="format">A standard or custom <see cref="TimeSpan"/> format string.</param>
        /// <returns>The string representation of the current <see cref="TimeOfDay"/> value in the format specified by the <paramref name="format"/> parameter.</returns>
        /// <exception cref="FormatException">Occurs when the format provided is invalid.</exception>
        public string ToString(string format)
            => Time.ToString(format);

        /// <summary>
        /// Converts the value of the current <see cref="TimeOfDay"/> object to its equivalent string representation by using the specified format and culture-specific
        /// formatting information.
        /// </summary>
        /// <param name="format">A standard or custom <see cref="TimeSpan"/> format string.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the current <see cref="TimeOfDay"/> value, as specified by <paramref name="format"/> and <paramref name="formatProvider"/>.</returns>
        public string ToString(string format, IFormatProvider formatProvider)
            => Time.ToString(format, formatProvider);
    }
}
