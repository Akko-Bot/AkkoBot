using System;
using System.Collections.Generic;
using AkkoBot.Command.Abstractions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Extensions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;

namespace AkkoBot.Command.Modules.Self.Services
{
    public class BotConfigService : ICommandService
    {
        /// <summary>
        /// Gets the collection of currently available locales.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <returns>A collection of strings of the registered locales.</returns>
        public IEnumerable<string> GetLocales(CommandContext context)
            => context.Services.GetService<ILocalizer>().GetLocales();

        /// <summary>
        /// Changes the bot configuration according to the actions defined in <paramref name="selector"/>.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="selector">A method that assigns values to the properties of a <see cref="BotConfigEntity"/> object.</param>
        public void SetProperty(CommandContext context, Action<BotConfigEntity> selector)
        {
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);

            // Change the cached settings
            selector(db.BotConfig.Cache);

            // Set the database entry to the modified cached settings
            db.BotConfig.Update(db.BotConfig.Cache);
            db.SaveChanges();
        }

        /// <summary>
        /// Changes the bot configuration according to the actions defined in <paramref name="selector"/>.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="selector">A method that assigns values to the properties of a <see cref="LogConfigEntity"/> object.</param>
        public void SetProperty(CommandContext context, Action<LogConfigEntity> selector)
        {
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);

            // Change the cached settings
            selector(db.LogConfig.Cache);

            // Set the database entry to the modified cached settings
            db.LogConfig.Update(db.LogConfig.Cache);
            db.SaveChanges();
        }

        /// <summary>
        /// Gets a collection of all bot settings.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <returns>A collection of setting name/value pairs.</returns>
        public IDictionary<string, string> GetConfigs(CommandContext context)
        {
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);

            var botConfig = db.BotConfig.Cache;
            var logConfig = db.LogConfig.Cache;

            var settings = botConfig.GetSettings();

            foreach (var propPair in logConfig.GetSettings())
                settings.TryAdd(propPair.Key, propPair.Value);

            return settings;
        }
    }
}