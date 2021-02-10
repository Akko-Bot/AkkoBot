using System;
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
        /// Truncates the string to the maximum specified length.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <param name="maxLength">The maximum length the string should have.</param>
        /// <returns>This string with length equal to or lower than <paramref name="maxLength"/>.</returns>
        public static string MaxLength(this string text, int maxLength)
            => text.Substring(0, Math.Min(text.Length, maxLength));

        /// <summary>
        /// Returns a string whose first character is uppercase and all others are lowercase.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <returns>This string capitalized.</returns>
        public static string Capitalize(this string text)
            => char.ToUpperInvariant(text[0]) + text[1..].ToLowerInvariant();

        /// <summary>
        /// Converts a string to the snake_case format.
        /// </summary>
        /// <param name="text">This <see cref="string"/> to be converted.</param>
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
    }
}