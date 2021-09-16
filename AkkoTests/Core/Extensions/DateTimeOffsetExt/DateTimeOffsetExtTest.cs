using AkkoCore.Extensions;
using DSharpPlus;
using System;
using Xunit;

namespace AkkoTests.Core.Extensions.DateTimeOffsetExt
{
    public class DateTimeOffsetExtTest
    {
        private readonly DateTimeOffset _now = DateTimeOffset.Now;

        [Theory] // Offset is in minutes
        [InlineData(0, 0)]
        [InlineData(0, 0.9)]
        [InlineData(180, 180)]
        [InlineData(180, 180.1)]
        [InlineData(23, 23.3333)]
        [InlineData(59, 59.9)]
        [InlineData(123, 123.456789)]
        [InlineData(600, 600.000001)]
        [InlineData(1, 1.000000001)]
        internal void StartOfDayTest(int goodOffset, double badOffset)
        {
            var today = DateTimeOffset.Now;

            // Offsets
            var inputOffset = TimeSpan.FromMinutes(badOffset);
            var correctedOffset = TimeSpan.FromMinutes(goodOffset);

            // Test input
            Assert.Equal(new(today.Year, today.Month, today.Day, 0, 0, 0, 0, correctedOffset), today.StartOfDay(inputOffset));
        }

        [Theory] // Offset is in minutes
        [InlineData(0, 0)]
        [InlineData(0, 0.9)]
        [InlineData(180, 180)]
        [InlineData(180, 180.1)]
        [InlineData(23, 23.3333)]
        [InlineData(59, 59.9)]
        [InlineData(123, 123.456789)]
        [InlineData(600, 600.000001)]
        [InlineData(1, 1.000000001)]
        internal void OffsetCorrectionTest(int expected, double actual)
            => Assert.Equal(TimeSpan.FromMinutes(expected), DateTimeOffset.Now.StartOfDay(TimeSpan.FromMinutes(actual)).Offset);

        [Theory]
        [InlineData(TimestampFormat.LongDate)]
        [InlineData(TimestampFormat.LongDateTime)]
        [InlineData(TimestampFormat.LongTime)]
        [InlineData(TimestampFormat.RelativeTime)]
        [InlineData(TimestampFormat.ShortDate)]
        [InlineData(TimestampFormat.ShortDateTime)]
        [InlineData(TimestampFormat.ShortTime)]
        internal void DiscordTimestampTest(TimestampFormat format)
            => Assert.True(_now.ToDiscordTimestamp(format).Equals($"<t:{_now.ToUnixTimeSeconds()}:{(char)format}>", StringComparison.Ordinal));
    }
}