using AkkoBot.Command.Abstractions;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using AkkoBot.Command.Modules.Administration.Services;
using System.Linq;

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
        public async Task Kick(CommandContext context, DiscordMember user, string reason = null)
        {
            if (!await _roleservice.CheckHierarchyAsync(context, user, "kick_error"))
                return;

            // Create the notification dm
            var dm = new DiscordEmbedBuilder()
                .WithDescription(
                    context.FormatLocalized("kick_notification", context.Guild.Name)
                )
                .AddField(
                    context.FormatLocalized("reason"),
                    reason
                );

            // This returns null if it fails
            var dmSuccess = await context.SendLocalizedDmAsync(user, dm);

            // Kick the user
            await user.RemoveAsync(context.Member.GetFullname() + " | " + reason);

            // Send kick message to the context channel
            var result = new DiscordEmbedBuilder()
                .WithTitle("kick_title")
                .WithDescription("kick_description");

            if (dmSuccess is null)
                result.WithFooter("kick_dm_failed");

            await context.RespondLocalizedAsync(result, false);
        }

        [Command("softban"), Aliases("sb")]
        [Description("cmd_sban")]
        [RequireBotPermissions(Permissions.BanMembers)]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task SoftBan(CommandContext context, DiscordMember user, string reason = null)
        {
            if (!await _roleservice.CheckHierarchyAsync(context, user, "sban_error"))
                return;

            // Create the notification dm
            var dm = new DiscordEmbedBuilder()
                .WithDescription(
                    context.FormatLocalized("sban_notification", context.Guild.Name)
                )
                .AddField(
                    context.FormatLocalized("reason"),
                    reason
                );

            // This returns null if it fails
            var dmSuccess = await context.SendLocalizedDmAsync(user, dm);

            // Ban the user
            await user.BanAsync(1, context.Member.GetFullname() + " | " + reason);

            // Unban the user
            await context.Guild.UnbanMemberAsync(user);

            // Send ban message to the context channel
            var result = new DiscordEmbedBuilder()
                .WithTitle("sban_title")
                .WithDescription("sban_description");

            if (dmSuccess is null)
                result.WithFooter("sban_dm_failed");

            await context.RespondLocalizedAsync(result, false);
        }

        [Command("ban"), Aliases("b")]
        [Description("cmd_ban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task Ban(CommandContext context, DiscordMember user, int messageDays = 1, string reason = null)
        {
            if (!await _roleservice.CheckHierarchyAsync(context, user, "ban_error"))
                return;

            // Create the notification dm
            var dm = new DiscordEmbedBuilder()
                .WithDescription(
                    context.FormatLocalized("ban_notification", context.Guild.Name)
                )
                .AddField(
                    context.FormatLocalized("reason"),
                    reason
                );

            // This returns null if it fails
            var dmSuccess = await context.SendLocalizedDmAsync(user, dm);

            // Ban the user
            await user.BanAsync(messageDays, context.Member.GetFullname() + " | " + reason);

            // Send ban message to the context channel
            var result = new DiscordEmbedBuilder()
                .WithTitle("ban_title")
                .WithDescription("ban_description");

            if (dmSuccess is null)
                result.WithFooter("ban_dm_failed");

            await context.RespondLocalizedAsync(result, false);
        }

        [Command("ban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task HackBan(CommandContext context, ulong userId, string reason = null)
        {
            // Ban the user
            await context.Guild.BanMemberAsync(userId, 1, context.Member.GetFullname() + " | " + reason);

            // Send ban message to the context channel
            var result = new DiscordEmbedBuilder()
                .WithDescription("ban_hackban");

            await context.RespondLocalizedAsync(result);
        }

        [Command("unban")]
        [Description("cmd_unban")]
        [RequirePermissions(Permissions.BanMembers)]
        public async Task Unban(CommandContext context, ulong userId, string reason = null)
        {
            // Get the user
            var user = (await context.Guild.GetBansAsync()).FirstOrDefault(u => u.User.Id == userId);

            if (user is null)
            {
                var result = new DiscordEmbedBuilder()
                    .WithDescription("unban_not_found");

                await context.RespondLocalizedAsync(result, isError: true);
            }
            else
            {
                // Unban the user
                await context.Guild.UnbanMemberAsync(user.User, reason);

                // Send unban message to the context channel
                var result = new DiscordEmbedBuilder()
                    .WithDescription("unban_success");

                await context.RespondLocalizedAsync(result);
            }
        }
    }
}