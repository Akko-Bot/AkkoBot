using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kotz.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration;

[Group("serverconfig"), Aliases("guildconfig", "server", "guild")]
[Description("cmd_guild")]
[RequireGuild, RequireUserPermissions(Permissions.ManageGuild)]
public sealed class GuildConfig : AkkoCommandModule
{
    private readonly GuildConfigService _service;
    private readonly UserPunishmentService _punishService;

    public GuildConfig(GuildConfigService service, UserPunishmentService punishService)
    {
        _service = service;
        _punishService = punishService;
    }

    [Command("embed")]
    [Description("cmd_guild_embed")]
    public async Task ChangeEmbedAsync(CommandContext context)
    {
        var result = await _service.SetPropertyAsync(context.Guild, x => x.Behavior = x.Behavior.ToggleFlag(GuildConfigBehavior.UseEmbed));

        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("guild_embed_change", (result.HasFlag(GuildConfigBehavior.UseEmbed)) ? "enabled" : "disabled"));

        await context.RespondLocalizedAsync(embed);
    }

    [Command("okcolor")]
    [Description("cmd_guild_okcolor")]
    public async Task ChangeOkColorAsync(CommandContext context, [Description("arg_color")] string newColor)
    {
        var color = AkkoUtilities.GetColor(newColor);

        var embed = new SerializableDiscordEmbed()
            .WithDescription(
                (color.HasValue)
                    ? context.FormatLocalized("guild_okcolor", Formatter.InlineCode(newColor.ToUpperInvariant()))
                    : "invalid_color"
            );

        if (color.HasValue)
            await _service.SetPropertyAsync(context.Guild, x => x.OkColor = newColor);

        await context.RespondLocalizedAsync(embed);
    }

    [Command("errorcolor")]
    [Description("cmd_guild_errorcolor")]
    public async Task ChangeErrorColorAsync(CommandContext context, [Description("arg_color")] string newColor)
    {
        var color = AkkoUtilities.GetColor(newColor);

        var embed = new SerializableDiscordEmbed()
            .WithDescription(
                (color.HasValue)
                    ? context.FormatLocalized("guild_errorcolor", Formatter.InlineCode(newColor.ToUpperInvariant()))
                    : "invalid_color"
            );

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

        var embed = new SerializableDiscordEmbed()
            .WithDescription(
                (result is null)
                    ? context.FormatLocalized("guild_timeout_reset")
                    : context.FormatLocalized("guild_timeout_changed", Formatter.InlineCode(result.Value.TotalSeconds.ToString()))
            );

        await context.RespondLocalizedAsync(embed);
    }

    [Command("permissivemention"), Aliases("rolemention")]
    [Description("cmd_guild_rolemention")]
    [RequireUserPermissions(Permissions.ManageRoles)]
    public async Task ChangeRoleMentionabilityAsync(CommandContext context)
    {
        var result = await _service.SetPropertyAsync(context.Guild, x => x.Behavior = x.Behavior.ToggleFlag(GuildConfigBehavior.PermissiveRoleMention));

        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("guild_role_mention", (result.HasFlag(GuildConfigBehavior.PermissiveRoleMention)) ? "enabled" : "disabled"));

        await context.RespondLocalizedAsync(embed);
    }

    [Command("locale"), Aliases("language")]
    [Description("cmd_guild_locale")]
    public async Task ListLocalesAsync(CommandContext context)
    {
        var locales = _service.GetLocales()
            .OrderBy(code => code);

        var embed = new SerializableDiscordEmbed()
            .WithTitle("locales_title")
            .AddField("code", string.Join('\n', locales), true)
            .AddField("language", string.Join('\n', locales.Select(x => AkkoUtilities.GetCultureInfo(x)?.NativeName ?? x)), true);

        await context.RespondLocalizedAsync(embed, false);
    }

    [Command("locale"), Aliases("languages")]
    public async Task ChangeGuildLocaleAsync(CommandContext context, [Description("arg_locale")] string languageCode)
    {
        var success = _service.IsLocaleRegistered(languageCode, out var responseKey);

        // Change the locale
        if (success)
            await _service.SetPropertyAsync(context.Guild, x => x.Locale = responseKey!);

        // Send the message
        var embed = new SerializableDiscordEmbed()
            .WithDescription(
                (success)
                    ? context.FormatLocalized("guild_locale_changed", Formatter.InlineCode(responseKey))
                    : context.FormatLocalized("guild_locale_unavailable", Formatter.InlineCode(context.Prefix + context.Command.QualifiedName))
            );

        await context.RespondLocalizedAsync(embed, isError: !success);
    }

    [Command("timezone")]
    [Description("cmd_guild_timezone")]
    public async Task TimezoneAsync(CommandContext context, [RemainingText, Description("arg_timezone")] string timezone)
    {
        var zone = AkkoUtilities.GetTimeZone(timezone);
        var embed = new SerializableDiscordEmbed()
            .WithDescription(
                (zone is null)
                    ? context.FormatLocalized("guild_timezone_error", Formatter.InlineCode(context.Prefix + "timezones"))
                    : context.FormatLocalized("guild_timezone_changed", Formatter.InlineCode($"{zone.StandardName} ({zone.BaseUtcOffset.Hours:00}:{zone.BaseUtcOffset.Minutes:00})"))
            );

        if (zone is not null)
            await _service.SetPropertyAsync(context.Guild, x => x.Timezone = zone.StandardName);

        await context.RespondLocalizedAsync(embed, isError: zone is null);
    }

    [Command("bantemplate")]
    [Description("cmd_bantemplate")]
    [RequireUserPermissions(Permissions.BanMembers)]
    [RequireBotPermissions(Permissions.AddReactions)]
    public async Task SetBanTemplateAsync(CommandContext context, [RemainingText, Description("arg_bantemplate")] string banTemplate = "")
    {
        if (string.IsNullOrWhiteSpace(banTemplate))
        {
            var message = await _punishService.SendBanDmAsync(context, context.Member, "reason");
            await context.Message.CreateReactionAsync((message is not null) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);

            return;
        }

        var result = await _service.SetPropertyAsync(context.Guild, x => x.BanTemplate = banTemplate);

        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("bantemplate_set", Formatter.InlineCode(result)));

        await context.RespondLocalizedAsync(embed);
    }

    [Command("bantemplateclear")]
    [Description("cmd_bantemplateclear")]
    public async Task ClearBanTemplateAsync(CommandContext context)
    {
        await _service.SetPropertyAsync(context.Guild, x => x.BanTemplate = default);

        var embed = new SerializableDiscordEmbed()
            .WithDescription("bantemplate_reset");

        await context.RespondLocalizedAsync(embed);
    }

    [Command("joinrole"), Aliases("jr")]
    [Description("cmd_joinrole")]
    [RequireUserPermissions(Permissions.ManageRoles)]
    public async Task AddJoinRoleAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role)
    {
        var dbGuild = _service.GetGuildSettings(context.Guild);
        var embed = new SerializableDiscordEmbed();
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
        var embed = new SerializableDiscordEmbed()
            .WithDescription((result) ? "joinrole_removed" : "role_not_found");

        await context.RespondLocalizedAsync(embed, isError: !result);
    }

    [Command("joinrole"), HiddenOverload]
    public async Task ListJoinRolesAsync(CommandContext context)
    {
        var dbGuild = _service.GetGuildSettings(context.Guild);
        var embed = new SerializableDiscordEmbed();
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

    [Command("ignoreglobaltags")]
    [Description("cmd_guild_ignoreglobaltags")]
    public async Task ToggleIgnoreGlobalTagsAsync(CommandContext context)
    {
        var result = await _service.SetPropertyAsync(context.Guild, x => x.Behavior = x.Behavior.ToggleFlag(GuildConfigBehavior.IgnoreGlobalTags));
        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("ignore_global_list_description", (!result.HasFlag(GuildConfigBehavior.IgnoreGlobalTags)) ? "enabled" : "disabled"));

        await context.RespondLocalizedAsync(embed);
    }

    [Command("basetagpermission"), Aliases("mintagperm")]
    [Description("cmd_guild_mintagperm")]
    public async Task SetMinTagPermissionAsync(CommandContext context, [Description("arg_permissions")] params Permissions[] permissions)
    {
        var result = await _service.SetPropertyAsync(context.Guild, x => x.MinimumTagPermissions = permissions.ToFlags());
        await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
    }

    [GroupCommand, Command("list"), Aliases("show")]
    [Description("cmd_guild_list")]
    public async Task ListGuildConfigsAsync(CommandContext context)
    {
        var settings = _service.GetGuildSettings(context.Guild).GetSettings();

        var embed = new SerializableDiscordEmbed()
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
            => await AddIdsAsync(context, user);

        [Command("ignore")]
        public async Task AddChannelAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel)
            => await AddIdsAsync(context, channel);

        [Command("ignore")]
        public async Task AddRoleAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role)
            => await AddIdsAsync(context, role);

        [Command("ignore")]
        [Description("cmd_guild_delmsgoncmd_ignore")]
        public async Task AddIdsAsync(CommandContext context, [Description("arg_snowflakes")] params SnowflakeObject[] ids)
        {
            var result = await _service.SetPropertyAsync(context.Guild, x => UpdateDelCmdBlacklist(x, ids.Select(x => x.Id)));

            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized((result) ? "ignored_ids_add" : "ignored_ids_remove", ids.Length));

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

            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized((result is not 0) ? "ignored_ids_remove" : "delmsgoncmd_empty", result));

            await context.RespondLocalizedAsync(embed, isError: result is 0);
        }

        [Command("listignored"), Aliases("list", "show")]
        [Description("cmd_guild_delmsgoncmd_listignored")]
        public async Task ListIgnoredIdsAsync(CommandContext context)
        {
            var dbGuild = _service.GetGuildSettings(context.Guild);
            var idsString = string.Join(", ", dbGuild.DelCmdBlacklist);
            var embed = new SerializableDiscordEmbed()
                .WithTitle("ignored_ids_list")
                .WithDescription(string.IsNullOrWhiteSpace(idsString) ? "ignored_ids_empty" : idsString);

            await context.RespondLocalizedAsync(embed, false);
        }

        [GroupCommand, Command("invert")]
        [Description("cmd_guild_delmsgoncmd_toggle")]
        public async Task ToggleDelCmdOnCmdAsync(CommandContext context)
        {
            var result = await _service.SetPropertyAsync(context.Guild, x => x.Behavior = x.Behavior.ToggleFlag(GuildConfigBehavior.DeleteCmdOnMessage));

            var embed = new SerializableDiscordEmbed()
                .WithDescription((result.HasFlag(GuildConfigBehavior.DeleteCmdOnMessage)) ? "delmsgoncmd_enabled" : "delmsgoncmd_disabled");

            await context.RespondLocalizedAsync(embed);
        }

        /// <summary>
        /// Updates the list of blacklisted IDs for automatic command message deletion.
        /// </summary>
        /// <param name="dbGuild">The guild settings.</param>
        /// <param name="contextIds">The IDs to be included or removed from the list.</param>
        /// <returns><see langword="true"/> if the list of IDs has increased, <see langword="false"/> otherwise.</returns>
        private bool UpdateDelCmdBlacklist(GuildConfigEntity dbGuild, IEnumerable<ulong> contextIds)
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