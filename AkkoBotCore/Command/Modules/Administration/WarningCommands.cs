using System;
using AkkoBot.Command.Abstractions;
using AkkoBot.Command.Attributes;
using AkkoBot.Command.Modules.Administration.Services;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Command.Modules.Administration
{
    public class WarningCommands : AkkoCommandModule
    {
        private readonly WarningService _warnService;
        private readonly RoleService _roleService;

        public WarningCommands(WarningService warnService, RoleService roleService)
        {
            _warnService = warnService;
            _roleService = roleService;
        }

        [Command("notice"), Aliases("note")]
        [Description("cmd_notice")]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task Notice(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_notice")] string notice)
        {
            if (!await _roleService.HierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            await Notice(context, user as DiscordUser, notice);
        }

        [Command("notice"), HiddenOverload]
        public async Task Notice(
            CommandContext context,
            [Description("arg_discord_user")] DiscordUser user,
            [RemainingText, Description("arg_notice")] string note)
        {
            if (string.IsNullOrWhiteSpace(note))
                return;

            await _warnService.SaveInfractionAsync(context, user, note);

            var embed = new DiscordEmbedBuilder()
                .WithDescription("notice_success");

            await context.RespondLocalizedAsync(embed);
        }

        [Command("warn")]
        [Description("cmd_warn")]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task Warn(CommandContext context,
        [Description("arg_discord_user")] DiscordMember user,
        [RemainingText, Description("arg_infraction")] string reason = null)
        {
            if (!await _roleService.HierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            // Dm the user about the warn
            var dm = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("warn_dm", Formatter.Bold(context.Guild.Name)));
            
            if (reason is not null)
                dm.AddField("reason", reason);

            await context.SendLocalizedDmAsync(user, dm, true);

            // Save warning to the database
            var (wasPunished, punishment) = await _warnService.SaveWarnAsync(context, user, reason);
            var embed = new DiscordEmbedBuilder();

            if (wasPunished)
            {
                embed.WithDescription(
                    context.FormatLocalized(
                        "warn_and_punish",
                        Formatter.Bold(user.GetFullname()),
                        context.FormatLocalized(punishment.ToString().ToLowerInvariant() + "_enum")
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

            await context.RespondLocalizedAsync(embed);
        }

        [Command("warnclear"), Aliases("warnc")]
        [Description("cmd_warnc")]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task Unwarn(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [Description("arg_uint")] int? id = null)
        {
            if (!await _roleService.SoftHierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            // Remove from the database
            var amount = await _warnService.RemoveInfractionAsync(context.Guild, user, id);

            // Send confirmation message
            var embed = new DiscordEmbedBuilder();

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
        public async Task Infractions(CommandContext context, [Description("arg_discord_user")] DiscordUser user = null)
        {
            if (!context.Member.PermissionsIn(context.Channel).HasFlag(Permissions.KickMembers) && user is not null)
                return;
            else if (user is null)
                user = context.User;

            var (guildSettings, users) = await _warnService.GetInfractionsAsync(context.Guild, user, WarnType.Warning);

            var embed = new DiscordEmbedBuilder()
                .WithTitle(context.FormatLocalized($"infractions_title", user.GetFullname()));

            foreach (var warn in guildSettings.WarnRel)
            {
                var position = "#" + Formatter.InlineCode(warn.Id.ToString());
                var fieldName = context.FormatLocalized(
                    "infractions_field",
                    $"{position} {warn.DateAdded.Date.ToShortDateString()}",
                    $"{warn.DateAdded.Hour:00.}:{warn.DateAdded.Minute:00.}",
                    users.FirstOrDefault(x => x.UserId == warn.AuthorId).ToString()
                );

                embed.AddField(fieldName, warn.WarningText);
            }

            if (guildSettings.WarnRel.Count == 0)
                embed.WithDescription("infractions_empty");

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("modlog")]
        [Description("cmd_modlog")]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task ModLog(CommandContext context, [Description("arg_discord_user")] DiscordUser user = null)
        {
            if (user is null)
                user = context.User;

            var (guildSettings, users) = await _warnService.GetInfractionsAsync(context.Guild, user);
            var occurrence = guildSettings.OccurrenceRel.FirstOrDefault() ?? new OccurrenceEntity();

            var embed = new DiscordEmbedBuilder()
                .WithTitle(context.FormatLocalized($"infractions_title", user.GetFullname()))
                .WithDescription(
                    context.FormatLocalized(
                        "modlog_description",
                        occurrence.Notices, occurrence.Warnings, occurrence.Mutes,
                        occurrence.Kicks, occurrence.Softbans, occurrence.Bans
                    )
                );

            foreach (var warn in guildSettings.WarnRel.OrderBy(x => x.Type))
            {
                var emote = (warn.Type == WarnType.Notice) ? "📝" : "⚠️";
                var position = "#" + Formatter.InlineCode(warn.Id.ToString());
                var fieldName = context.FormatLocalized(
                    "infractions_field",
                    $"{emote} {position} {warn.DateAdded.Date.ToShortDateString()}",
                    $"{warn.DateAdded.Hour:00.}:{warn.DateAdded.Minute:00.}",
                    users.FirstOrDefault(x => x.UserId == warn.AuthorId).ToString()
                );

                embed.AddField(fieldName, warn.WarningText);
            }

            if (guildSettings.WarnRel.Count == 0)
                embed.Description += "\n\n" + context.FormatLocalized("infractions_empty");

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("warnpunishment"), HiddenOverload]
        public async Task Warnp(CommandContext context, int amount, WarnPunishType punishmentType, TimeSpan? time = null)
            => await Warnp(context, amount, punishmentType, null, time);

        [Command("warnpunishment"), Aliases("warnp")]
        [Description("cmd_warnp")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task Warnp(
            CommandContext context,
            [Description("arg_warnp_amount")] int amount,
            [Description("arg_warnp_type")] WarnPunishType punishmentType,
            [Description("arg_warnp_role")] DiscordRole role = null,
            [Description("arg_warnp_time")] TimeSpan? time = null)
        {
            await _warnService.SaveWarnPunishmentAsync(context.Guild, amount, punishmentType, role, time);

            var embed = new DiscordEmbedBuilder()
                .WithDescription("warnp_success");

            await context.RespondLocalizedAsync(embed);
        }

        [Command("warnpunishment")]
        public async Task Warnp(CommandContext context, [Description("arg_warnp_rem_amount")] int amount)
        {
            var embed = new DiscordEmbedBuilder();
            var isRemoved = await _warnService.RemoveWarnPunishmentAsync(context.Guild, amount);

            if (isRemoved)
                embed.WithDescription("warnp_rem_success");
            else
                embed.WithDescription("warnp_rem_failure");

            await context.RespondLocalizedAsync(embed, true, !isRemoved);
        }

        [Command("warnpunishmentlist"), Aliases("warnpl")]
        [Description("cmd_warnpl")]
        public async Task Warnpl(CommandContext context)
        {
            var punishments = await _warnService.GetServerPunishmentsAsync(context.Guild);

            var embed = new DiscordEmbedBuilder()
                .WithTitle("warnpl_title");

            if (punishments.Count == 0)
            {
                embed.WithDescription("warnpl_empty");
                await context.RespondLocalizedAsync(embed, false, true);
                return;
            }

            var amount = string.Join("\n", punishments.Select(x => x.WarnAmount).ToArray());

            var punish = string.Join(
                "\n",
                punishments.Select(x =>
                    (context.Guild.Roles.TryGetValue(x.PunishRoleId ?? default, out var punishRole))
                        ? context.FormatLocalized(x.Type.ToString().ToLowerInvariant()) + ": " + punishRole.Name
                        : context.FormatLocalized(x.Type.ToString().ToLowerInvariant())
                ).ToArray()
            );

            var interval = string.Join("\n", punishments.Select(x => x.Interval?.ToString(@"%d\d\ %h\h\ %m\m") ?? "-").ToArray());

            embed.AddField("warnpl_amount", amount, true)
                .AddField("warnpl_punish", punish, true)
                .AddField("warnpl_interval", interval, true);

            await context.RespondLocalizedAsync(embed);
        }

        [Command("warnexpire"), Aliases("warne")]
        [Description("cmd_warne")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task Warne(CommandContext context, [Description("arg_timed_warn")] TimeSpan time)
        {
            var embed = new DiscordEmbedBuilder();

            if (time < TimeSpan.FromDays(30) && time != TimeSpan.Zero)
            {
                embed.WithDescription(context.FormatLocalized("warne_failure", TimeSpan.FromDays(30).Days));
                await context.RespondLocalizedAsync(embed, isError: true);

                return;
            }

            await _warnService.SaveWarnExpireAsync(context, time);
            embed.WithDescription(context.FormatLocalized("warne_success", time.Days));

            await context.RespondLocalizedAsync(embed);
        }
    }
}