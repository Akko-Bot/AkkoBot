using AkkoCore.Extensions;
using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace AkkoTests.Core.Extensions;

public sealed class DateTimeOffsetExtTests
{
    /// <summary>
    /// Returns all <see cref="TimestampFormat"/> values.
    /// </summary>
    public static IEnumerable<object[]> TimestampFormats { get; } = Enum.GetValues<TimestampFormat>().Select(x => new object[] { DateTimeOffset.Now, x });

    [Theory]
    [MemberData(nameof(TimestampFormats))]
    internal void DiscordTimestampTest(DateTimeOffset now, TimestampFormat format)
        => Assert.True(now.ToDiscordTimestamp(format).Equals($"<t:{now.ToUnixTimeSeconds()}:{(char)format}>", StringComparison.Ordinal));
}