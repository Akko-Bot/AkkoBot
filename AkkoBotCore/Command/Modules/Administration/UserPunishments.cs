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
    public class UserPunishments : AkkoCommandModule
    {
        private readonly RoleService _roleservice;

        public UserPunishments(RoleService roleservice)
            => _roleservice = roleservice;

        [Command("kick"), Aliases("k")]
        [Description("cmd_kick")]
        [RequirePermissions(Permissions.KickMembers)]
        public async Task Kick(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleservice.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // Create the notification dm
            var dm = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("kick_notification", Formatter.Bold(context.Guild.Name)));

            if (reason is not null)
                dm.AddField(context.FormatLocalized("reason"), reason);

            // This returns null if it fails
            var dmSuccess = await context.SendLocalizedDmAsync(user, dm);

            // Kick the user
            await user.RemoveAsync(context.Member.GetFullname() + " | " + reason);

            // Send kick message to the context channel
            var result = new DiscordEmbedBuilder()
                .WithTitle("kick_title")
                .AddField("user", user.GetFullname(), true)
                .AddField("id", user.Id.ToString(), true);

            if (dmSuccess is null && !user.IsBot)
                result.WithFooter("punishment_dm_failed");

            await context.RespondLocalizedAsync(result, false);
        }

        [HiddenOverload]
        [Command("softban"), Aliases("sb")]
        [Description("cmd_sban")]
        [RequireBotPermissions(Permissions.BanMembers)]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task SoftBan(CommandContext context, DiscordMember user, [RemainingText] string reason = null)
            => await SoftBan(context, user, null, reason);

        [Command("softban")]
        public async Task SoftBan(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [Description("arg_ban_deletion")] TimeSpan? time = null,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleservice.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // Create the notification dm
            var dm = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("sban_notification", Formatter.Bold(context.Guild.Name)));

            if (reason is not null)
                dm.AddField(context.FormatLocalized("reason"), reason);

            // This returns null if it fails
            var dmSuccess = await context.SendLocalizedDmAsync(user, dm);

            // Ban the user
            await user.BanAsync((int)Math.Round(time?.TotalDays ?? 1), context.Member.GetFullname() + " | " + reason);

            // Unban the user
            await context.Guild.UnbanMemberAsync(user);

            // Send ban message to the context channel
            var result = new DiscordEmbedBuilder()
                .WithTitle(":biohazard: " + context.FormatLocalized("sban_title"))
                .AddField("user", user.GetFullname(), true)
                .AddField("id", user.Id.ToString(), true);

            if (dmSuccess is null && !user.IsBot)
                result.WithFooter("punishment_dm_failed");

            await context.RespondLocalizedAsync(result, false);
        }

        [HiddenOverload]
        [Command("ban"), Aliases("b")]
        [Description("cmd_ban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task Ban(CommandContext context, DiscordMember user, [RemainingText] string reason = null)
            => await Ban(context, user, null, reason);

        [Command("ban")]
        public async Task Ban(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [Description("arg_ban_deletion")] TimeSpan? time = null,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleservice.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // Create the notification dm
            var dm = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("ban_notification", Formatter.Bold(context.Guild.Name)));

            if (reason is not null)
                dm.AddField(context.FormatLocalized("reason"), reason);

            // This returns null if it fails
            var dmSuccess = await context.SendLocalizedDmAsync(user, dm);

            // Ban the user
            await context.Guild.BanMemberAsync(user.Id, (int)Math.Round(time?.TotalDays ?? 1), context.Member.GetFullname() + " | " + reason);

            // Send ban message to the context channel
            var result = new DiscordEmbedBuilder()
                .WithTitle("⛔️ " + context.FormatLocalized("ban_title"))
                .AddField("user", user.GetFullname(), true)
                .AddField("id", user.Id.ToString(), true);

            if (dmSuccess is null && !user.IsBot)
                result.WithFooter("punishment_dm_failed");

            await context.RespondLocalizedAsync(result, false);
        }

        [Command("ban"), HiddenOverload]
        public async Task HackBan(CommandContext context, DiscordUser user, [RemainingText] string reason = null)
        {
            // Ban the user
            await context.Guild.BanMemberAsync(user.Id, 1, context.Member.GetFullname() + " | " + reason);

            // Send ban message to the context channel
            var embed = new DiscordEmbedBuilder()
                .WithTitle("⛔️ " + context.FormatLocalized("hackban_title"))
                .AddField("user", user.GetFullname(), true)
                .AddField("id", user.Id.ToString(), true);

            await context.RespondLocalizedAsync(embed);
        }

        // public async Task TimedBan(CommandContext context, DiscordMember user, TimeSpan time, string reason = null)
        // {
        //     //
        // }

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
    }
}