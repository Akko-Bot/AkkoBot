using AkkoBot.Common;
using AkkoBot.Services.Localization.Abstractions;
using AkkoCore.Common;
using AkkoCore.Extensions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AkkoBot.Services.Localization
{
    /// <summary>
    /// Manages the cache for the response strings loaded into memory.
    /// </summary>
    public class AkkoLocalizer : ILocalizer
    {
        /// <summary>
        /// The cache of response strings. First key is the locale. Second key is the response
        /// string's key. The value is the response string itself.
        /// </summary>
        private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _localizedStrings = new();

        /// <summary>
        /// Regex to get the locale of the response files. Matches anything enclosed between "_" and "."
        /// </summary>
        private static readonly Regex _localeRegex = new(@"_(.*?)\.", RegexOptions.Compiled);

        public IReadOnlyCollection<string> Locales
            => _localizedStrings.Keys;

        public AkkoLocalizer()
            => LoadLocalizedStrings();

        public string GetResponseString(CultureInfo locale, string response)
            => GetResponseString(locale.Name, response);

        public string[] GetResponseStrings(CultureInfo locale, params string[] responses)
            => GetResponseStrings(locale.Name, responses);

        public bool ContainsLocale(string locale)
            => _localizedStrings.ContainsKey(locale);

        public bool ContainsResponse(string locale, string response)
        {
            return !string.IsNullOrWhiteSpace(response)
                && _localizedStrings.ContainsKey(locale)
                && (_localizedStrings[locale].ContainsKey(response) || _localizedStrings[AkkoConstants.DefaultLanguage].ContainsKey(response));
        }

        public void ReloadLocalizedStrings()
        {
            _localizedStrings.Clear();
            LoadLocalizedStrings();
        }

        public string[] GetResponseStrings(string locale, params string[] responses)
        {
            var result = new string[responses.Length];
            var index = 0;

            foreach (var response in responses)
                result[index++] = GetResponseString(locale, response);

            return result;
        }

        public string GetResponseString(string locale, string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return string.Empty;

            if (!_localizedStrings.ContainsKey(locale))
                locale = AkkoConstants.DefaultLanguage;

            if (_localizedStrings[locale].ContainsKey(response) || _localizedStrings[AkkoConstants.DefaultLanguage].ContainsKey(response))
            {
                return (_localizedStrings[locale].TryGetValue(response, out _))
                    ? _localizedStrings[locale][response]
                    : _localizedStrings[AkkoConstants.DefaultLanguage][response];
            }

            return response;
        }

        /// <summary>
        /// Gets the locale of the response string file, assuming it follows the "*_{locale}.yaml" format.
        /// </summary>
        /// <param name="filePath">Path to the file with the response strings.</param>
        /// <returns>The locale of the response string's file, <see langword="null"/> if no match occured.</returns>
        private string GetFileLocale(string filePath)
        {
            var match = _localeRegex.Match(filePath).Groups.Values.LastOrDefault();
            return match?.Value;
        }

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
            foreach (var file in fileNames)
            {
                var reader = new StreamReader(File.OpenRead(file));
                var lStrings = reader.FromYaml<Dictionary<string, string>>();

                _localizedStrings.TryAdd(GetFileLocale(file), lStrings);
                reader.Dispose();
            }
        }

        public string FormatLocalized(string locale, string key, params object[] args)
        {
            for (var index = 0; index < args.Length; index++)
            {
                if (args[index] is string arg)
                    args[index] = GetResponseString(locale, arg);
                else if (args[index] is null)
                    args[index] = string.Empty;
            }

            key = GetResponseString(locale, key);

            return string.Format(key, args);
        }

        public void Dispose()
        {
            foreach (var stringGroup in _localizedStrings.Values.Cast<Dictionary<string, string>>())
            {
                stringGroup.Clear();
                stringGroup.TrimExcess();
            }

            _localizedStrings.Clear();
            _localizedStrings.TrimExcess();

            GC.SuppressFinalize(this);
        }
    }
}