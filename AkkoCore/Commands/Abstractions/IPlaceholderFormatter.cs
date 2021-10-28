using DSharpPlus.CommandsNext;
using System.Text.RegularExpressions;

namespace AkkoCore.Commands.Abstractions
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
        /// <param name="match">The regex match for the placeholder.</param>
        /// <param name="result">The parsed placeholder, <see langword="null"/> if parsing fails.</param>
        /// <remarks><paramref name="result"/> can return <see langword="null"/> even if this method returns <see langword="true"/>.</remarks>
        /// <returns><see langword="true"/> if the placeholder was recognized, <see langword="false"/> otherwise.</returns>
        bool TryParse(CommandContext context, Match match, out object? result);
    }
}