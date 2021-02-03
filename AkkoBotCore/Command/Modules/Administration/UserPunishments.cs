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
        public async Task Kick(CommandContext context, [Description("arg_discord_user")] DiscordMember user, [Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleservice.CheckHierarchyAsync(context, user, "kick_error"))
                return;

            // Create the notification dm
            var dm = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("kick_notification", context.Guild.Name));

            if (reason is not null)
                dm.AddField(context.FormatLocalized("reason"), reason);

            // This returns null if it fails
            var dmSuccess = await context.SendLocalizedDmAsync(user, dm);

            // Kick the user
            await user.RemoveAsync(context.Member.GetFullname() + " | " + reason);

            // Send kick message to the context channel
            var result = new DiscordEmbedBuilder()
                .WithTitle("kick_title")
                .WithDescription("kick_description");

            if (dmSuccess is null && !user.IsBot)
                result.WithFooter("kick_dm_failed");

            await context.RespondLocalizedAsync(result, false);
        }

        [Command("softban"), Aliases("sb")]
        [Description("cmd_sban")]
        [RequireBotPermissions(Permissions.BanMembers)]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task SoftBan(CommandContext context, [HiddenOverload] DiscordMember user, string reason = null)
            => await SoftBan(context, user, null, reason);

        [Command("softban")]
        public async Task SoftBan(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [Description("arg_ban_deletion")] TimeSpan? time = null,
            [Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleservice.CheckHierarchyAsync(context, user, "sban_error"))
                return;

            // Create the notification dm
            var dm = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("sban_notification", context.Guild.Name));

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
                .WithTitle("sban_title")
                .WithDescription("sban_description");

            if (dmSuccess is null && !user.IsBot)
                result.WithFooter("ban_dm_failed");

            await context.RespondLocalizedAsync(result, false);
        }

        [Command("ban"), Aliases("b")]
        [Description("cmd_ban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task Ban(CommandContext context, [HiddenOverload] DiscordMember user, string reason = null)
            => await Ban(context, user, null, reason);

        [Command("ban")]
        public async Task Ban(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [Description("arg_ban_deletion")] TimeSpan? time = null,
            [Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleservice.CheckHierarchyAsync(context, user, "ban_error"))
                return;

            // Create the notification dm
            var dm = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("ban_notification", context.Guild.Name));

            if (reason is not null)
                dm.AddField(context.FormatLocalized("reason"), reason);

            // This returns null if it fails
            var dmSuccess = await context.SendLocalizedDmAsync(user, dm);

            // Ban the user
            await context.Guild.BanMemberAsync(user.Id, (int)Math.Round(time?.TotalDays ?? 1), context.Member.GetFullname() + " | " + reason);

            // Send ban message to the context channel
            var result = new DiscordEmbedBuilder()
                .WithTitle("ban_title")
                .WithDescription("ban_description");

            if (dmSuccess is null && !user.IsBot)
                result.WithFooter("ban_dm_failed");

            await context.RespondLocalizedAsync(result, false);
        }

        [Command("ban")]
        public async Task HackBan(CommandContext context, [HiddenOverload] ulong userId, string reason = null)
        {
            // Ban the user
            await context.Guild.BanMemberAsync(userId, 1, context.Member.GetFullname() + " | " + reason);

            // Send ban message to the context channel
            var embed = new DiscordEmbedBuilder()
                .WithDescription("ban_hackban");

            await context.RespondLocalizedAsync(embed);
        }

        // public async Task TimedBan(CommandContext context, DiscordMember user, TimeSpan time, string reason = null)
        // {
        //     //
        // }

        [Command("unban")]
        [Description("cmd_unban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task Unban(CommandContext context, ulong userId, string reason = null)
        {
            // Get the user
            var user = (await context.Guild.GetBansAsync()).FirstOrDefault(u => u.User.Id == userId);

            if (user is null)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithDescription("unban_not_found");

                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                // Unban the user
                await context.Guild.UnbanMemberAsync(user.User, reason);

                // Send unban message to the context channel
                var embed = new DiscordEmbedBuilder()
                    .WithDescription(context.FormatLocalized("unban_success", user.User.GetFullname()));

                await context.RespondLocalizedAsync(embed);
            }
        }
    }
}