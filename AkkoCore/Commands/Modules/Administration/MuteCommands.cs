using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration
{
    [RequireGuild]
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
        [RequireBotPermissions(Permissions.ManageRoles | Permissions.ManageChannels)]
        [Priority(0)]
        public async Task MuteAsync(CommandContext context, [Description("arg_discord_user")] DiscordMember user, [RemainingText, Description("arg_punishment_reason")] string reason)
            => await TimedMuteAsync(context, user, TimeSpan.FromHours(1), reason);

        [Command("mute")]
        [Priority(1)]
        public async Task TimedMuteAsync(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [Description("arg_timed_mute")] TimeSpan? time = null,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // Get the mute role
            var role = await _roleService.FetchMuteRoleAsync(context.Guild);
            await _channelServices.SetMuteOverwritesAsync(context.Guild, role, reason, false);

            // Apply it to the user
            await _roleService.MuteUserAsync(context, role, user, time ?? TimeSpan.FromHours(1), reason);

            if (user.VoiceState is not null)
                await user.SetMuteAsync(true);

            // Send confirmation message
            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized("mute_success", Formatter.Bold(user.GetFullname())));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("unmute")]
        [Description("cmd_unmute")]
        [RequirePermissions(Permissions.MuteMembers)]
        [RequireBotPermissions(Permissions.ManageRoles)]
        public async Task UnmuteAsync(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_unpunishment_reason")] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            // Get the mute role
            var role = await _roleService.FetchMuteRoleAsync(context.Guild);

            // Remove it from the user
            await _roleService.UnmuteUserAsync(context.Guild, role, user, reason);

            if (user.VoiceState is not null)
                await user.SetMuteAsync(false);

            // Send confirmation message
            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized("unmute_success", Formatter.Bold(user.GetFullname())));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("voicemute")]
        [Description("cmd_voicemute")]
        [RequirePermissions(Permissions.MuteMembers)]
        public async Task VoiceMuteAsync(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            var embed = await _roleService.SetVoiceMuteAsync(user, true, "voicemute_success", reason);
            embed.Body.Description = context.FormatLocalized(embed.Body.Description, Formatter.Bold(user.GetFullname()));

            await context.RespondLocalizedAsync(embed, isError: user.VoiceState is null);
        }

        [Command("voiceunmute")]
        [Description("cmd_voiceunmute")]
        [RequirePermissions(Permissions.MuteMembers)]
        public async Task VoiceUnmuteAsync(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_unpunishment_reason")] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            var embed = await _roleService.SetVoiceMuteAsync(user, false, "voiceunmute_success", reason);
            embed.Body.Description = context.FormatLocalized(embed.Body.Description, Formatter.Bold(user.GetFullname()));

            await context.RespondLocalizedAsync(embed, isError: user.VoiceState is null);
        }

        [Command("deafen"), Aliases("deaf")]
        [Description("cmd_deafen")]
        [RequirePermissions(Permissions.DeafenMembers)]
        public async Task DeafAsync(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            var embed = await _roleService.SetDeafAsync(user, true, "deafen_success", reason);
            embed.Body.Description = context.FormatLocalized(embed.Body.Description, Formatter.Bold(user.GetFullname()));

            await context.RespondLocalizedAsync(embed, isError: user.VoiceState is null);
        }

        [Command("undeafen"), Aliases("undeaf")]
        [Description("cmd_undeafen")]
        [RequirePermissions(Permissions.DeafenMembers)]
        public async Task UndeafAsync(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_unpunishment_reason")] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            var embed = await _roleService.SetDeafAsync(user, false, "undeafen_success", reason);
            embed.Body.Description = context.FormatLocalized(embed.Body.Description, Formatter.Bold(user.GetFullname()));

            await context.RespondLocalizedAsync(embed, isError: user.VoiceState is null);
        }

        [Command("chatmute")]
        [Description("cmd_chatmute")]
        [RequireUserPermissions(Permissions.MuteMembers)]
        [RequireBotPermissions(Permissions.ManageChannels)]
        public async Task ChatMuteAsync(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_punishment_reason")] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            await context.TriggerTypingAsync();
            await _channelServices.SetMuteOverwritesAsync(context.Guild, user, reason, true);

            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized("chatmute_success", Formatter.Bold(user.GetFullname())));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("chatunmute")]
        [Description("cmd_chatunmute")]
        [RequireUserPermissions(Permissions.MuteMembers)]
        [RequireBotPermissions(Permissions.ManageChannels)]
        public async Task ChatUnmuteAsync(
            CommandContext context,
            [Description("arg_discord_user")] DiscordMember user,
            [RemainingText, Description("arg_unpunishment_reason")] string reason = null)
        {
            if (!await _roleService.CheckHierarchyAsync(context, user, "error_hierarchy"))
                return;

            await context.TriggerTypingAsync();
            await _channelServices.RemoveOverwritesAsync(context.Guild, reason, x => x.Id == user.Id);

            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized("chatunmute_success", Formatter.Bold(user.GetFullname())));

            await context.RespondLocalizedAsync(embed);
        }
    }
}