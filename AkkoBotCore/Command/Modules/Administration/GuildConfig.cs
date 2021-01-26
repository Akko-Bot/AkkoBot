using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using AkkoBot.Command.Modules.Administration.Services;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace AkkoBot.Command.Modules.Administration
{
    [Group("server"), Aliases("guild")]
    [Description("cmd_guild")]
    public class GuildConfig : AkkoCommandModule
    {
        private readonly GuildConfigService _service;

        public GuildConfig(GuildConfigService service)
        {
            _service = service;
        }

        [Command("locale"), Aliases("language")]
        [Description("cmd_guild_locale")]
        public async Task ListLocales(CommandContext context)
        {
            var locales = _service.GetLocales()
                .Select(x => $"{Formatter.InlineCode(x)} - {new CultureInfo(x).NativeName}")
                .OrderBy(x => x)
                .ToArray();

            var embed = new DiscordEmbedBuilder()
                .WithTitle("locales_title")
                .WithDescription(string.Join("\n", locales));

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("locale"), Aliases("language")]
        [Description("cmd_guild_locale")]
        public async Task ChangeGuildLocale(CommandContext context, [Description("arg_locale")] string locale)
        {
            // If locale does not exist, send error message
            if (!_service.IsLocaleRegistered(locale))
            {
                var errorEmbed = new DiscordEmbedBuilder()
                    .WithDescription(
                        context.FormatLocalized(
                            "guild_locale_unavailable",
                            Formatter.InlineCode(context.GetGuildPrefix() + context.Command.QualifiedName)
                        )
                    );

                await context.RespondLocalizedAsync(errorEmbed, isError: true);
                return;
            }

            // Change the locale
            _service.SetProperty(context, x => x.Locale = locale);

            // Send the message
            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_locale_changed", Formatter.InlineCode(locale)));

            await context.RespondLocalizedAsync(embed);
        }
    }
}