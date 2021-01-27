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
    [Group("server"), Aliases("guild")]
    [Description("cmd_guild")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public class GuildConfig : AkkoCommandModule
    {
        private readonly GuildConfigService _service;

        public GuildConfig(GuildConfigService service)
        {
            _service = service;
        }
        // TODO: make command group for listing the guild settings
        [Command("prefix")]
        [Description("cmd_guild_prefix")]
        public async Task CheckPrefix(CommandContext context)
        {
            var prefix = _service.GetProperty(context, x => x.Prefix);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_prefix_check", Formatter.InlineCode(prefix)));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("prefix")]
        public async Task ChangePrefix(CommandContext context, [Description("arg_prefix")] string prefix)
        {
            _service.GetProperty(context, x => x.Prefix = prefix);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_prefix_change", Formatter.InlineCode(prefix)));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("embed")]
        [Description("cmd_guild_embed")]
        public async Task ChangeEmbed(CommandContext context)
        {
            var result = _service.GetProperty(context, x => x.UseEmbed = !x.UseEmbed);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_embed_change", (result) ? "enabled" : "disabled"));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("okcolor")]
        [Description("cmd_guild_okcolor")]
        public async Task ChangeOkColor(CommandContext context, string newColor)
        {
            var result = _service.GetProperty(context, x => x.OkColor = newColor);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_okcolor", Formatter.InlineCode(result)));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("errorcolor")]
        [Description("cmd_guild_errorcolor")]
        public async Task ChangeErrorColor(CommandContext context, string newColor)
        {
            var result = _service.GetProperty(context, x => x.ErrorColor = newColor);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_errorcolor", Formatter.InlineCode(result)));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("timeout")]
        [Description("cmd_guild_timeout")]
        public async Task ChangeTimeout(CommandContext context, uint? seconds = null)
        {
            var result = _service.GetProperty(context, x => x.InteractiveTimeout = (seconds is null or 0) ? null : new TimeSpan(0, 0, (int)seconds));

            var embed = new DiscordEmbedBuilder();
            embed.Description = (result is null)
                ? context.FormatLocalized("guild_timeout_reset")
                : context.FormatLocalized("guild_timeout_changed", Formatter.InlineCode(result.Value.TotalSeconds.ToString()));

            await context.RespondLocalizedAsync(embed);
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
                            Formatter.InlineCode(context.GetGuildPrefix() + context.Command.QualifiedName)
                        )
                    );

                await context.RespondLocalizedAsync(errorEmbed, isError: true);
                return;
            }

            // Change the locale
            _service.GetProperty(context, x => x.Locale = responseKey);

            // Send the message
            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_locale_changed", Formatter.InlineCode(responseKey)));

            await context.RespondLocalizedAsync(embed);
        }
    }
}