using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Commands.Modules.Self.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Models.Serializable.EmbedParts;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration;

[RequireGuild]
public sealed class WarningCommands : AkkoCommandModule
{
    private const string _pencil = ":pencil:";
    private const string _warning = "\u26A0";
    private readonly WarningService _warnService;
    private readonly RoleService _roleService;
    private readonly BotConfigService _botService;

    public WarningCommands(WarningService warnService, RoleService roleService, BotConfigService botService)
    {
        _warnService = warnService;
        _roleService = roleService;
        _botService = botService;
    }

    [Command("notice"), Aliases("note")]
    [Description("cmd_notice")]
    [RequireUserPermissions(Permissions.KickMembers)]
    public async Task NoticeAsync(
        CommandContext context,
        [Description("arg_discord_user")] DiscordMember user,
        [RemainingText, Description("arg_notice")] string notice)
    {
        if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
            return;

        await NoticeAsync(context, user as DiscordUser, notice);
    }

    [Command("notice"), HiddenOverload]
    public async Task NoticeAsync(
        CommandContext context,
        [Description("arg_discord_user")] DiscordUser user,
        [RemainingText, Description("arg_notice")] string note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return;

        await _warnService.SaveInfractionAsync(context, user, note);

        var embed = new SerializableDiscordEmbed()
            .WithDescription("notice_success");

        await context.RespondLocalizedAsync(embed);
    }

    [Command("warn")]
    [Description("cmd_warn")]
    [RequireUserPermissions(Permissions.KickMembers)]
    public async Task WarnAsync(
        CommandContext context,
        [Description("arg_discord_user")] DiscordMember user,
        [RemainingText, Description("arg_infraction")] string? reason = default)
    {
        if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
            return;

        // Dm the user about the warn
        var notification = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("warn_dm", Formatter.Bold(context.Guild.Name)));

        if (reason is not null)
            notification.AddField("reason", reason);

        var dm = await context.SendLocalizedDmAsync(user.Id, notification, true);

        // Save warning to the database
        var punishment = await _warnService.SaveWarnAsync(context, user, reason);
        var embed = new SerializableDiscordEmbed();

        if (punishment is not null)
        {
            embed.WithDescription(
                context.FormatLocalized(
                    "warn_and_punish",
                    Formatter.Bold(user.GetFullname()),
                    punishment.ToString()!.ToLowerInvariant() + "_enum"
                )
            );
        }
        else
        {
            embed.WithDescription(
                context.FormatLocalized(
                    "warn_success",
                    Formatter.Bold(user.GetFullname())
                )
            );
        }

        if (dm is null)
            embed.WithFooter(AkkoStatics.WarningEmoji.Name + context.FormatLocalized("punishment_dm_failed"));

        await context.RespondLocalizedAsync(embed);
    }

    [Command("warnclear"), Aliases("warnc")]
    [Description("cmd_warnc")]
    [RequireUserPermissions(Permissions.KickMembers)]
    public async Task UnwarnAsync(
        CommandContext context,
        [Description("arg_discord_user")] DiscordMember user,
        [Description("arg_uint")] int? id = null)
    {
        if (!await _roleService.SoftCheckHierarchyAsync(context, user, "error_hierarchy"))
            return;

        // Remove from the database
        var amount = await _warnService.RemoveInfractionAsync(context.Guild, user, id);

        // Send confirmation message
        var embed = new SerializableDiscordEmbed();

        if (amount == 1 && id.HasValue)
            embed.WithDescription(context.FormatLocalized("unwarn_success", Formatter.InlineCode("#" + id), Formatter.Bold(user.GetFullname())));
        else if (amount >= 1)
            embed.WithDescription(context.FormatLocalized("unwarn_all", Formatter.Bold(user.GetFullname())));
        else
            embed.WithDescription(context.FormatLocalized("unwarn_failure", Formatter.InlineCode("#" + id)));

        await context.RespondLocalizedAsync(embed, true, amount == 0);
    }

    [Command("infractions"), Aliases("warnlog")]
    [Description("cmd_infractions")]
    public async Task InfractionsAsync(CommandContext context, [Description("arg_discord_user")] DiscordUser? user = default)
    {
        if (!context.Member.PermissionsIn(context.Channel).HasFlag(Permissions.KickMembers) && user is not null)
            return;

        user ??= context.User;
        var infractions = await _warnService.GetInfractionsAsync(context.Guild, user, WarnType.Warning);
        var fields = new List<SerializableEmbedField>(infractions.Count);

        var embed = new SerializableDiscordEmbed()
            .WithTitle(context.FormatLocalized($"infractions_title", user.GetFullname()));

        foreach (var (modName, infraction) in infractions)
        {
            var position = "#" + Formatter.InlineCode(infraction.Id.ToString());
            var fieldName = context.FormatLocalized(
                "infractions_field",
                $"{position} {infraction.DateAdded.Date.ToShortDateString()}",
                infraction.DateAdded.ToString(@"HH:mm"),
                modName
            );

            fields.Add(new(fieldName, infraction.WarningText ?? "-"));
        }

        if (infractions.Count is 0)
            embed.WithDescription("infractions_empty");

        await context.RespondPaginatedByFieldsAsync(embed, fields);
    }

    [Command("modlog")]
    [Description("cmd_modlog")]
    [RequireUserPermissions(Permissions.KickMembers)]
    public async Task ModLogAsync(CommandContext context, [Description("arg_discord_user")] DiscordUser? user = default)
    {
        user ??= context.User;
        var infractions = await _warnService.GetInfractionsAsync(context.Guild, user);
        var occurrence = await _warnService.GetUserOccurrencesAsync(context.Guild, user);
        var fields = new List<SerializableEmbedField>(infractions.Count);

        var embed = new SerializableDiscordEmbed()
            .WithTitle(context.FormatLocalized($"infractions_title", user.GetFullname()))
            .WithDescription(
                context.FormatLocalized(
                    "modlog_description",
                    occurrence.Notices, occurrence.Warnings, occurrence.Mutes,
                    occurrence.Kicks, occurrence.Softbans, occurrence.Bans
                )
            );

        foreach (var (modName, infraction) in infractions.OrderBy(x => x.Item2.Type))
        {
            var emote = (infraction.Type is WarnType.Notice) ? _pencil : _warning;
            var position = "#" + Formatter.InlineCode(infraction.Id.ToString());
            var fieldName = context.FormatLocalized(
                "infractions_field",
                $"{emote} {position} {infraction.DateAdded.Date.ToShortDateString()}",
                infraction.DateAdded.ToString(@"HH:mm"),
                modName.ToString()
            );

            fields.Add(new(fieldName, infraction.WarningText ?? "-"));
        }

        if (infractions.Count is 0)
            embed.Body!.Description += "\n\n" + context.FormatLocalized("infractions_empty");

        await context.RespondPaginatedByFieldsAsync(embed, fields);
    }

    [Command("warnpunishment"), HiddenOverload]
    public async Task WarnpAsync(CommandContext context)
        => await WarnplAsync(context);

    [Command("warnpunishment"), HiddenOverload]
    public async Task WarnpAsync(CommandContext context, int amount, PunishmentType punishmentType, TimeSpan? time = default)
        => await WarnpAsync(context, amount, punishmentType, default, time);

    [Command("warnpunishment"), Aliases("warnp")]
    [Description("cmd_warnp")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task WarnpAsync(
        CommandContext context,
        [Description("arg_warnp_amount")] int amount,
        [Description("arg_warnp_type")] PunishmentType punishmentType,
        [Description("arg_warnp_role")] DiscordRole? role = default,
        [Description("arg_warnp_time")] TimeSpan? time = default)
    {
        await _warnService.SaveWarnPunishmentAsync(context.Guild, amount, punishmentType, role, time);

        var embed = new SerializableDiscordEmbed()
            .WithDescription("warnp_success");

        await context.RespondLocalizedAsync(embed);
    }

    [Command("warnpunishment")]
    public async Task WarnpAsync(CommandContext context, [Description("arg_warnp_rem_amount")] int amount)
    {
        var embed = new SerializableDiscordEmbed();
        var isRemoved = await _warnService.RemoveWarnPunishmentAsync(context.Guild, amount);

        if (isRemoved)
            embed.WithDescription("warnp_rem_success");
        else
            embed.WithDescription("warnp_rem_failure");

        await context.RespondLocalizedAsync(embed, true, !isRemoved);
    }

    [Command("warnpunishmentlist"), Aliases("warnpl")]
    [Description("cmd_warnpl")]
    public async Task WarnplAsync(CommandContext context)
    {
        var punishments = await _warnService.GetServerPunishmentsAsync(context.Guild);

        var embed = new SerializableDiscordEmbed()
            .WithTitle("warnpl_title");

        if (punishments.Count == 0)
        {
            embed.WithDescription("warnpl_empty");
            await context.RespondLocalizedAsync(embed, false, true);
            return;
        }

        var amount = string.Join("\n", punishments.Select(x => x.WarnAmount));

        var punish = string.Join(
            "\n",
            punishments.Select(x =>
                (context.Guild.Roles.TryGetValue(x.PunishRoleId ?? default, out var punishRole))
                    ? context.FormatLocalized(x.Type.ToString().ToLowerInvariant()) + ": " + punishRole.Name
                    : context.FormatLocalized(x.Type.ToString().ToLowerInvariant())
            )
        );

        var interval = string.Join("\n", punishments.Select(x => x.Interval?.ToString(@"%d\d\ %h\h\ %m\m") ?? "-"));

        embed.AddField("warnpl_amount", amount, true)
            .AddField("warnpl_punish", punish, true)
            .AddField("expires_in", interval, true);

        await context.RespondLocalizedAsync(embed);
    }

    [Command("warnexpire"), Aliases("warne")]
    [Description("cmd_warne")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task WarneAsync(CommandContext context, [Description("arg_timed_warn")] TimeSpan time)
    {
        var botConfig = _botService.GetConfig();
        var embed = new SerializableDiscordEmbed();

        if (time < botConfig.MinWarnExpire && time != TimeSpan.Zero)
        {
            embed.WithDescription(context.FormatLocalized("warne_failure", botConfig.MinWarnExpire.Days));
            await context.RespondLocalizedAsync(embed, isError: true);

            return;
        }

        await _warnService.SaveWarnExpireAsync(context, time);
        embed.WithDescription(context.FormatLocalized("warne_success", time.Days));

        await context.RespondLocalizedAsync(embed);
    }
}