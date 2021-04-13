using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Models;
using AkkoBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration
{
    [Group("serverconfig"), Aliases("guildconfig", "server", "guild")]
    [Description("cmd_guild")]
    [RequireGuild, RequireUserPermissions(Permissions.ManageGuild)]
    public class GuildConfig : AkkoCommandModule
    {
        private readonly GuildConfigService _service;

        public GuildConfig(GuildConfigService service)
            => _service = service;

        [Command("embed")]
        [Description("cmd_guild_embed")]
        public async Task ChangeEmbed(CommandContext context)
        {
            var result = await _service.GetOrSetPropertyAsync(context.Guild, x => x.UseEmbed = !x.UseEmbed);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_embed_change", (result) ? "enabled" : "disabled"));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("okcolor")]
        [Description("cmd_guild_okcolor")]
        public async Task ChangeOkColor(CommandContext context, [Description("arg_color")] string newColor)
        {
            var color = GeneralService.GetColor(newColor);

            var embed = new DiscordEmbedBuilder()
            {
                Description = (color.HasValue)
                    ? context.FormatLocalized("guild_okcolor", Formatter.InlineCode(newColor.ToUpperInvariant()))
                    : "invalid_color"
            };

            if (color.HasValue)
                await _service.GetOrSetPropertyAsync(context.Guild, x => x.OkColor = newColor);

            await context.RespondLocalizedAsync(embed);
        }

        [Command("errorcolor")]
        [Description("cmd_guild_errorcolor")]
        public async Task ChangeErrorColor(CommandContext context, [Description("arg_color")] string newColor)
        {
            var color = GeneralService.GetColor(newColor);

            var embed = new DiscordEmbedBuilder()
            {
                Description = (color.HasValue)
                    ? context.FormatLocalized("guild_errorcolor", Formatter.InlineCode(newColor.ToUpperInvariant()))
                    : "invalid_color"
            };

            if (color.HasValue)
                await _service.GetOrSetPropertyAsync(context.Guild, x => x.ErrorColor = newColor);

            await context.RespondLocalizedAsync(embed);
        }

        [Command("timeout")]
        [Description("cmd_guild_timeout")]
        public async Task ChangeTimeout(CommandContext context, [Description("arg_timeout")] uint? seconds = null)
        {
            var result = await _service.GetOrSetPropertyAsync(
                context.Guild,
                settings => settings.InteractiveTimeout = (seconds is null or < 10 or > 120)
                    ? null
                    : TimeSpan.FromSeconds(seconds.Value)
                );

            var embed = new DiscordEmbedBuilder()
            {
                Description = (result is null)
                    ? context.FormatLocalized("guild_timeout_reset")
                    : context.FormatLocalized("guild_timeout_changed", Formatter.InlineCode(result.Value.TotalSeconds.ToString()))
            };

            await context.RespondLocalizedAsync(embed);
        }

        [Command("permissivemention"), Aliases("rolemention")]
        [Description("cmd_guild_rolemention")]
        public async Task ChangeRoleMentionability(CommandContext context)
        {
            var result = await _service.GetOrSetPropertyAsync(context.Guild, x => x.PermissiveRoleMention = !x.PermissiveRoleMention);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_role_mention", (result) ? "enabled" : "disabled"));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("locale"), Aliases("language")]
        [Description("cmd_guild_locale")]
        public async Task ListLocales(CommandContext context)
        {
            var locales = _service.GetLocales()
                .OrderBy(code => code);

            var embed = new DiscordEmbedBuilder()
                .WithTitle("locales_title")
                .AddField("code", string.Join('\n', locales.ToArray()), true)
                .AddField("language", string.Join('\n', locales.Select(x => GeneralService.GetCultureInfo(x)?.NativeName ?? x).ToArray()), true);

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
            await _service.GetOrSetPropertyAsync(context.Guild, x => x.Locale = responseKey);

            // Send the message
            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_locale_changed", Formatter.InlineCode(responseKey)));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("timezone")]
        [Description("cmd_guild_timezone")]
        public async Task Timezone(CommandContext context, [RemainingText, Description("arg_timezone")] string timezone)
        {
            var zone = GeneralService.GetTimeZone(timezone);
            var embed = new DiscordEmbedBuilder()
            {
                Description = (zone is null)
                    ? context.FormatLocalized("guild_timezone_error", Formatter.InlineCode(context.Prefix + "timezones"))
                    : context.FormatLocalized("guild_timezone_changed", Formatter.InlineCode($"{zone.StandardName} ({zone.BaseUtcOffset.Hours:00}:{zone.BaseUtcOffset.Minutes:00})"))
            };

            if (zone is not null)
                await _service.GetOrSetPropertyAsync(context.Guild, x => x.Timezone = zone.StandardName);

            await context.RespondLocalizedAsync(embed, isError: zone is null);
        }

        [GroupCommand, Command("list")]
        [Description("cmd_guild_list")]
        public async Task ListGuildConfigs(CommandContext context)
        {
            var settings = _service.GetGuildSettings(context.Guild);

            var embed = new DiscordEmbedBuilder()
                .WithTitle("guild_settings_title")
                .AddField("settings", string.Join("\n", settings.Keys), true)
                .AddField("value", string.Join("\n", settings.Values), true);

            await context.RespondLocalizedAsync(embed);
        }
    }
}