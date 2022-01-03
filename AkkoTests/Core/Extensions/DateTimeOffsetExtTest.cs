using AkkoCore.Extensions;
using AkkoTests.TestData;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AkkoTests.Core.Extensions;

public sealed class DateTimeOffsetExtTest
{
    /// <summary>
    /// Returns all <see cref="TimestampFormat"/> values.
    /// </summary>
    public static IEnumerable<object[]> TimestampFormats { get; } = Enum.GetValues<TimestampFormat>().Select(x => new object[] { DateTimeOffset.Now, x });

    [Theory] // Offset is in minutes
    [ClassData(typeof(OffsetCorrectionTestData))]
    internal void StartOfDayTest(int goodOffset, double badOffset)
    {
        var today = DateTimeOffset.UtcNow;

        // Offsets
        var inputOffset = TimeSpan.FromMinutes(badOffset);
        var correctedOffset = TimeSpan.FromMinutes(goodOffset);

        // Test input
        Assert.Equal(new(today.Year, today.Month, today.Day, 0, 0, 0, 0, correctedOffset), today.StartOfDay(inputOffset));
    }

    [Theory] // Offset is in minutes
    [ClassData(typeof(OffsetCorrectionTestData))]
    internal void OffsetCorrectionTest(int expected, double actual)
        => Assert.Equal(TimeSpan.FromMinutes(expected), DateTimeOffset.UtcNow.StartOfDay(TimeSpan.FromMinutes(actual)).Offset);

    [Theory]
    [MemberData(nameof(TimestampFormats))]
    internal void DiscordTimestampTest(DateTimeOffset now, TimestampFormat format)
        => Assert.True(now.ToDiscordTimestamp(format).Equals($"<t:{now.ToUnixTimeSeconds()}:{(char)format}>", StringComparison.Ordinal));
}