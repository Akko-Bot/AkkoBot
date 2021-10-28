using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Modules.Utilities.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Models.Serializable.EmbedParts;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Utilities
{
    [Group("voicerole"), Aliases("vcrole")]
    [Description("cmd_voicerole")]
    [RequireGuild, RequirePermissions(Permissions.ManageRoles | Permissions.ManageChannels)]
    public sealed class VoiceRoles : AkkoCommandModule
    {
        private readonly VoiceRoleService _service;

        public VoiceRoles(VoiceRoleService service)
            => _service = service;

        [Command("add")]
        [Description("cmd_voicerole_add")]
        public async Task AddVoiceRoleAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role, [Description("arg_discord_channel")] DiscordChannel? channel = default)
        {
            channel ??= context.Member.VoiceState?.Channel;
            var success = channel?.Type is ChannelType.Voice && await _service.AddVoiceRoleAsync(context.Guild, channel, role);

            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("remove"), Aliases("rm")]
        [Description("cmd_voicerole_remove")]
        public async Task RemoveVoiceRoleAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role, [Description("arg_discord_channel")] DiscordChannel? channel = default)
        {
            channel ??= context.Member.VoiceState?.Channel;
            var success = await _service.RemoveVoiceRoleAsync(context.Guild, x => x.ChannelId == channel?.Id && x.RoleId == role.Id);

            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("removeall"), Aliases("rmall")]
        [Description("cmd_voicerole_removeall")]
        public async Task RemoveAllVoiceRolesAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel? channel = default)
        {
            channel ??= context.Member.VoiceState?.Channel;
            var success = await _service.RemoveVoiceRoleAsync(context.Guild, x => x.ChannelId == channel?.Id);

            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_voicerole_list")]
        public async Task ListVoiceRolesAsync(CommandContext context)
        {
            var voiceRoles = await _service.GetVoiceRolesAsync(context.Guild);

            var embed = new SerializableDiscordEmbed();
            var fields = new List<SerializableEmbedField>();

            if (voiceRoles.Count == 0)
                embed.WithDescription("vcrole_empty");
            else
            {
                embed.WithTitle("vcrole_list_title");

                foreach (var channel in voiceRoles.Select(x => x.ChannelId).Distinct())
                    fields.Add(new SerializableEmbedField(context.Guild.GetChannel(channel).Name, string.Join("\n", voiceRoles.Where(x => x.ChannelId == channel).Select(x => context.Guild.GetRole(x.RoleId).Name).ToArray()), true));
            }

            await context.RespondPaginatedByFieldsAsync(embed, fields, 9);
        }
    }
}