using System;
using System.Collections.Generic;
using System.Linq;
using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace AkkoBot.Command.Modules.Administration.Services
{
    /// <summary>
    /// Groups utility methods for retrieving and manipulating <see cref="GuildConfigEntity"/> objects.
    /// </summary>
    public class GuildConfigService : ICommandService
    {
        private readonly IServiceProvider _services;
        private readonly ILocalizer _localizer;

        public GuildConfigService(IServiceProvider services, ILocalizer localizer)
        {
            _services = services;
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
        /// Gets or sets the specified guild setting.
        /// </summary>
        /// <typeparam name="T">The type of the setting to be returned.</typeparam>
        /// <param name="context">The command context.</param>
        /// <param name="selector">A method to get or set the property.</param>
        /// <returns>The requested setting, <see langword="null"/> if the context is from a private context.</returns>
        public T GetOrSetProperty<T>(CommandContext context, Func<GuildConfigEntity, T> selector)
        {
            using var scope = context.Services.GetScopedService<IUnitOfWork>(out var db);
            var guild = db.GuildConfig.GetGuild(context.Guild?.Id ?? 0);
            var result = selector(guild);

            if (guild is not null)
            {
                db.GuildConfig.Update(guild);
                db.SaveChanges();
            }

            return result;
        }

        /// <summary>
        /// Returns the settings of the specified Discord guild.
        /// </summary>
        /// <param name="server">The Discord guild.</param>
        /// <returns>A collection of settings.</returns>
        public IReadOnlyDictionary<string, string> GetGuildSettings(DiscordGuild server)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            return db.GuildConfig.GetGuild(server.Id).GetSettings();
        }
    }
}