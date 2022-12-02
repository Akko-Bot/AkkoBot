using System;
using System.Text;
using Kotz.Extensions;

namespace AkkoCore.Extensions;

public static class StringExt
{
    /// <summary>
    /// Checks if this string is equal or starts with the first character specified in <paramref name="target"/>.
    /// </summary>
    /// <param name="msg">This string.</param>
    /// <param name="target">The string to compare with.</param>
    /// <param name="comparisonType">The type of string comparison to be used.</param>
    /// <returns><see langword="true"/> if it matches, <see langword="false"/> otherwise.</returns>
    public static bool EqualsOrStartsWith(this string msg, string target, StringComparison comparisonType = StringComparison.Ordinal)
        => target is not null && (msg.AsSpan().Equals(target, comparisonType) || msg.AsSpan().StartsWith(target.AsSpan()[..1], comparisonType));

    /// <summary>
    /// Removes the file extension of this string, if there is one.
    /// </summary>
    /// <param name="text">This string.</param>
    /// <returns>This file name, without the extension.</returns>
    public static string RemoveExtension(this string text)
        => text.Contains('.') ? text[..text.LastIndexOf('.')] : text;

    /// <summary>
    /// Returns a new string that has a space character inserted at its begining and that
    /// left-aligns the characters in this string by padding them with spaces on the right,
    /// for a specified total length.
    /// </summary>
    /// <param name="text">This string.</param>
    /// <param name="totalLength">The length of the resulting string.</param>
    /// <returns>This string padded to the right.</returns>
    public static string HardPad(this string text, int totalLength)
        => string.IsNullOrWhiteSpace(text)
            ? text?.PadRight(totalLength) ?? string.Empty
            : text.Insert(0, " ").PadRight(totalLength);

    /// <summary>
    /// Returns a string whose first character is uppercase and all others are lowercase.
    /// </summary>
    /// <param name="text">This string.</param>
    /// <returns>This string capitalized.</returns>
    public static string Capitalize(this string text)
        => string.IsNullOrWhiteSpace(text) ? string.Empty : char.ToUpperInvariant(text[0]) + text[1..].ToLowerInvariant();

    /// <summary>
    /// Converts a string to the format used by Discord's text channel names.
    /// </summary>
    /// <param name="text">This string.</param>
    /// <returns>This <see cref="string"/> converted to a text-channel-name.</returns>
    public static string ToTextChannelName(this string text)
        => text.ToLowerInvariant().Replace(' ', '-');

    /// <summary>
    /// Ensures this string is a valid <see cref="DiscordEmoji"/> name.
    /// </summary>
    /// <param name="text">This string.</param>
    /// <returns>This string sanitized to an emoji name.</returns>
    public static string SanitizeEmojiName(this string text)
    {
        if (text.Length < 2)
            return "emoji";

        var result = new StringBuilder();

        foreach (var character in text)
        {
            if (character is '_' || !char.IsPunctuation(character))
                result.Append(character);
        }

        // Emoji names have a max length of 50 characters
        if (result.Length > 50)
            result.Remove(0, 50);

        return result.ToStringAndClear();
    }

    /// <summary>
    /// Removes punctuation and symbol characters from the beginning of this username.
    /// </summary>
    /// <param name="text">This string.</param>
    /// <returns>A sanitized username.</returns>
    public static string SanitizeUsername(this string text)
    {
        var index = 0;

        while (index != text.Length && (char.IsPunctuation(text[index]) || char.IsSymbol(text[index])))
            index++;

        return text[index..];
    }
}