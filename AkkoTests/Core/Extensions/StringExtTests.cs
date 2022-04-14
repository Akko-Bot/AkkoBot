using AkkoCore.Extensions;
using Xunit;

namespace AkkoTests.Core.Extensions;

public sealed class StringExtTests
{
    [Theory]
    [InlineData("test.yaml", "test")]
    [InlineData("test", "test")]
    [InlineData("test.a", "test")]
    [InlineData("test.not.yaml", "test.not")]
    [InlineData("", "")]
    internal void RemoveExtensionTest(string caller, string result)
        => Assert.Equal(result, caller.RemoveExtension());

    [Theory]
    [InlineData(5, "", "     ")]
    [InlineData(5, "hello", " hello")]
    [InlineData(6, "hello", " hello")]
    [InlineData(7, "hello", " hello ")]
    [InlineData(10, "hello", " hello    ")]
    internal void HardPadTest(int targetLength, string caller, string result)
        => Assert.Equal(result, caller.HardPad(targetLength));

    [Theory]
    [InlineData("hello", "Hello")]
    [InlineData(" hello", " hello")]
    [InlineData("hello there", "Hello there")]
    [InlineData("a", "A")]
    [InlineData("", "")]
    internal void CapitalizeTest(string caller, string result)
        => Assert.Equal(result, caller.Capitalize());

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("hello there", "hello-there")]
    [InlineData("HeLlo THere", "hello-there")]
    [InlineData("Long Channel Name", "long-channel-name")]
    internal void ToTextChannelNameTest(string caller, string result)
        => Assert.Equal(result, caller.ToTextChannelName());

    [Theory]
    [InlineData("emoji_name", "emoji_name")]
    [InlineData(":emoji_name:", "emoji_name")]
    [InlineData(":emoji!_name:", "emoji_name")]
    [InlineData("!?:emoji_name:", "emoji_name")]
    internal void SanitizeEmojiNameTest(string caller, string result)
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
    internal void SanitizeUsernameTest(string caller, string result)
        => Assert.Equal(result, caller.SanitizeUsername());
}