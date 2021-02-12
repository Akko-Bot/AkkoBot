using AkkoBot.Command.Abstractions;
using AkkoBot.Command.Attributes;
using AkkoBot.Command.Modules.Administration.Services;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Command.Modules.Administration
{
    [RequirePermissions(Permissions.MuteMembers)]
    [RequireBotPermissions(Permissions.ManageRoles)]
    public class MuteCommands : AkkoCommandModule
    {
        private readonly RoleService _roleService;

        public MuteCommands(RoleService roleService)
            => _roleService = roleService;

        [Command("mute"), HiddenOverload]
        [Description("cmd_mute")]
        [Priority(0)]
        public async Task Mute(CommandContext context, DiscordMember user, [RemainingText] string reason)
            => await TimedMute(context, user, TimeSpan.FromHours(1), reason);

        [Command("mute")]
        [Priority(1)]
        public async Task TimedMute(CommandContext context, DiscordMember user, TimeSpan? time = null, [RemainingText] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // Get the mute role
            var role = await _roleService.FetchMuteRole(context.Guild);
            await _roleService.SetMuteOverwrites(context.Guild, role);

            // Apply it to the user
            await _roleService.MuteUser(context, role, user, time ?? TimeSpan.FromHours(1), reason);

            // Send confirmation message
            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("mute_success", Formatter.Bold(user.GetFullname())));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("unmute")]
        [Description("cmd_unmute")]
        public async Task Unmute(CommandContext context, DiscordMember user, [RemainingText] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // Get the mute role
            var role = await _roleService.FetchMuteRole(context.Guild);

            // Remove it from the user
            await _roleService.UnmuteUser(context.Guild, role, user, reason);

            // Send confirmation message
            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("unmute_success", Formatter.Bold(user.GetFullname())));

            await context.RespondLocalizedAsync(embed);
        }
    }
}