using AkkoBot.Commands.Abstractions;
using AkkoBot.Common;
using AkkoBot.Credential;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Localization.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace AkkoBot.Commands.Modules.Self.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="BotConfigEntity"/> objects.
    /// </summary>
    public class BotConfigService : ICommandService
    {
        private readonly IServiceProvider _services;

        public BotConfigService(IServiceProvider services)
            => _services = services;

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
            => _services.GetService<IDbCacher>().BotConfig;

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
        public T GetOrSetProperty<T>(Func<BotConfigEntity, T> selector)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            // Change the cached settings
            var result = selector(db.BotConfig.Cache);

            // Set the database entry to the modified cached settings
            db.BotConfig.Update(db.BotConfig.Cache);
            db.SaveChanges();

            return result;
        }

        /// <summary>
        /// Gets or sets the specified bot configuration.
        /// </summary>
        /// <param name="selector">A method to get or set the property.</param>
        public T GetOrSetProperty<T>(Func<LogConfigEntity, T> selector)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            // Change the cached settings
            var result = selector(db.LogConfig.Cache);

            // Set the database entry to the modified cached settings
            db.LogConfig.Update(db.LogConfig.Cache);
            db.SaveChanges();

            return result;
        }

        /// <summary>
        /// Gets a collection of all bot settings.
        /// </summary>
        /// <returns>A collection of setting name/value pairs.</returns>
        public IReadOnlyDictionary<string, string> GetConfigs()
        {
            var dbCache = _services.GetService<IDbCacher>();

            var botConfig = dbCache.BotConfig;
            var logConfig = dbCache.LogConfig;

            var settings = new Dictionary<string, string>(botConfig.GetSettings());

            foreach (var propPair in logConfig.GetSettings())
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