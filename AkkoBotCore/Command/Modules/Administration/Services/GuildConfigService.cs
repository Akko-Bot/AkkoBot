using System;
using System.Collections.Generic;
using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.CommandsNext;

namespace AkkoBot.Command.Modules.Administration.Services
{
    public class GuildConfigService : ICommandService
    {
        private readonly ILocalizer _localizer;

        public GuildConfigService(ILocalizer localizer)
        {
            _localizer = localizer;
        }

        /// <summary>
        /// Checks if the specified locale is available and returns it if so.
        /// </summary>
        /// <param name="locale">The locale to check for.</param>
        /// <param name="match">The locale if found, <see langword="null"/> otherwise.</param>
        /// <returns><see langword="true"/> if a match is found, <see langword="false"/> otherwise.</returns>
        public bool IsLocaleRegistered(string locale, out string match)
            => _localizer.GetLocales().Contains(locale, StringComparison.InvariantCultureIgnoreCase, out match);

        /// <summary>
        /// Gets all registered localed.
        /// </summary>
        /// <returns>A collection of registered locales.</returns>
        public IEnumerable<string> GetLocales()
            => _localizer.GetLocales();

        /// <summary>
        /// Gets the specified guild setting.
        /// </summary>
        /// <typeparam name="T">The type of the setting to be returned.</typeparam>
        /// <param name="context">The command context.</param>
        /// <param name="selector">The action to get the property.</param>
        /// <returns>The requested setting.</returns>
        public T GetOrSetProperty<T>(CommandContext context, Func<GuildConfigEntity, T> selector)
        {
            using var scope = context.Services.GetScopedService<IUnitOfWork>(out var db);
            var guild = db.GuildConfigs.GetGuild(context.Guild.Id);
            var result = selector(guild);

            db.GuildConfigs.Update(guild);
            db.SaveChanges();

            return result;
        }

        public IReadOnlyDictionary<string, string> GetGuildSettings(CommandContext context)
        {
            using var scope = context.Services.GetScopedService<IUnitOfWork>(out var db);
            return db.GuildConfigs.GetGuild(context.Guild.Id).GetSettings();
        }
    }
}