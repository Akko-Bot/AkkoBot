using AkkoBot.Commands.Abstractions;
using AkkoBot.Common;
using AkkoBot.Config;
using AkkoBot.Core.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly IServiceProvider _services;
        private readonly IDbCache _dbCache;

        public BotConfigService(IServiceProvider services, IDbCache dbCache)
        {
            _services = services;
            _dbCache = dbCache;
        }

        /// <summary>
        /// Gets the collection of currently available locales.
        /// </summary>
        /// <returns>A collection of strings of the registered locales.</returns>
        public IEnumerable<string> GetLocales()
            => _services.GetService<ILocalizer>().Locales;

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
            var localizer = _services.GetService<ILocalizer>();
            localizer.ReloadLocalizedStrings();

            return localizer.Locales.Count;
        }

        /// <summary>
        /// Gets or sets the specified bot configuration.
        /// </summary>
        /// <param name="selector">A method to get or set the property.</param>
        /// <returns>The targeted property.</returns>
        public T GetOrSetProperty<T>(Func<BotConfig, T> selector)
        {
            var configLoader = _services.GetService<ConfigLoader>();

            // Change the cached settings
            var result = selector(_dbCache.BotConfig);
            configLoader.SaveConfig(_dbCache.BotConfig, AkkoEnvironment.BotConfigPath);

            return result;
        }

        /// <summary>
        /// Gets or sets the specified bot configuration.
        /// </summary>
        /// <param name="selector">A method to get or set the property.</param>
        /// <returns>The targeted property.</returns>
        public T GetOrSetProperty<T>(Func<LogConfig, T> selector)
        {
            var configLoader = _services.GetService<ConfigLoader>();

            // Change the cached settings
            var result = selector(_dbCache.LogConfig);
            configLoader.SaveConfig(_dbCache.LogConfig, AkkoEnvironment.LogConfigPath);

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