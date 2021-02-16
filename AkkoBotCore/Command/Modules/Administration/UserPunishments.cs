using System;
using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using AkkoBot.Command.Modules.Administration.Services;
using System.Linq;
using AkkoBot.Command.Attributes;

namespace AkkoBot.Command.Modules.Administration
{
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
        public async Task Kick(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.HierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            // This returns null if it fails
            var dmMsg = _punishService.SendPunishmentDm(context, user, "kick_notification", reason);

            // Kick the user
            await _punishService.KickUser(context.Guild, user, context.Member.GetFullname() + " | " + reason);

            // Send kick message to the context channel
            var embed = _punishService.GetPunishEmbed(context, user, string.Empty, "kick_title");

            if (dmMsg is null && !user.IsBot)
                embed.WithFooter("punishment_dm_failed");

            await context.RespondLocalizedAsync(embed, false);
        }

        [HiddenOverload]
        [Command("softban"), Aliases("sb")]
        [Description("cmd_sban")]
        [RequireBotPermissions(Permissions.BanMembers)]
        [RequireUserPermissions(Permissions.KickMembers)]
        [Priority(0)]
        public async Task SoftBan(CommandContext context, DiscordMember user, [RemainingText] string reason = null)
            => await SoftBan(context, user, null, reason);

        [Command("softban")]
        [Priority(1)]
        public async Task SoftBan(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [Description("arg_ban_deletion")] TimeSpan? time = null,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.HierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            // This returns null if it fails
            var dmMsg = _punishService.SendPunishmentDm(context, user, "sban_notification", reason);

            // Softban the user
            await _punishService.SoftbanUser(context.Guild, user, (int)Math.Round(time?.TotalDays ?? 1), context.Member.GetFullname() + " | " + reason);

            // Send soft-ban message to the context channel
            var embed = _punishService.GetPunishEmbed(context, user, ":biohazard:", "sban_title");

            if (dmMsg is null && !user.IsBot)
                embed.WithFooter("punishment_dm_failed");

            await context.RespondLocalizedAsync(embed, false);
        }

        [HiddenOverload]
        [Command("ban"), Aliases("b")]
        [Description("cmd_ban")]
        [RequirePermissions(Permissions.BanMembers)]
        [Priority(1)]
        public async Task Ban(CommandContext context, DiscordMember user, [RemainingText] string reason = null)
            => await Ban(context, user, null, reason);

        [Command("ban")]
        [Priority(2)]
        public async Task Ban(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [Description("arg_ban_deletion")] TimeSpan? time = null,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.HierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            // This returns null if it fails
            var dmMsg = _punishService.SendPunishmentDm(context, user, "ban_notification", reason);

            // Ban the user
            await _punishService.BanUser(context.Guild, user, (int)Math.Round(time?.TotalDays ?? 1), context.Member.GetFullname() + " | " + reason);

            // Send ban message to the context channel
            var embed = _punishService.GetPunishEmbed(context, user, ":no_entry:", "ban_title");

            if (dmMsg is null && !user.IsBot)
                embed.WithFooter("punishment_dm_failed");

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("ban"), HiddenOverload]
        [Priority(0)]
        public async Task HackBan(CommandContext context, DiscordUser user, [RemainingText] string reason = null)
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
        public async Task MassBan(CommandContext context, [RemainingText, Description("arg_ulong_user_col")] params ulong[] userIds)
        {
            // Remove users that are already banned
            var nonBanned = userIds
                .Except((await context.Guild.GetBansAsync()).Select(banned => banned.User.Id))
                .ToArray();

            // Send the confirmation message
            var embed = new DiscordEmbedBuilder()
                .WithTitle(":radioactive: " + context.FormatLocalized("massban"))
                .WithDescription(context.FormatLocalized("massban_description", nonBanned.Length, nonBanned.Length * 0.5));

            var result = await context.RespondLocalizedAsync(embed, false);

            // Trigger typing so the user knows the bot is doing something
            await context.TriggerTypingAsync();

            // Process the bans. Have in mind that IDs might be invalid
            var fails = 0;
            foreach (var userId in nonBanned)
            {
                try { await _punishService.BanUser(context.Guild, userId, 1, context.Member.GetFullname() + " | " + context.FormatLocalized("massban")); }
                catch { fails += 1; }

                // Safety delay
                await Task.Delay(TimeSpan.FromSeconds(0.5));
            }

            // Check if no user got banned
            if (fails == nonBanned.Length)
            {
                embed.Description = context.FormatLocalized("massban_failure");
                await result.ModifyLocalizedAsync(context, embed, false, true);
            }
            else
            {
                embed.Description = context.FormatLocalized("massban_success", nonBanned.Length - fails);
                await result.ModifyLocalizedAsync(context, embed, false);
            }
        }

        [Command("timedban"), Aliases("tb")]
        [Description("cmd_timedban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task TimedBan(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [Description("arg_timed_ban")] TimeSpan time,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (time <= TimeSpan.Zero)
            {
                await Ban(context, user, reason);
                return;
            }

            // Execute ban and send confirmation message
            await _punishService.SendPunishmentDm(context, user, "ban_notification", reason);

            // Perform the timed ban
            await _punishService.TimedBan(context, time, user.Id, reason);

            // Send ban message to the context channel
            var embed = _punishService.GetPunishEmbed(context, user, ":no_entry:", "timedban_title")
                .WithFooter(
                    context.FormatLocalized("banned_for") + ": " +
                    context.FormatLocalized("days_hours_minutes", time.Days, time.Hours, time.Minutes)
                );

            await context.RespondLocalizedAsync(embed);
        }

        [Command("timedban"), HiddenOverload]
        public async Task TimedHackBan(CommandContext context, DiscordUser user, TimeSpan time, [RemainingText] string reason = null)
        {
            // Perform the timed ban
            await _punishService.TimedBan(context, time, user.Id, reason);

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
        public async Task Unban(
            CommandContext context,
            [Description("arg_ulong_id")] ulong userId,
            [RemainingText, Description("arg_unpunishment_reason")] string reason = null)
        {
            // Get the user
            var user = (await context.Guild.GetBansAsync()).FirstOrDefault(u => u.User.Id == userId);

            if (user is null)
            {
                var embed = new DiscordEmbedBuilder().WithDescription("unban_not_found");
                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                // Unban the user
                await context.Guild.UnbanMemberAsync(user.User, reason);

                // Send unban message to the context channel
                var embed = new DiscordEmbedBuilder()
                    .WithDescription(context.FormatLocalized("unban_success", Formatter.Bold(user.User.GetFullname())));

                await context.RespondLocalizedAsync(embed);
            }
        }

        [Command("massunban")]
        [Description("cmd_massunban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task MassUnban(CommandContext context, [RemainingText, Description("arg_ulong_user_col")] params ulong[] userIds)
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
                    try { await context.Guild.UnbanMemberAsync(userId, context.Member.GetFullname() + " | " + context.FormatLocalized("massunban")); }
                    catch { failed += 1; }

                    await Task.Delay(TimeSpan.FromSeconds(0.5));
                }
            }

            var embed = new DiscordEmbedBuilder();

            if (failed == toUnban.Length)
            {
                embed.Description = "massunban_failed";
                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                embed.Description = context.FormatLocalized("massunban_succeded", toUnban.Length - failed);
                await context.RespondLocalizedAsync(embed);
            }
        }
    }
}