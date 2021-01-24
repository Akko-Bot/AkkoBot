using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AkkoBot.Extensions;
using AkkoBot.Services.Localization.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AkkoBot.Services.Localization
{
    /// <summary>
    /// Manages the cache for the response strings loaded into memory.
    /// </summary>
    public class AkkoLocalizer : ILocalizer
    {
        /// <summary>
        /// The language the response strings default to when the requested locale does not exist.
        /// </summary>
        public const string DefaultLanguage = "en-US";

        /// <summary>
        /// The cache of response strings. First key is the locale. Second key is the response 
        /// string's key. The value is the response string itself.
        /// </summary>
        private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _localizedStrings = new();

        public AkkoLocalizer()
            => LoadLocalizedStrings();

        /// <summary>
        /// Gets all cached locales.
        /// </summary>
        /// <returns>A collection of the registered locale keys.</returns>
        public IEnumerable<string> GetLocales()
            => _localizedStrings.Keys;

        /// <summary>
        /// Gets the specified response string.
        /// </summary>
        /// <param name="locale">The desired locale.</param>
        /// <param name="response">Key of the response string to be fetched.</param>
        /// <returns>The localized response string, if it exists.</returns>
        public string GetResponseString(CultureInfo locale, string response)
            => GetResponseString(locale.Name, response);

        /// <summary>
        /// Gets a collection of the specified response strings.
        /// </summary>
        /// <param name="locale">The desired locale.</param>
        /// <param name="responses">Keys of the response strings to be fetched.</param>
        /// <returns>The localized response strings, if they exist.</returns>
        public string[] GetResponseStrings(CultureInfo locale, params string[] responses)
            => GetResponseStrings(locale.Name, responses);

        /// <summary>
        /// Checks if the bot contains a valid response for a given response key.
        /// </summary>
        /// <param name="locale">Locale of the response string.</param>
        /// <param name="response">Response string key to be checked.</param>
        /// <returns><see langword="true"/> if the response is registered, otherwise <see langword="false"/>.</returns>
        public bool ContainsResponse(string locale, string response)
            => _localizedStrings.ContainsKey(locale)
                && (_localizedStrings[locale].ContainsKey(response)
                || _localizedStrings[DefaultLanguage].ContainsKey(response));

        /// <summary>
        /// Clears the cache and loads all response strings again.
        /// </summary>
        public void ReloadLocalizedStrings()
        {
            _localizedStrings.Clear();
            LoadLocalizedStrings();
        }

        /// <summary>
        /// Gets a collection of the specified response strings.
        /// </summary>
        /// <param name="locale">The desired locale.</param>
        /// <param name="responses">Keys of the response strings to be fetched.</param>
        /// <returns>The localized response strings, if they exist.</returns>
        public string[] GetResponseStrings(string locale, params string[] responses)
        {
            var result = new string[responses.Length];
            var index = 0;

            foreach (var response in responses)
                result[index++] = GetResponseString(locale, response);

            return result;
        }

        /// <summary>
        /// Gets the specified response string.
        /// </summary>
        /// <param name="locale">The desired locale.</param>
        /// <param name="response">Key of the response string to be fetched.</param>
        /// <returns>
        /// The localized response string, if it exists.
        /// An empty string, if <paramref name="response"/> is <see langword="null"/>.
        /// </returns>
        public string GetResponseString(string locale, string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return string.Empty;

            if (!_localizedStrings.ContainsKey(locale))
                locale = DefaultLanguage;

            if (_localizedStrings[locale].ContainsKey(response) || _localizedStrings[DefaultLanguage].ContainsKey(response))
            {
                return (_localizedStrings[locale].TryGetValue(response, out _))
                    ? _localizedStrings[locale][response]
                    : _localizedStrings[DefaultLanguage][response];
            }
            else
            {
                return (_localizedStrings[locale].TryGetValue("error_not_found", out _))
                    ? _localizedStrings[locale]["error_not_found"]
                    : _localizedStrings[DefaultLanguage]["error_not_found"];
            }
        }

        /// <summary>
        /// Gets the locale of the response string file, assuming it follows the "*_{locale}.yaml" format.
        /// </summary>
        /// <param name="filePath">Path to the file with the response strings.</param>
        /// <returns>The locale of the response string's file.</returns>
        private string GetFileLocale(string filePath)
            => filePath[(filePath.LastIndexOf('_') + 1)..filePath.LastIndexOf('.')];

        /// <summary>
        /// Loads all response strings into the cache.
        /// </summary>
        /// <exception cref="DirectoryNotFoundException"/>
        /// <exception cref="FileNotFoundException"/>
        /// <exception cref="IOException"/>
        private void LoadLocalizedStrings()
        {
            var fileNames = Directory
                .GetFiles(AkkoEnvironment.LocalesDirectory)
                .Where(x => x.Contains(".yaml") && x.Contains('_'));

            // If directory doesn't contain response strings, stop program execution
            if (!fileNames.Any())
                throw new FileNotFoundException("No localization file has been found.");

            // Start deserialization
            var yaml = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            foreach (var file in fileNames)
            {
                using var reader = new StreamReader(File.OpenRead(file));
                var lStrings = yaml.Deserialize<Dictionary<string, string>>(reader);

                _localizedStrings.TryAdd(GetFileLocale(file), lStrings);
            }
        }
    }
}