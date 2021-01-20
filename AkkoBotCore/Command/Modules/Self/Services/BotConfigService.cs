using System;
using System.Collections.Generic;
using AkkoBot.Command.Abstractions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;

namespace AkkoBot.Command.Modules.Self.Services
{
    public class BotConfigService : ICommandService
    {
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

        public IReadOnlyDictionary<string, string> GetConfigs(CommandContext context)
        {
            using var scope = context.CommandsNext.Services.CreateScope();
            var botConfig = scope.ServiceProvider.GetService<IUnitOfWork>().BotConfig.Cache;

            return botConfig.GetSettings();
        }
    }
}