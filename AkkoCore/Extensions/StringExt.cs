using System;
using System.Collections.Generic;
using System.Text;

namespace AkkoCore.Extensions
{
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
            => target is not null && (msg.Equals(target, comparisonType) || msg.StartsWith(target[..1], comparisonType));

        /// <summary>
        /// Removes the file extension of this string, if there is one.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <returns></returns>
        public static string RemoveExtension(this string text)
            => text.Contains('.') ? text[..text.LastIndexOf('.')] : text;

        /// <summary>
        /// Truncates the string to the maximum specified length.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <param name="maxLength">The maximum length the string should have.</param>
        /// <returns>This string with length equal to or lower than <paramref name="maxLength"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Occurs when <paramref name="maxLength"/> is less than zero.</exception>
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
        /// <exception cref="ArgumentOutOfRangeException">Occurs when <paramref name="maxLength"/> is less than zero.</exception>
        public static string MaxLength(this string text, int maxLength, string append)
            => (text.Length <= maxLength)
                ? text
                : (text.MaxLength(Math.Max(0, maxLength - append.Length)) + append)[..maxLength];

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
                ? text?.PadRight(totalLength)
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
        /// Converts a string to the snake_case format.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <returns>This <see cref="string"/> converted to snake_case.</returns>
        public static string ToSnakeCase(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var buffer = new StringBuilder();

            for (var index = 0; index < text.Length; index++)
            {
                if (index != text.Length - 1 && char.IsUpper(text[index]) && !char.IsUpper(text[index + 1]) && !char.IsWhiteSpace(text[index + 1]))
                    buffer.Append('_');

                buffer.Append(char.ToLowerInvariant(text[index]));
            }
            
            if (buffer[0] == '_')
                buffer.Remove(0, 1);

            buffer.Replace(" _", " ")
                .Replace("_ ", "_")
                .Replace("__", "_");

            return buffer.ToString();
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
        /// <param name="comparisonType">The comparison rules.</param>
        /// <param name="match">The resulting match in the collection or <see langword="null"/> if none was found.</param>
        /// <returns><see langword="true"/> if there was one matching entry, <see langword="false"/> otherwise.</returns>
        public static bool Equals(this IEnumerable<string> collection, string target, StringComparison comparisonType, out string match)
        {
            foreach (var word in collection)
            {
                if (word.Equals(target, comparisonType))
                {
                    match = word;
                    return true;
                }
            }

            match = null;
            return false;
        }

        /// <summary>
        /// Checks if this string occurs within at least one of the entries in the specified collection.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <param name="collection">The collection to compare to.</param>
        /// <param name="comparisonType">The type of string comparison to be used.</param>
        /// <returns><see langword="true"/> if a match occurred, <see langword="false"/> otherwise.</returns>
        public static bool Contains(this string text, IEnumerable<string> collection, StringComparison comparisonType = StringComparison.Ordinal)
        {
            foreach (var word in collection)
            {
                if (text.Contains(word, comparisonType))
                    return true;
            }

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

            return result.ToString();
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

        /// <summary>
        /// Returns a string with all digits present in this string.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <returns>A string with all digits of this string, <see cref="string.Empty"/> if none is found.</returns>
        public static string GetDigits(this string text)
        {
            var result = new StringBuilder();

            foreach (var character in text)
            {
                if (char.IsDigit(character))
                    result.Append(character);
            }

            return result.ToString();
        }

        /// <summary>
        /// Returns the "Nth" index of the specified character.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <param name="character">The character to get the index from.</param>
        /// <param name="occurrence">Defines how many occurrences should be skipped, starting from 0 (first match).</param>
        /// <returns>The index of the specified character or -1 if it was not found.</returns>
        /// <example>This returns 2: <code>"hello".MatchedIndexOf('l', 0)</code></example>
        /// <example>This returns 3: <code>"hello".MatchedIndexOf('l', 1)</code></example>
        /// <example>This returns -1: <code>"hello".MatchedIndexOf('l', 2)</code></example>
        /// <seealso cref="LastOccurrenceOf(string, char, int)"/>
        public static int FirstOccurrenceOf(this string text, char character, int occurrence)
        {
            if (occurrence < 0)
                occurrence = 0;

            int counter = -1, result = -1;

            for (var index = 0; index < text.Length - 1; index++)
            {
                if (text[index].Equals(character) && ++counter == occurrence)
                {
                    result = index;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns the "Nth" last index of the specified character.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <param name="character">The character to get the index from.</param>
        /// <param name="occurrence">Defines how many occurrences should be skipped, starting from 0 (first match).</param>
        /// <returns>The last index of the specified character or -1 if it was not found.</returns>
        /// <example>This returns 3: <code>"hello".LastMatchedIndexOf('l', 0)</code></example>
        /// <example>This returns 2: <code>"hello".LastMatchedIndexOf('l', 1)</code></example>
        /// <example>This returns -1: <code>"hello".LastMatchedIndexOf('l', 2)</code></example>
        /// <seealso cref="FirstOccurrenceOf(string, char, int)"/>
        public static int LastOccurrenceOf(this string text, char character, int occurrence)
        {
            if (occurrence < 0)
                occurrence = 0;

            int counter = -1, result = -1;

            for (var index = text.Length - 1; index >= 0; index--)
            {
                if (text[index].Equals(character) && ++counter == occurrence)
                {
                    result = index;
                    break;
                }
            }

            return result;
        }
    }
}