using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace AkkoBot.Extensions
{
    public static class StringExt
    {
        /// <summary>
        /// Checks if this string is equal or starts with the first character specified in <paramref name="target"/>.
        /// </summary>
        /// <param name="msg">This string.</param>
        /// <param name="target">The string to compare with.</param>
        /// <returns><see langword="true"/> if it matches, <see langword="false"/> otherwise.</returns>
        public static bool EqualsOrStartsWith(this string msg, string target)
            => msg.Equals(target) || msg.StartsWith(target[..1]);

        /// <summary>
        /// Removes the file extension of this string, if there is one.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <returns></returns>
        public static string RemoveExtension(this string text)
            => (text.Contains('.')) ? text[..text.LastIndexOf('.')] : text;

        /// <summary>
        /// Truncates the string to the maximum specified length.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <param name="maxLength">The maximum length the string should have.</param>
        /// <returns>This string with length equal to or lower than <paramref name="maxLength"/>.</returns>
        public static string MaxLength(this string text, int maxLength)
            => text?.Substring(0, Math.Min(text.Length, maxLength));

        /// <summary>
        /// Truncates the string to the maximum specified length.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <param name="maxLength">The maximum length the string should have.</param>
        /// <param name="append">The string to be appended to the end of the truncated string.</param>
        /// <remarks>The <paramref name="append"/> only gets added to the truncated string if this string exceeds <paramref name="maxLength"/> in length.</remarks>
        /// <returns>This string with length equal to or lower than <paramref name="maxLength"/>.</returns>
        public static string MaxLength(this string text, int maxLength, string append)
            => (text.Length <= maxLength)
                ? text
                : text.MaxLength(maxLength - append.Length) + append;

        /// <summary>
        /// Returns a new string that has a space character inserted at its begining and that
        /// left-aligns the characters in this string by padding them with spaces on the right,
        /// for a specified total length.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <param name="totalLength">The length of the resulting string.</param>
        /// <returns>This string padded to the right.</returns>
        public static string HardPad(this string text, int totalLength)
            => (string.IsNullOrWhiteSpace(text))
                ? text?.PadRight(totalLength)
                : text.Insert(0, " ").PadRight(totalLength);

        /// <summary>
        /// Returns a string whose first character is uppercase and all others are lowercase.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <returns>This string capitalized.</returns>
        public static string Capitalize(this string text)
            => char.ToUpperInvariant(text[0]) + text[1..].ToLowerInvariant();

        /// <summary>
        /// Converts a string to the format used by Discord's text channel names.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <returns>This <see cref="string"/> converted to a text-channel-name.</returns>
        public static string ToTextChannelName(this string text)
            => text.ToLowerInvariant().Replace(' ', '-');

        /// <summary>
        /// Converts a string to the snake_case format.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <returns>This <see cref="string"/> converted to snake_case.</returns>
        public static string ToSnakeCase(this string text)
        {
            var buffer = new StringBuilder(text);

            for (int index = 1; index < buffer.Length; index++)
            {
                if (char.IsUpper(buffer[index]) && !char.IsUpper(buffer[index - 1]))
                    buffer.Insert(index++, '_');
            }

            if (buffer[0] == '_')
                buffer.Remove(0, 1);

            return buffer.ToString().ToLowerInvariant();
        }

        /// <summary>
        /// Gets the amount of occurences of a given character in this string.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <param name="target">The character to check for.</param>
        /// <returns>The amount of occurences of <paramref name="target"/> in this string.</returns>
        public static int Occurrences(this string text, char target)
        {
            var counter = 0;

            foreach (var letter in text)
            {
                if (letter == target)
                    counter++;
            }

            return counter;
        }

        /// <summary>
        /// Get the length of the longest string of this collection.
        /// </summary>
        /// <param name="collection">This collection of strings.</param>
        /// <returns>The length of the longest element.</returns>
        public static int MaxElementLength(this IEnumerable<string> collection)
        {
            var max = 0;

            foreach (var element in collection)
                max = Math.Max(max, element.Length);

            return max;
        }

        /// <summary>
        /// Checks whether this string is equal to any of the strings provided in <paramref name="samples"/>.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <param name="comparisonType">The type of string comparison to be used.</param>
        /// <param name="samples">The strings to compare to.</param>
        /// <returns><see langword="true"/> if this string equals one of the strings in <paramref name="samples"/>, <see langword="false"/> otherwise.</returns>
        public static bool Equals(this string text, StringComparison comparisonType, params string[] samples)
        {
            foreach (var sample in samples)
            {
                if (sample.Equals(text, comparisonType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if at least one entry in this collection matches the specified string and returns the match, if it exists.
        /// </summary>
        /// <param name="collection">This string collection.</param>
        /// <param name="target">The string to be compared with.</param>
        /// <param name="comparison">The comparison rules.</param>
        /// <param name="match">The resulting match in the collection or <see langword="null"/> if none was found.</param>
        /// <returns><see langword="true"/> if there was one matching entry, <see langword="false"/> otherwise.</returns>
        public static bool Contains(this IEnumerable<string> collection, string target, StringComparison comparison, out string match)
        {
            foreach (var word in collection)
            {
                if (word.Equals(target, comparison))
                {
                    match = word;
                    return true;
                }
            }

            match = null;
            return false;
        }

        /// <summary>
        /// Checks whether the end of this string matches any string stored in <paramref name="collection"/> when compared using the specified comparison option.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <param name="collection">The collection to compare to.</param>
        /// <param name="comparisonType">The type of string comparison to be used.</param>
        /// <returns><see langword="true"/> if a match occurred, <see langword="false"/> otherwise.</returns>
        public static bool EndsWith(this string text, IEnumerable<string> collection, StringComparison comparisonType = StringComparison.Ordinal)
        {
            foreach (var element in collection)
            {
                if (text.EndsWith(element, comparisonType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks whether the beginning of this string matches any string stored in <paramref name="collection"/> when compared using the specified comparison option.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <param name="collection">The collection to compare to.</param>
        /// <param name="comparisonType">The type of string comparison to be used.</param>
        /// <returns><see langword="true"/> if a match occurred, <see langword="false"/> otherwise.</returns>
        public static bool StartsWith(this string text, IEnumerable<string> collection, StringComparison comparisonType = StringComparison.Ordinal)
        {
            foreach (var element in collection)
            {
                if (text.StartsWith(element, comparisonType))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Ensures this string is a valid <see cref="DiscordEmoji"/> name.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <returns>This string sanitized to an emoji name.</returns>
        public static string SanitizeEmojiName(this string text)
        {
            var result = new StringBuilder(text.Trim(':'));

            foreach (var character in result.ToString())
            {
                if (char.IsPunctuation(character))
                    result.Replace(character.ToString(), string.Empty);
            }

            if (result.Length < 2)
            {
                result.Clear();
                result.Append("emoji");
            }

            return result.ToString().MaxLength(50);
        }
    }
}