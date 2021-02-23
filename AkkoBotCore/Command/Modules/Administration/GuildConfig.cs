using System;
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
    [Group("serverconfig"), Aliases("guildconfig", "servercfg", "guildcfg")]
    [Description("cmd_guild")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public class GuildConfig : AkkoCommandModule
    {
        private readonly GuildConfigService _service;

        public GuildConfig(GuildConfigService service)
            => _service = service;

        [Command("embed")]
        [Description("cmd_guild_embed")]
        public async Task ChangeEmbed(CommandContext context)
        {
            var result = _service.GetOrSetProperty(context, x => x.UseEmbed = !x.UseEmbed);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_embed_change", (result) ? "enabled" : "disabled"));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("okcolor")]
        [Description("cmd_guild_okcolor")]
        public async Task ChangeOkColor(CommandContext context, [Description("arg_color")] string newColor)
        {
            var result = _service.GetOrSetProperty(context, x => x.OkColor = newColor);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_okcolor", Formatter.InlineCode(result)));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("errorcolor")]
        [Description("cmd_guild_errorcolor")]
        public async Task ChangeErrorColor(CommandContext context, [Description("arg_color")] string newColor)
        {
            var result = _service.GetOrSetProperty(context, x => x.ErrorColor = newColor);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_errorcolor", Formatter.InlineCode(result)));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("timeout")]
        [Description("cmd_guild_timeout")]
        public async Task ChangeTimeout(CommandContext context, [Description("arg_timeout")] uint? seconds = null)
        {
            var result = _service.GetOrSetProperty(
                context,
                settings => settings.InteractiveTimeout = (seconds is null or < 10 or > 120)
                    ? null
                    : new TimeSpan(0, 0, (int)seconds)
                );

            var embed = new DiscordEmbedBuilder()
            {
                Description = (result is null)
                    ? context.FormatLocalized("guild_timeout_reset")
                    : context.FormatLocalized("guild_timeout_changed", Formatter.InlineCode(result.Value.TotalSeconds.ToString()))
            };

            await context.RespondLocalizedAsync(embed);
        }

        [Command("locale"), Aliases("language")]
        [Description("cmd_guild_locale")]
        public async Task ListLocales(CommandContext context)
        {
            var locales = _service.GetLocales()
                .Select(code => (code, new CultureInfo(code).NativeName))
                .OrderBy(code => code);

            var embed = new DiscordEmbedBuilder()
                .WithTitle("locales_title")
                .AddField("code", string.Join('\n', locales.Select(x => x.code).ToArray()), true)
                .AddField("language", string.Join('\n', locales.Select(x => x.NativeName).ToArray()), true);

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("locale"), Aliases("languages")]
        public async Task ChangeGuildLocale(CommandContext context, [Description("arg_locale")] string languageCode)
        {
            // If locale does not exist, send error message
            if (!_service.IsLocaleRegistered(languageCode, out var responseKey))
            {
                var errorEmbed = new DiscordEmbedBuilder()
                    .WithDescription(
                        context.FormatLocalized(
                            "guild_locale_unavailable",
                            Formatter.InlineCode(context.Prefix + context.Command.QualifiedName)
                        )
                    );

                await context.RespondLocalizedAsync(errorEmbed, isError: true);
                return;
            }

            // Change the locale
            _service.GetOrSetProperty(context, x => x.Locale = responseKey);

            // Send the message
            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_locale_changed", Formatter.InlineCode(responseKey)));

            await context.RespondLocalizedAsync(embed);
        }

        [GroupCommand, Command("list")]
        [Description("cmd_guild_list")]
        public async Task ListGuildConfigs(CommandContext context)
        {
            var settings = _service.GetGuildSettings(context.Guild);

            var embed = new DiscordEmbedBuilder()
                .WithTitle("guild_settings_title")
                .AddField("settings", string.Join("\n", settings.Keys.ToArray()), true)
                .AddField("value", string.Join("\n", settings.Values.ToArray()), true);

            await context.RespondLocalizedAsync(embed);
        }
    }
}