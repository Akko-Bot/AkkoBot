using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Extensions;
using AkkoBot.Services;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
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
        public async Task ChangeEmbedAsync(CommandContext context)
        {
            var result = await _service.SetPropertyAsync(context.Guild, x => x.UseEmbed = !x.UseEmbed);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_embed_change", (result) ? "enabled" : "disabled"));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("okcolor")]
        [Description("cmd_guild_okcolor")]
        public async Task ChangeOkColorAsync(CommandContext context, [Description("arg_color")] string newColor)
        {
            var color = GeneralService.GetColor(newColor);

            var embed = new DiscordEmbedBuilder()
            {
                Description = (color.HasValue)
                    ? context.FormatLocalized("guild_okcolor", Formatter.InlineCode(newColor.ToUpperInvariant()))
                    : "invalid_color"
            };

            if (color.HasValue)
                await _service.SetPropertyAsync(context.Guild, x => x.OkColor = newColor);

            await context.RespondLocalizedAsync(embed);
        }

        [Command("errorcolor")]
        [Description("cmd_guild_errorcolor")]
        public async Task ChangeErrorColorAsync(CommandContext context, [Description("arg_color")] string newColor)
        {
            var color = GeneralService.GetColor(newColor);

            var embed = new DiscordEmbedBuilder()
            {
                Description = (color.HasValue)
                    ? context.FormatLocalized("guild_errorcolor", Formatter.InlineCode(newColor.ToUpperInvariant()))
                    : "invalid_color"
            };

            if (color.HasValue)
                await _service.SetPropertyAsync(context.Guild, x => x.ErrorColor = newColor);

            await context.RespondLocalizedAsync(embed);
        }

        [Command("timeout")]
        [Description("cmd_guild_timeout")]
        public async Task ChangeTimeoutAsync(CommandContext context, [Description("arg_timeout")] uint? seconds = null)
        {
            var result = await _service.SetPropertyAsync(
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
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task ChangeRoleMentionabilityAsync(CommandContext context)
        {
            var result = await _service.SetPropertyAsync(context.Guild, x => x.PermissiveRoleMention = !x.PermissiveRoleMention);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_role_mention", (result) ? "enabled" : "disabled"));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("locale"), Aliases("language")]
        [Description("cmd_guild_locale")]
        public async Task ListLocalesAsync(CommandContext context)
        {
            var locales = _service.GetLocales()
                .OrderBy(code => code);

            var embed = new DiscordEmbedBuilder()
                .WithTitle("locales_title")
                .AddField("code", string.Join('\n', locales), true)
                .AddField("language", string.Join('\n', locales.Select(x => GeneralService.GetCultureInfo(x)?.NativeName ?? x)), true);

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("locale"), Aliases("languages")]
        public async Task ChangeGuildLocaleAsync(CommandContext context, [Description("arg_locale")] string languageCode)
        {
            var success = _service.IsLocaleRegistered(languageCode, out var responseKey);

            // Change the locale
            if (success)
                await _service.SetPropertyAsync(context.Guild, x => x.Locale = responseKey);

            // Send the message
            var embed = new DiscordEmbedBuilder()
            {
                Description = (success)
                    ? context.FormatLocalized("guild_locale_changed", Formatter.InlineCode(responseKey))
                    : context.FormatLocalized("guild_locale_unavailable", Formatter.InlineCode(context.Prefix + context.Command.QualifiedName))
            };

            await context.RespondLocalizedAsync(embed, isError: !success);
        }

        [Command("timezone")]
        [Description("cmd_guild_timezone")]
        public async Task TimezoneAsync(CommandContext context, [RemainingText, Description("arg_timezone")] string timezone)
        {
            var zone = GeneralService.GetTimeZone(timezone);
            var embed = new DiscordEmbedBuilder()
            {
                Description = (zone is null)
                    ? context.FormatLocalized("guild_timezone_error", Formatter.InlineCode(context.Prefix + "timezones"))
                    : context.FormatLocalized("guild_timezone_changed", Formatter.InlineCode($"{zone.StandardName} ({zone.BaseUtcOffset.Hours:00}:{zone.BaseUtcOffset.Minutes:00})"))
            };

            if (zone is not null)
                await _service.SetPropertyAsync(context.Guild, x => x.Timezone = zone.StandardName);

            await context.RespondLocalizedAsync(embed, isError: zone is null);
        }

        [Command("bantemplate")]
        [Description("cmd_bantemplate")]
        [RequireUserPermissions(Permissions.BanMembers)]
        public async Task SetBanTemplateAsync(CommandContext context, [RemainingText, Description("arg_bantemplate")] string banTemplate = "")
        {
            var result = await _service.SetPropertyAsync(context.Guild, x => x.BanTemplate = banTemplate);

            var embed = new DiscordEmbedBuilder()
            {
                Description = (string.IsNullOrWhiteSpace(result))
                    ? "bantemplate_reset"
                    : context.FormatLocalized("bantemplate_set", Formatter.InlineCode(result))
            };

            await context.RespondLocalizedAsync(embed);
        }

        [Command("joinrole"), Aliases("jr")]
        [Description("cmd_joinrole")]
        [RequireUserPermissions(Permissions.ManageRoles)]
        public async Task AddJoinRoleAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role)
        {
            var dbGuild = _service.GetGuildSettings(context.Guild);
            var embed = new DiscordEmbedBuilder();
            var isInvalid = role.IsManaged || (!dbGuild.JoinRoles.Contains((long)role.Id) && dbGuild.JoinRoles.Count >= 3);

            if (isInvalid)
                embed.WithDescription((role.IsManaged) ? "joinrole_error" : "joinrole_limit");
            else
            {
                var result = await _service.SetPropertyAsync(
                    context.Guild,
                    x =>
                    {
                        var id = (long)role.Id;
                        var amount = x.JoinRoles.Count;

                        if (x.JoinRoles.Contains(id))
                            x.JoinRoles.Remove(id);
                        else
                            x.JoinRoles.Add(id);

                        return amount < x.JoinRoles.Count;
                    }
                );

                embed.WithDescription(context.FormatLocalized((result) ? "joinrole_added" : "joinrole_removed", Formatter.Bold(role.Name)));
            }

            await context.RespondLocalizedAsync(embed, true, isInvalid);
        }

        [Command("joinrole"), HiddenOverload]
        public async Task AddOrRemoveJoinRoleAsync(CommandContext context, ulong id)
        {
            if (context.Guild.Roles.TryGetValue(id, out var role))
            {
                await AddJoinRoleAsync(context, role);
                return;
            }

            var result = await _service.SetPropertyAsync(context.Guild, x => x.JoinRoles.Remove((long)id));
            var embed = new DiscordEmbedBuilder()
                .WithDescription((result) ? "joinrole_removed" : "role_not_found");

            await context.RespondLocalizedAsync(embed, isError: !result);
        }

        [Command("joinrole"), HiddenOverload]
        public async Task ListJoinRolesAsync(CommandContext context)
        {
            var dbGuild = _service.GetGuildSettings(context.Guild);
            var embed = new DiscordEmbedBuilder();
            var isEmpty = dbGuild.JoinRoles.Count is 0;

            if (isEmpty)
                embed.WithDescription("joinrole_list_empty");
            else
            {
                var roles = dbGuild.JoinRoles
                    .Select(x => (Id: x, RoleName: context.Guild.GetRole((ulong)x)?.Name ?? context.FormatLocalized("deleted_role")))
                    .OrderByDescending(x => x.Id);

                embed.WithTitle("joinrole_list_title")
                    .WithDescription(string.Join("\n", roles.Select(x => $"{Formatter.InlineCode(x.Id.ToString())} - {Formatter.Bold(x.RoleName)}")));
            }

            await context.RespondLocalizedAsync(embed, isEmpty, isEmpty);
        }

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_guild_list")]
        public async Task ListGuildConfigsAsync(CommandContext context)
        {
            var settings = _service.GetGuildSettings(context.Guild).GetSettings();

            var embed = new DiscordEmbedBuilder()
                .WithTitle("guild_settings_title")
                .AddField("settings", string.Join("\n", settings.Keys), true)
                .AddField("value", string.Join("\n", settings.Values), true);

            await context.RespondLocalizedAsync(embed);
        }

        [Group("delmsgoncmd"), Aliases("deletemessageoncommand", "deletemsgoncmd")]
        [Description("cmd_guild_delmsgoncmd")]
        public class DeleteMessageOnCommand : AkkoCommandModule
        {
            private readonly GuildConfigService _service;

            public DeleteMessageOnCommand(GuildConfigService service)
                => _service = service;

            [Command("ignore")]
            public async Task AddUserAsync(CommandContext context, [Description("arg_discord_user")] DiscordMember user)
                => await AddIdsAsync(context, user.Id);

            [Command("ignore")]
            public async Task AddChannelAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel)
                => await AddIdsAsync(context, channel.Id);

            [Command("ignore")]
            public async Task AddRoleAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role)
                => await AddIdsAsync(context, role.Id);

            [Command("ignore")]
            [Description("cmd_guild_delmsgoncmd_ignore")]
            public async Task AddIdsAsync(CommandContext context, [Description("arg_fw_ids")] params ulong[] ids)
            {
                var result = await _service.SetPropertyAsync(context.Guild, x => UpdateDelCmdBlacklist(x, ids));

                var embed = new DiscordEmbedBuilder()
                    .WithDescription(context.FormatLocalized((result) ? "delmsgoncmd_added_id" : "delmsgoncmd_removed_id", ids.Length));

                await context.RespondLocalizedAsync(embed);
            }

            [Command("clearignored")]
            [Description("cmd_guild_delmsgoncmd_clearignored")]
            public async Task ClearIdsAsync(CommandContext context)
            {
                var result = await _service.SetPropertyAsync(context.Guild, x =>
                {
                    var amount = x.DelCmdBlacklist.Count;
                    x.DelCmdBlacklist.Clear();

                    return amount;
                });

                var embed = new DiscordEmbedBuilder()
                    .WithDescription(context.FormatLocalized((result is not 0) ? "delmsgoncmd_removed_id" : "delmsgoncmd_empty", result));

                await context.RespondLocalizedAsync(embed, isError: result is 0);
            }

            [Command("listignored"), Aliases("list", "show")]
            [Description("cmd_guild_delmsgoncmd_listignored")]
            public async Task ListIgnoredIdsAsync(CommandContext context)
            {
                var dbGuild = _service.GetGuildSettings(context.Guild);
                var idsString = string.Join(", ", dbGuild.DelCmdBlacklist);
                var embed = new DiscordEmbedBuilder()
                    .WithTitle("delmsgoncmd_ignored_ids")
                    .WithDescription(string.IsNullOrWhiteSpace(idsString) ? "delmsgoncmd_empty" : idsString);

                await context.RespondLocalizedAsync(embed, false);
            }

            [GroupCommand, Command("invert")]
            [Description("cmd_guild_delmsgoncmd_toggle")]
            public async Task ToggleDelCmdOnCmdAsync(CommandContext context)
            {
                var result = await _service.SetPropertyAsync(context.Guild, x => x.DeleteCmdOnMessage = !x.DeleteCmdOnMessage);

                var embed = new DiscordEmbedBuilder()
                    .WithDescription((result) ? "delmsgoncmd_enabled" : "delmsgoncmd_disabled");

                await context.RespondLocalizedAsync(embed);
            }

            /// <summary>
            /// Updates the list of blacklisted IDs for automatic command message deletion.
            /// </summary>
            /// <param name="dbGuild">The guild settings.</param>
            /// <param name="contextIds">The IDs to be included or removed from the list.</param>
            /// <returns><see langword="true"/> if the ID list has increased, <see langword="false"/> otherwise.</returns>
            private bool UpdateDelCmdBlacklist(GuildConfigEntity dbGuild, params ulong[] contextIds)
            {
                var amount = dbGuild.DelCmdBlacklist.Count;

                foreach (long id in contextIds)
                {
                    if (dbGuild.DelCmdBlacklist.Contains(id))
                        dbGuild.DelCmdBlacklist.Remove(id);
                    else
                        dbGuild.DelCmdBlacklist.Add(id);
                }

                return amount < dbGuild.DelCmdBlacklist.Count;
            }
        }
    }
}