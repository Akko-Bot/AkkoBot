﻿using AkkoBot.Extensions;
using System;
using Xunit;

namespace AkkoTests.Core.Extensions.StringExt
{
    public class StringExtTest
    {
        [Theory]
        [InlineData("hello", "hello", StringComparison.Ordinal, true)]
        [InlineData("hello", "hel", StringComparison.Ordinal, true)]
        [InlineData("", "", StringComparison.Ordinal, true)]
        [InlineData("hello", "HEllo", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("hello", "HEl", StringComparison.OrdinalIgnoreCase, true)]
        [InlineData("hello", "hEl", StringComparison.Ordinal, true)]
        [InlineData("hello", "h", StringComparison.Ordinal, true)]
        [InlineData("hello", null, StringComparison.Ordinal, false)]
        [InlineData("hello", "HEllo", StringComparison.Ordinal, false)]
        public void EqualsOrStartsWithTest(string caller, string calle, StringComparison comparisonType, bool result)
            => Assert.Equal(result, caller.EqualsOrStartsWith(calle, comparisonType));

        [Theory]
        [InlineData("test.yaml", "test")]
        [InlineData("test", "test")]
        [InlineData("test.a", "test")]
        [InlineData("test.not.yaml", "test.not")]
        [InlineData("", "")]
        public void RemoveExtensionTest(string caller, string result)
            => Assert.Equal(result, caller.RemoveExtension());

        [Theory]
        [InlineData(5, "hello", "hello")]
        [InlineData(5, "avocado", "avoca")]
        [InlineData(0, "banana", "")]
        [InlineData(5, null, null)]
        public void MaxLengthTest(int length, string caller, string result)
            => Assert.Equal(result, caller.MaxLength(length));

        [Theory]
        [InlineData(11, "banana cheesecake", "banana[...]", "[...]")]
        [InlineData(11, "banana chee", "banana chee", "[...]")]
        [InlineData(11, "banana chees", "banana[...]", "[...]")]
        [InlineData(0, "banana", "", "[...]")]
        [InlineData(5, "", "", "[...]")]
        [InlineData(5, "a", "a", "[...]")]
        [InlineData(5, "a", "a", null)]
        public void MaxLengthWithAppendTest(int length, string caller, string result, string append)
            => Assert.Equal(result, caller.MaxLength(length, append));

        [Theory]
        [InlineData(5, "", "     ")]
        [InlineData(5, "hello", " hello")]
        [InlineData(6, "hello", " hello")]
        [InlineData(7, "hello", " hello ")]
        [InlineData(10, "hello", " hello    ")]
        public void HardPadTest(int targetLength, string caller, string result)
            => Assert.Equal(result, caller.HardPad(targetLength));

        [Theory]
        [InlineData("hello", "Hello")]
        [InlineData(" hello", " hello")]
        [InlineData("hello there", "Hello there")]
        [InlineData("", "")]
        public void CapitalizeTest(string caller, string result)
            => Assert.Equal(result, caller.Capitalize());

        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("hello there", "hello-there")]
        [InlineData("HeLlo THere", "hello-there")]
        [InlineData("Long Channel Name", "long-channel-name")]
        public void ToTextChannelNameTest(string caller, string result)
            => Assert.Equal(result, caller.ToTextChannelName());

        [Theory]
        [InlineData("hello", "hello")]
        [InlineData("hello there", "hello_there")]
        [InlineData("heLlo tHere", "he_llo_t_here")]
        [InlineData("Long Channel Name", "long_channel_name")]
        [InlineData("Double  space!", "double_space!")]
        [InlineData("ALL CAPS", "all_caps")]
        [InlineData("has_Underscore", "has_underscore")]
        //[InlineData("has_ Underscore", "has_underscore")] // No need to support this
        public void ToSnakeCaseTest(string caller, string result)
            => Assert.Equal(result, caller.ToSnakeCase());

        [Theory]
        [InlineData("hello", 'l', 2)]
        [InlineData("hello", 'a', 0)]
        [InlineData("hello there", 'e', 3)]
        [InlineData("this has three spaces", ' ', 3)]
        public void OccurrencesTest(string caller, char target, int result)
            => Assert.Equal(result, caller.Occurrences(target));

        [Theory]
        [InlineData(7, "hello", "banana", "avocado")]
        [InlineData(5, "hello", "", "test")]
        [InlineData(0, "", "", "")]
        public void MaxElementLengthTest(int result, params string[] collection)
            => Assert.Equal(result, collection.MaxElementLength());

        [Theory]
        [InlineData("emoji_name", "emoji_name")]
        [InlineData(":emoji_name:", "emoji_name")]
        [InlineData(":emoji!_name:", "emoji_name")]
        [InlineData("!?:emoji_name:", "emoji_name")]
        public void SanitizeEmojiNameTest(string caller, string result)
            => Assert.Equal(result, caller.SanitizeEmojiName());

        [Theory]
        [InlineData("test", "test")]
        [InlineData("", "")]
        [InlineData(".test", "test")]
        [InlineData("_test", "test")]
        [InlineData("t!est", "t!est")]
        [InlineData("?!test", "test")]
        [InlineData("@test", "test")]
        [InlineData("test!", "test!")]
        public void SanitizeUsernameTest(string caller, string result)
            => Assert.Equal(result, caller.SanitizeUsername());

        [Theory]
        [InlineData("12345", "12345")]
        [InlineData("", "")]
        [InlineData("123abc45", "12345")]
        [InlineData("!1_2a3&4%5", "12345")]
        [InlineData("Nothing", "")]
        [InlineData("111e111", "111111")]
        public void GetDigitsTest(string caller, string result)
            => Assert.Equal(result, caller.GetDigits());
    }
}