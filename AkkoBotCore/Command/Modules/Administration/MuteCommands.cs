using AkkoBot.Command.Abstractions;
using AkkoBot.Command.Modules.Administration.Services;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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

        [Command("mute")]
        [Description("cmd_mute")]
        public async Task Mute(CommandContext context, DiscordMember user, string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // Get the mute role
            var role = await _roleService.GetMuteRole(context.Guild);
            await _roleService.SetMuteOverwrites(context.Guild, role);

            // Apply it to the user
            await _roleService.PermaMuteUser(context.Guild, role, user, reason);

            // Send confirmation message
            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("mute_success", user.GetFullname()));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("unmute")]
        [Description("cmd_unmute")]
        public async Task Unmute(CommandContext context, DiscordMember user, string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // Get the mute role
            var role = await _roleService.GetMuteRole(context.Guild);

            // Remove it from the user
            await _roleService.UnmuteUser(context.Guild, role, user, reason);

            // Send confirmation message
            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("unmute_success", user.GetFullname()));

            await context.RespondLocalizedAsync(embed);
        }
    }
}