using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AkkoBot.Command.Abstractions;
using YamlDotNet.Serialization;

namespace AkkoBot.Services.Localization
{
    public class LocalizationCacher : ICommandService
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

        /// <summary>
        /// Gets the specified response string.
        /// </summary>
        /// <param name="locale">Locale of the <paramref name="response"/>.</param>
        /// <param name="response">Key of the response string to be fetched.</param>
        /// <returns>The localized response string, if it exists.</returns>
        public string GetResponseString(CultureInfo locale, string response)
            => GetResponseString(locale.Name, response);

        /// <summary>
        /// Loads all response strings into the cache.
        /// </summary>
        public void LoadLocalizedStrings()
        {
            // If Localization directory doesn't exist or is empty, create one with en-US strings
            if (!Directory.Exists(AkkoEnvironment.LocalesDirectory) || !Directory.GetFiles(AkkoEnvironment.LocalesDirectory).Any())
                CreateDefaultLocaleFile(AkkoEnvironment.LocalesDirectory);

            var fileNames = Directory
                .GetFiles(AkkoEnvironment.LocalesDirectory)
                .Where(x => x.Contains(".yaml") && x.Contains('_'));

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
        /// Gets the specified response string.
        /// </summary>
        /// <param name="locale">Locale of the <paramref name="response"/>.</param>
        /// <param name="response">Key of the response string to be fetched.</param>
        /// <returns>The localized response string, if it exists.</returns>
        public string GetResponseString(string locale, string response)
        {
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
            => filePath.Substring(filePath.LastIndexOf('_'), filePath.LastIndexOf('.'));

        /// <summary>
        /// Creates the directory where the response strings will be stored and a default 
        /// response string file in en-US.
        /// </summary>
        /// <param name="filePath">Path to the directory where the response strings should be stored.</param>
        private void CreateDefaultLocaleFile(string filePath)
        {
            Directory.CreateDirectory(AkkoEnvironment.LocalesDirectory);

            using var writer = File.CreateText(filePath + "ResponseStrings_en-US.yaml");
            new Serializer().Serialize(writer, new LocalizedStrings());
        }
    }
}