using AkkoBot.Commands.Abstractions;
using AkkoBot.Common;
using AkkoBot.Config;
using AkkoBot.Core.Common.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;

namespace AkkoBot.Commands.Modules.Self.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="BotConfigEntity"/> objects.
    /// </summary>
    public class BotConfigService : ICommandService
    {
        private readonly IDbCache _dbCache;
        private readonly ILocalizer _localizer;
        private readonly IConfigLoader _configLoader;

        public BotConfigService(IDbCache dbCache, ILocalizer localizer, IConfigLoader configLoader)
        {
            _dbCache = dbCache;
            _localizer = localizer;
            _configLoader = configLoader;
        }

        /// <summary>
        /// Gets the collection of currently available locales.
        /// </summary>
        /// <returns>A collection of strings of the registered locales.</returns>
        public IEnumerable<string> GetLocales()
            => _localizer.Locales;

        /// <summary>
        /// Gets the bot's global settings.
        /// </summary>
        /// <returns>The bot settings.</returns>
        public BotConfig GetConfig()
            => _dbCache.BotConfig;

        /// <summary>
        /// Gets the bot's log settings.
        /// </summary>
        /// <returns>The log settings.</returns>
        public LogConfig GetLogConfig()
            => _dbCache.LogConfig;

        /// <summary>
        /// Reloads the response strings from the original files.
        /// </summary>
        /// <returns>The amount of locales stored in memory.</returns>
        public int ReloadLocales()
        {
            _localizer.ReloadLocalizedStrings();
            return _localizer.Locales.Count;
        }

        /// <summary>
        /// Gets or sets the specified bot configuration.
        /// </summary>
        /// <param name="selector">A method to get or set the property.</param>
        /// <returns>The targeted property.</returns>
        public T GetOrSetProperty<T>(Func<BotConfig, T> selector)
        {
            var result = selector(_dbCache.BotConfig);
            _configLoader.SaveConfig(_dbCache.BotConfig, AkkoEnvironment.BotConfigPath);

            return result;
        }

        /// <summary>
        /// Gets or sets the specified bot configuration.
        /// </summary>
        /// <param name="selector">A method to get or set the property.</param>
        /// <returns>The targeted property.</returns>
        public T GetOrSetProperty<T>(Func<LogConfig, T> selector)
        {
            var result = selector(_dbCache.LogConfig);
            _configLoader.SaveConfig(_dbCache.LogConfig, AkkoEnvironment.LogConfigPath);

            return result;
        }

        /// <summary>
        /// Gets a collection of all bot settings.
        /// </summary>
        /// <returns>A collection of setting name/value pairs.</returns>
        public IReadOnlyDictionary<string, string> GetConfigs()
        {
            var settings = new Dictionary<string, string>(_dbCache.BotConfig.GetSettings());

            foreach (var propPair in _dbCache.LogConfig.GetSettings())
                settings.TryAdd(propPair.Key, propPair.Value);

            return settings;
        }

        /// <summary>
        /// Serializes a credentials file.
        /// </summary>
        /// <param name="creds">The data to be serialized.</param>
        /// <returns>The text stream.</returns>
        public TextWriter SerializeCredentials(Credentials creds)
            => creds.ToYaml(File.CreateText(AkkoEnvironment.CredsPath), new Serializer());
    }
}