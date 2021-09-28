using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Services.Localization.Abstractions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AkkoCore.Services.Localization
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
        private readonly Dictionary<string, Dictionary<string, string>> _localizedStrings = new();

        /// <summary>
        /// Regex to get the locale of the response files. Locale must be between "_" and ".yaml"
        /// </summary>
        private static readonly Regex _localeRegex = new(@"_([\w-]+?(?=\.(?:yaml|yml)$))", RegexOptions.Compiled);

        public IReadOnlyCollection<string> Locales
            => _localizedStrings.Keys;

        public AkkoLocalizer()
            => LoadLocalizedStrings(AkkoEnvironment.LocalesDirectory);

        public IEnumerable<KeyValuePair<string, string>> GetResponsePairsByPartialKey(string locale, string keyPart)
            => _localizedStrings[locale].Where(x => x.Key.Contains(keyPart, StringComparison.Ordinal));

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
            foreach (var localizedGroup in _localizedStrings.Values)
            {
                localizedGroup.Clear();
                localizedGroup.TrimExcess();
            }

            _localizedStrings.Clear();
            _localizedStrings.TrimExcess();

            LoadLocalizedStrings(AkkoEnvironment.LocalesDirectory);
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

        public void LoadLocalizedStrings(string localesDirectory)
        {
            if (string.IsNullOrWhiteSpace(localesDirectory))
                return;

            var filePaths = Directory
                .GetFiles(localesDirectory)
                .Where(x => _localeRegex.IsMatch(x));

            // If directory doesn't contain response strings, stop program execution
            if (!filePaths.Any())
                throw new FileNotFoundException($"No localization file has been found at \"{localesDirectory}.\"");

            // Start deserialization
            foreach (var filePath in filePaths)
            {
                //var reader = new StreamReader(File.OpenRead(filePath));
                //var lStrings = reader.FromYaml<Dictionary<string, string>>();

                //_localizedStrings.TryAdd(GetFileLocale(filePath), lStrings);
                //reader.Dispose();

                var locale = GetFileLocale(filePath);
                var reader = new StreamReader(File.OpenRead(filePath));
                var lStrings = reader.FromYaml<Dictionary<string, string>>();

                if (!_localizedStrings.ContainsKey(locale))
                    _localizedStrings.Add(locale, lStrings);
                else
                {
                    foreach (var stringPair in lStrings)
                        _localizedStrings[locale].Add(stringPair.Key, stringPair.Value);
                }
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
            foreach (var stringGroup in _localizedStrings.Values)
            {
                stringGroup.Clear();
                stringGroup.TrimExcess();
            }

            _localizedStrings.Clear();
            _localizedStrings.TrimExcess();

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Gets the locale of the response string file, assuming it follows the "*_{locale}.yaml" format.
        /// </summary>
        /// <param name="filePath">Path to the file with the response strings.</param>
        /// <returns>The locale of the response string's file, <see langword="null"/> if no match occured.</returns>
        private string GetFileLocale(string filePath)
        {
            var match = _localeRegex.Match(filePath).Groups.Values.LastOrDefault()?.Value;

            if (match is not null && match.Contains('_'))
                match = match[(match.LastOccurrenceOf('_', 0) + 1)..];

            return match;
        }
    }
}