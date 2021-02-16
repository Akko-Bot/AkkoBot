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
    public class MuteCommands : AkkoCommandModule
    {
        private readonly RoleService _roleService;
        private readonly ChannelService _channelServices;

        public MuteCommands(RoleService roleService, ChannelService channelServices)
        {
            _roleService = roleService;
            _channelServices = channelServices;
        }

        [Command("mute"), HiddenOverload]
        [Description("cmd_mute")]
        [RequirePermissions(Permissions.MuteMembers)]
        [RequireBotPermissions(Permissions.ManageRoles)]
        [Priority(0)]
        public async Task Mute(CommandContext context, [Description("arg_discord_user")] DiscordMember user, [RemainingText, Description("arg_punishment_reason")] string reason)
            => await TimedMute(context, user, TimeSpan.FromHours(1), reason);

        [Command("mute")]
        [Priority(1)]
        public async Task TimedMute(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [Description("arg_timed_mute")] TimeSpan? time = null,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.HierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            // Get the mute role
            var role = await _roleService.FetchMuteRoleAsync(context.Guild);
            await _channelServices.SetMuteOverwritesAsync(context.Guild, role, reason);

            // Apply it to the user
            await _roleService.MuteUserAsync(context, role, user, time ?? TimeSpan.FromHours(1), reason);

            if (user.VoiceState is not null)
                await user.SetMuteAsync(true);

            // Send confirmation message
            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("mute_success", Formatter.Bold(user.GetFullname())));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("unmute")]
        [Description("cmd_unmute")]
        [RequirePermissions(Permissions.MuteMembers)]
        [RequireBotPermissions(Permissions.ManageRoles)]
        public async Task Unmute(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_unpunishment_reason")] string reason = null)
        {
            if (!await _roleService.HierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            // Get the mute role
            var role = await _roleService.FetchMuteRoleAsync(context.Guild);

            // Remove it from the user
            await _roleService.UnmuteUserAsync(context.Guild, role, user, reason);

            if (user.VoiceState is not null)
                await user.SetMuteAsync(false);

            // Send confirmation message
            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("unmute_success", Formatter.Bold(user.GetFullname())));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("voicemute")]
        [Description("cmd_voicemute")]
        [RequirePermissions(Permissions.MuteMembers)]
        public async Task VoiceMute(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user, 
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.HierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            var embed = await _roleService.SetVoiceMuteAsync(user, true, "voicemute_success", reason);
            embed.Description = context.FormatLocalized(embed.Description, Formatter.Bold(user.GetFullname()));

            await context.RespondLocalizedAsync(embed, isError: user.VoiceState is null);
        }

        [Command("voiceunmute")]
        [Description("cmd_voiceunmute")]
        [RequirePermissions(Permissions.MuteMembers)]
        public async Task VoiceUnmute(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_unpunishment_reason")] string reason = null)
        {
            if (!await _roleService.HierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            var embed = await _roleService.SetVoiceMuteAsync(user, false, "voiceunmute_success", reason);
            embed.Description = context.FormatLocalized(embed.Description, Formatter.Bold(user.GetFullname()));

            await context.RespondLocalizedAsync(embed, isError: user.VoiceState is null);
        }

        [Command("deafen"), Aliases("deaf")]
        [Description("cmd_deafen")]
        [RequirePermissions(Permissions.DeafenMembers)]
        public async Task Deaf(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.HierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            var embed = await _roleService.SetDeafAsync(user, true, "deafen_success", reason);
            embed.Description = context.FormatLocalized(embed.Description, Formatter.Bold(user.GetFullname()));

            await context.RespondLocalizedAsync(embed, isError: user.VoiceState is null);
        }

        [Command("undeafen"), Aliases("undeaf")]
        [Description("cmd_undeafen")]
        [RequirePermissions(Permissions.DeafenMembers)]
        public async Task Undeaf(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_unpunishment_reason")] string reason = null)
        {
            if (!await _roleService.HierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            var embed = await _roleService.SetDeafAsync(user, false, "undeafen_success", reason);
            embed.Description = context.FormatLocalized(embed.Description, Formatter.Bold(user.GetFullname()));

            await context.RespondLocalizedAsync(embed, isError: user.VoiceState is null);
        }

        [Command("chatmute")]
        [Description("cmd_chatmute")]
        [RequireUserPermissions(Permissions.MuteMembers)]
        [RequireBotPermissions(Permissions.ManageChannels)]
        public async Task ChatMute(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.HierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            await _channelServices.SetMuteOverwritesAsync(context.Guild, user, reason);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("chatmute_success", Formatter.Bold(user.GetFullname())));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("chatunmute")]
        [Description("cmd_chatunmute")]
        [RequireUserPermissions(Permissions.MuteMembers)]
        [RequireBotPermissions(Permissions.ManageChannels)]
        public async Task ChatUnmute(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_unpunishment_reason")] string reason = null)
        {
            if (!await _roleService.HierarchyCheckAsync(context, user, "error_hierarchy"))
                return;

            await _channelServices.RemoveOverwritesAsync(context.Guild, reason, x => x.Id == user.Id);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("chatunmute_success", Formatter.Bold(user.GetFullname())));

            await context.RespondLocalizedAsync(embed);
        }
    }
}