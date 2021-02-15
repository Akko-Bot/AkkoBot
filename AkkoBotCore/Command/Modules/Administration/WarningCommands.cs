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

        [Command("note")]
        [Description("cmd_note")]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task Note(CommandContext context, DiscordMember user, [RemainingText] string note)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            await Note(context, user, note);
        }

        [Command("note"), HiddenOverload]
        public async Task Note(CommandContext context, DiscordUser user, [RemainingText] string note)
        {
            await _warnService.SaveRecord(context, user, note);

            var embed = new DiscordEmbedBuilder()
                .WithDescription("note_success");

            await context.RespondLocalizedAsync(embed);
        }

        [Command("warn")]
        [Description("cmd_warn")]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task Warn(CommandContext context, DiscordMember user, [RemainingText] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // Dm the user about the warn
            var dm = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("warn_dm", Formatter.Bold(context.Guild.Name)));
            
            if (reason is not null)
                dm.AddField("reason", reason);

            await context.SendLocalizedDmAsync(user, dm, true);

            // Save warning to the database
            var (wasPunished, punishment) = await _warnService.SaveWarn(context, user, reason);
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
        [RequireUserPermissions(Permissions.BanMembers)]
        public async Task Unwarn(CommandContext context, DiscordMember user, int position)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // Remove from the database
            await _warnService.RemoveNote(context.Guild, user, position, WarnType.Warning);

            // Send confirmation message
            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("unwarn_success", Formatter.Bold("#" + position), Formatter.Bold(user.GetFullname())));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("warnlog")]
        [Description("cmd_warnlog")]
        public async Task WarnLog(CommandContext context, DiscordUser user = null)
        {
            if (!context.Member.PermissionsIn(context.Channel).HasFlag(Permissions.KickMembers) && user is not null)
                return;
            else if (user is null)
                user = context.User;

            await ListRecords(context, user, WarnType.Warning, "warnlog");
        }

        [Command("noticelog")]
        [Description("cmd_noticelog")]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task NoticeLog(CommandContext context, DiscordUser user = null)
        {
            if (user is null)
                user = context.User;

            await ListRecords(context, user, WarnType.Notice, "notice");
        }

        // Make modlog that merges both and tracks notices, warnings, mutes, kicks, sbs and bans?
        private async Task ListRecords(CommandContext context, DiscordUser user, WarnType type, string keyType)
        {
            var (warnings, users) = await _warnService.GetRecords(context.Guild, user, type);

            var embed = new DiscordEmbedBuilder()
                .WithTitle(context.FormatLocalized($"{keyType}_title", user.GetFullname()));

            var counter = 1;
            foreach (var warn in warnings)
            {
                var position = "#" + Formatter.InlineCode(counter++.ToString());
                var fieldName = context.FormatLocalized(
                    "registerlog_field",
                    $"{position} {warn.DateAdded.Date.ToShortDateString()}",
                    $"{warn.DateAdded.Hour}:{warn.DateAdded.Minute}",
                    users.FirstOrDefault(x => x.UserId == warn.AuthorId).ToString()
                );

                embed.AddField(fieldName, warn.WarningText);
            }

            if (warnings.Count == 0)
                embed.WithDescription($"{keyType}_empty");

            await context.RespondLocalizedAsync(embed, false, warnings.Count == 0);
        }
    }
}