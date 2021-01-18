using System;
using System.Text;

namespace AkkoBot.Extensions
{
    public static class StringExt
    {
        /// <summary>
        /// Truncates the string to the maximum specified length.
        /// </summary>
        /// <param name="text">This string.</param>
        /// <param name="maxLength">The maximum length the string should have.</param>
        /// <returns>This string with length equal to or lower than <paramref name="maxLength"/>.</returns>
        public static string MaxLength(this string text, int maxLength)
            => text.Substring(0, Math.Min(text.Length, maxLength));

        /// <summary>
        /// Converts a string to the snake_case format.
        /// </summary>
        /// <param name="text">This <see cref="string"/> to be converted.</param>
        /// <returns>This <see cref="string"/> converted to snake_case.</returns>
        public static string ToSnakeCase(this string text)
        {
            var buffer = new StringBuilder(text);

            for (int index = 0; index < buffer.Length; index++)
            {
                if (char.IsUpper(buffer[index]))
                    buffer.Insert(index++, '_');
            }

            if (buffer[0] == '_')
                buffer.Remove(0, 1);

            return buffer.ToString().ToLowerInvariant();
        }
    }
}