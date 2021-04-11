using AkkoBot.Commands.Abstractions;
using AkkoBot.Common;
using AkkoBot.Credential;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Localization.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            => _services.GetService<ILocalizer>().GetLocales();

        /// <summary>
        /// Gets the bot's global settings.
        /// </summary>
        /// <returns>The bot settings.</returns>
        public BotConfigEntity GetConfig()
            => _dbCache.BotConfig;

        /// <summary>
        /// Reloads the response strings from the original files.
        /// </summary>
        /// <returns>The amount of locales stored in memory.</returns>
        public int ReloadLocales()
        {
            var localizer = _services.GetService<ILocalizer>();
            localizer.ReloadLocalizedStrings();

            return localizer.GetLocales().Count();
        }

        /// <summary>
        /// Gets or sets the specified bot configuration.
        /// </summary>
        /// <param name="selector">A method to get or set the property.</param>
        public async Task<T> GetOrSetPropertyAsync<T>(Func<BotConfigEntity, T> selector)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            // Change the cached settings
            var result = selector(_dbCache.BotConfig);

            // Set the database entry to the modified cached settings
            db.BotConfig.Update(_dbCache.BotConfig);
            await db.SaveChangesAsync();

            return result;
        }

        /// <summary>
        /// Gets or sets the specified bot configuration.
        /// </summary>
        /// <param name="selector">A method to get or set the property.</param>
        public async Task<T> GetOrSetPropertyAsync<T>(Func<LogConfigEntity, T> selector)
        {
            using var scope = _services.GetScopedService<AkkoDbContext>(out var db);

            // Change the cached settings
            var result = selector(_dbCache.LogConfig);

            // Set the database entry to the modified cached settings
            db.Update(_dbCache.LogConfig);
            await db.SaveChangesAsync();

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