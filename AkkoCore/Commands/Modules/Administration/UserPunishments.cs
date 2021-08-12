using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration
{
    [RequireGuild]
    public class UserPunishment : AkkoCommandModule
    {
        private readonly RoleService _roleService;
        private readonly UserPunishmentService _punishService;

        public UserPunishment(RoleService roleservice, UserPunishmentService punishService)
        {
            _roleService = roleservice;
            _punishService = punishService;
        }

        [Command("kick"), Aliases("k")]
        [Description("cmd_kick")]
        [RequirePermissions(Permissions.KickMembers)]
        public async Task KickAsync(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // This returns null if it fails
            var dmMsg = await _punishService.SendPunishmentDmAsync(context, user, "kick_notification", reason);

            // Kick the user
            await _punishService.KickUserAsync(context, user, $"{context.Member.GetFullname()} | {reason}");

            // Send kick message to the context channel
            var embed = _punishService.GetPunishEmbed(context, user, string.Empty, "kick_title");

            if (dmMsg is null && !user.IsBot)
                embed.WithFooter(AkkoStatics.WarningEmoji.Name + context.FormatLocalized("punishment_dm_failed"));

            await context.RespondLocalizedAsync(embed, false);
        }

        [HiddenOverload]
        [Command("softban"), Aliases("sb")]
        [Description("cmd_sban")]
        [RequireBotPermissions(Permissions.BanMembers)]
        [RequireUserPermissions(Permissions.KickMembers)]
        [Priority(0)]
        public async Task SoftBanAsync(CommandContext context, DiscordMember user, [RemainingText] string reason = null)
            => await SoftBanAsync(context, user, null, reason);

        [Command("softban")]
        [Priority(1)]
        public async Task SoftBanAsync(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [Description("arg_ban_deletion")] TimeSpan? time = null,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // This returns null if it fails
            var dmMsg = await _punishService.SendPunishmentDmAsync(context, user, "sban_notification", reason);

            // Softban the user
            await _punishService.SoftbanUserAsync(context, user, (int)Math.Round(time?.TotalDays ?? 1), context.Member.GetFullname() + " | " + reason);

            // Send soft-ban message to the context channel
            var embed = _punishService.GetPunishEmbed(context, user, ":biohazard:", "sban_title");

            if (dmMsg is null && !user.IsBot)
                embed.WithFooter(AkkoStatics.WarningEmoji.Name + context.FormatLocalized("punishment_dm_failed"));

            await context.RespondLocalizedAsync(embed, false);
        }

        [HiddenOverload]
        [Command("ban"), Aliases("b")]
        [Description("cmd_ban")]
        [RequirePermissions(Permissions.BanMembers)]
        [Priority(1)]
        public async Task BanAsync(CommandContext context, DiscordMember user, [RemainingText] string reason = null)
            => await BanAsync(context, user, null, reason);

        [Command("ban")]
        [Priority(2)]
        public async Task BanAsync(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [Description("arg_ban_deletion")] TimeSpan? time = null,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // This returns null if it fails
            var dmMsg = await _punishService.SendBanDmAsync(context, user, reason);

            // Ban the user
            await _punishService.BanUserAsync(context, user, (int)Math.Round(time?.TotalDays ?? 1), context.Member.GetFullname() + " | " + reason);

            // Send ban message to the context channel
            var embed = _punishService.GetPunishEmbed(context, user, ":no_entry:", "ban_title");

            if (dmMsg is null && !user.IsBot)
                embed.WithFooter(AkkoStatics.WarningEmoji.Name + context.FormatLocalized("punishment_dm_failed"));

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("ban"), HiddenOverload]
        [Priority(0)]
        public async Task HackBanAsync(CommandContext context, DiscordUser user, [RemainingText] string reason = null)
        {
            // Ban the user - Don't register any occurrency
            await context.Guild.BanMemberAsync(user.Id, 1, context.Member.GetFullname() + " | " + reason);

            // Send ban message to the context channel
            var embed = _punishService.GetPunishEmbed(context, user, ":no_entry:", "hackban_title");
            await context.RespondLocalizedAsync(embed);
        }

        [Command("massban")]
        [Description("cmd_massban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task MassBanAsync(CommandContext context, [RemainingText, Description("arg_ulong_user_col")] params ulong[] userIds)
        {
            // Remove users that are already banned
            var nonBanned = userIds
                .Except((await context.Guild.GetBansAsync()).Select(banned => banned.User.Id))
                .ToArray();

            // Send the confirmation message
            var embed = new SerializableDiscordMessage()
                .WithTitle(":radioactive: " + context.FormatLocalized("massban"))
                .WithDescription(context.FormatLocalized("massban_description", nonBanned.Length))
                .WithFooter(context.FormatLocalized("q_operation_length_seconds", nonBanned.Length * AkkoStatics.SafetyDelay.TotalSeconds));

            var result = await context.RespondLocalizedAsync(embed, false);

            // Trigger typing so the user knows the bot is doing something
            await context.TriggerTypingAsync();

            // Process the bans. Have in mind that IDs might be invalid
            var fails = 0;
            foreach (var userId in nonBanned)
            {
                try { await _punishService.BanUserAsync(context, userId, 1, context.Member.GetFullname() + " | " + context.FormatLocalized("massban")); }
                catch { fails += 1; }

                // Safety delay
                await Task.Delay(AkkoStatics.SafetyDelay);
            }

            // Check if no user got banned
            embed.WithDescription(
                (fails == nonBanned.Length)
                    ? "massban_failure"
                    : context.FormatLocalized("massban_success", nonBanned.Length - fails)
            );

            await result.ModifyLocalizedAsync(context, embed, false, fails == nonBanned.Length);
        }

        [Command("timedban"), Aliases("tb")]
        [Description("cmd_timedban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task TimedBanAsync(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [Description("arg_timed_ban")] TimeSpan time,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (time <= TimeSpan.Zero)
            {
                await BanAsync(context, user, reason);
                return;
            }

            // Execute ban and send confirmation message
            await _punishService.SendPunishmentDmAsync(context, user, "timedban_notification", reason, time);

            // Perform the timed ban
            await _punishService.TimedBanAsync(context, time, user.Id, reason);

            // Send ban message to the context channel
            var embed = _punishService.GetPunishEmbed(context, user, ":no_entry:", "timedban_title")
                .WithFooter(
                    context.FormatLocalized("banned_for") + ": " +
                    context.FormatLocalized("days_hours_minutes", time.Days, time.Hours, time.Minutes)
                );

            await context.RespondLocalizedAsync(embed);
        }

        [Command("timedban"), HiddenOverload]
        public async Task TimedHackBanAsync(CommandContext context, DiscordUser user, TimeSpan time, [RemainingText] string reason = null)
        {
            // Perform the timed ban
            await _punishService.TimedBanAsync(context, time, user.Id, reason);

            // Send ban message to the context channel
            var embed = _punishService.GetPunishEmbed(context, user, ":no_entry:", "timedban_title")
                .WithFooter(
                    context.FormatLocalized("banned_for") + ": " +
                    context.FormatLocalized("days_hours_minutes", time.Days, time.Hours, time.Minutes)
                );

            await context.RespondLocalizedAsync(embed);
        }

        [Command("unban")]
        [Description("cmd_unban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task UnbanAsync(
            CommandContext context,
            [Description("arg_ulong_id")] ulong userId,
            [RemainingText, Description("arg_unpunishment_reason")] string reason = null)
        {
            // Get the user
            var user = (await context.Guild.GetBansAsync()).FirstOrDefault(u => u.User.Id == userId);

            if (user is null)
            {
                var embed = new SerializableDiscordMessage().WithDescription("unban_not_found");
                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                // Unban the user
                await context.Guild.UnbanMemberAsync(user.User, $"{context.User.GetFullname()} | {reason}");

                // Send unban message to the context channel
                var embed = new SerializableDiscordMessage()
                    .WithDescription(context.FormatLocalized("unban_success", Formatter.Bold(user.User.GetFullname())));

                await context.RespondLocalizedAsync(embed);
            }
        }

        [Command("massunban")]
        [Description("cmd_massunban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task MassUnbanAsync(CommandContext context, [RemainingText, Description("arg_ulong_user_col")] params ulong[] userIds)
        {
            var toUnban = (await context.Guild.GetBansAsync())
                .Select(banned => banned.User.Id)
                .Intersect(userIds)
                .ToArray();

            var failed = 0;
            if (toUnban.Length != 0)
            {
                await context.TriggerTypingAsync();

                foreach (var userId in toUnban)
                {
                    try { await context.Guild.UnbanMemberAsync(userId, $"{context.Member.GetFullname()} | {context.FormatLocalized("massunban")}"); }
                    catch { failed += 1; }

                    await Task.Delay(AkkoStatics.SafetyDelay);
                }
            }

            var embed = new SerializableDiscordMessage()
                .WithDescription(
                    (failed == toUnban.Length)
                        ? "massunban_failed"
                        : context.FormatLocalized("massunban_succeded", toUnban.Length - failed)
                );

            await context.RespondLocalizedAsync(embed, true, failed == toUnban.Length);
        }
    }
}