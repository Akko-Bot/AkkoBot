using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AkkoBot.Services.Localization.Abstractions;
using YamlDotNet.Serialization;

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
        private readonly Dictionary<string, Dictionary<string, string>> _localizedStrings = new();

        public AkkoLocalizer()
            => LoadLocalizedStrings();

        /// <summary>
        /// Checks if the bot contains a valid default response for a given response key.
        /// </summary>
        /// <param name="response">Response string key to be checked.</param>
        /// <returns><see langword="true"/> if the response is registered, otherwise <see langword="false"/>.</returns>
        public bool ContainsResponse(string response)
            => _localizedStrings[DefaultLanguage].ContainsKey(response);

        /// <summary>
        /// Gets all cached locales.
        /// </summary>
        /// <returns>A collection of the registered locale keys.</returns>
        public IEnumerable<string> GetAllLocales()
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
        /// Loads all response strings into the cache.
        /// </summary>
        public void LoadLocalizedStrings()
        {
            // If Localization directory doesn't exist create one with en-US strings
            if (!Directory.Exists(AkkoEnvironment.LocalesDirectory))
                CreateDefaultLocaleFile(AkkoEnvironment.LocalesDirectory);

            var fileNames = Directory
                .GetFiles(AkkoEnvironment.LocalesDirectory)
                .Where(x => x.Contains(".yaml") && x.Contains('_'));

            // If directory doesn't contain response strings files create the default one
            if (!fileNames.Any())
                fileNames = fileNames.Append(CreateDefaultLocaleFile(AkkoEnvironment.LocalesDirectory));

            foreach (var file in fileNames)
            {
                using var reader = new StreamReader(File.OpenRead(file));
                var lStrings = new Deserializer().Deserialize<LocalizedStrings>(reader);
                _localizedStrings.TryAdd(GetFileLocale(file), lStrings.GetStringCollection());
            }
        }

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

            if (_localizedStrings[locale].ContainsKey(response))
            {
                return string.IsNullOrEmpty(_localizedStrings[locale][response])
                    ? _localizedStrings[DefaultLanguage][response]
                    : _localizedStrings[locale][response];
            }
            else
            {
                return string.IsNullOrEmpty(_localizedStrings[locale]["error_not_found"])
                    ? _localizedStrings[DefaultLanguage]["error_not_found"]
                    : _localizedStrings[locale]["error_not_found"];
            }
        }

        /// <summary>
        /// Gets the locale of the response string file, assuming it follows the "*_{locale}.yaml" format.
        /// </summary>
        /// <param name="filePath">Path to the file with the response strings.</param>
        /// <returns>The locale of the response string's file.</returns>
        private string GetFileLocale(string filePath)
            => filePath[(filePath.LastIndexOf('_') + 1).. filePath.LastIndexOf('.')];

        /// <summary>
        /// Creates the directory where the response strings will be stored and a default 
        /// response string file in en-US.
        /// </summary>
        /// <param name="filePath">Path to the directory where the response strings should be stored.</param>
        /// <returns>The path to the default response strings file.</returns>
        private string CreateDefaultLocaleFile(string filePath)
        {
            Directory.CreateDirectory(AkkoEnvironment.LocalesDirectory);
            var fileName = filePath + $"ResponseStrings_{DefaultLanguage}.yaml";

            using var writer = File.CreateText(fileName);
            new Serializer().Serialize(writer, new LocalizedStrings());

            return fileName;
        }
    }
}