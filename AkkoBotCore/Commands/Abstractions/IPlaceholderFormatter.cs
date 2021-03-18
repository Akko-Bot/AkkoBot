using DSharpPlus.CommandsNext;

namespace AkkoBot.Commands.Abstractions
{
    /// <summary>
    /// Represents an object that parses a string placeholder to the value it represents.
    /// </summary>
    public interface IPlaceholderFormatter
    {
        /// <summary>
        /// Parses a string placeholder to the value it represents.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="placeholder">The placeholder.</param>
        /// <param name="result">The string representation of the parsed placeholder, <see langword="null"/> if parsing fails.</param>
        /// <returns>The value of the boxed object the placeholder represents, <see langword="null"/> if parsing fails.</returns>
        public object Parse(CommandContext context, string placeholder, out string result);
    }
}
