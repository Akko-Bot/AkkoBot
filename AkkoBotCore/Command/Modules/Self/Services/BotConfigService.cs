using System;
using System.Collections.Generic;
using AkkoBot.Command.Abstractions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;

namespace AkkoBot.Command.Modules.Self.Services
{
    public class BotConfigService : ICommandService
    {
        public IEnumerable<string> GetLocales(CommandContext context)
            => context.Services.GetService<ILocalizer>().GetLocales();

        /// <summary>
        /// Changes the bot configuration according to the actions defined in <paramref name="selector"/>.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="selector">A method that assigns values to the properties of a <see cref="BotConfigEntity"/> object.</param>
        public void SetProperty(CommandContext context, Action<BotConfigEntity> selector)
        {
            using var scope = context.CommandsNext.Services.CreateScope();
            var db = scope.ServiceProvider.GetService<IUnitOfWork>();

            // Change the cached settings
            selector(db.BotConfig.Cache);

            // Set the database entry to the modified cached settings
            db.BotConfig.Update(db.BotConfig.Cache);
            db.SaveChanges();
        }

        public void SetProperty(CommandContext context, Action<LogConfigEntity> selector)
        {
            using var scope = context.CommandsNext.Services.CreateScope();
            var db = scope.ServiceProvider.GetService<IUnitOfWork>();

            // Change the cached settings
            selector(db.LogConfig.Cache);

            // Set the database entry to the modified cached settings
            db.LogConfig.Update(db.LogConfig.Cache);
            db.SaveChanges();
        }

        public IDictionary<string, string> GetConfigs(CommandContext context)
        {
            using var scope = context.CommandsNext.Services.CreateScope();
            var db = scope.ServiceProvider.GetService<IUnitOfWork>();

            var botConfig = db.BotConfig.Cache;
            var logConfig = db.LogConfig.Cache;

            var settings = botConfig.GetSettings();

            foreach (var propPair in logConfig.GetSettings())
                settings.TryAdd(propPair.Key, propPair.Value);

            return settings;
        }
    }
}