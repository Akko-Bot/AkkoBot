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

        public bool IsLocaleRegistered(string locale)
            => _localizer.ContainsLocale(locale);

        public IEnumerable<string> GetLocales()
            => _localizer.GetLocales();

        public void SetProperty(CommandContext context, Action<GuildConfigEntity> selector)
        {
            using var scope = context.Services.GetScopedService<IUnitOfWork>(out var db);
            var guild = db.GuildConfigs.GetGuild(context.Guild.Id);

            selector(guild);

            db.GuildConfigs.Update(guild);
            db.SaveChanges();
        }
    }
}