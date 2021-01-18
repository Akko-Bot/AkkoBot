using System.Collections.Generic;
using System.Globalization;

namespace AkkoBot.Services.Localization.Abstractions
{
    public interface ILocalizer
    {
        /// <summary>
        /// Gets all cached locales.
        /// </summary>
        /// <returns>A collection of the registered locale keys.</returns>
        IEnumerable<string> GetAllLocales();

        /// <summary>
        /// Checks if the bot contains a valid response for a given response key.
        /// </summary>
        /// <param name="locale">Locale of the response string.</param>
        /// <param name="response">Response string key to be checked.</param>
        /// <returns><see langword="true"/> if the response is registered, otherwise <see langword="false"/>.</returns>
        bool ContainsResponse(string locale, string response);

        /// <summary>
        /// Gets a collection of the specified response strings.
        /// </summary>
        /// <param name="locale">The desired locale.</param>
        /// <param name="responses">Keys of the response strings to be fetched.</param>
        /// <returns>The localized response strings, if they exist.</returns>
        string[] GetResponseStrings(CultureInfo locale, params string[] responses);

        /// <summary>
        /// Gets a collection of the specified response strings.
        /// </summary>
        /// <param name="locale">The desired locale.</param>
        /// <param name="responses">Keys of the response strings to be fetched.</param>
        /// <returns>The localized response strings, if they exist.</returns>
        string[] GetResponseStrings(string locale, params string[] responses);

        /// <summary>
        /// Gets the specified response string.
        /// </summary>
        /// <param name="locale">The desired locale.</param>
        /// <param name="response">Key of the response string to be fetched.</param>
        /// <returns>The localized response string, if it exists.</returns>
        string GetResponseString(CultureInfo locale, string response);

        /// <summary>
        /// Gets the specified response string.
        /// </summary>
        /// <param name="locale">The desired locale.</param>
        /// <param name="response">Key of the response string to be fetched.</param>
        /// <returns>
        /// The localized response string, if it exists.
        /// An empty string, if <paramref name="response"/> is <see langword="null"/>.
        /// </returns>
        string GetResponseString(string locale, string response);

        /// <summary>
        /// Clears the cache and loads all response strings again.
        /// </summary>
        void ReloadLocalizedStrings();
    }
}