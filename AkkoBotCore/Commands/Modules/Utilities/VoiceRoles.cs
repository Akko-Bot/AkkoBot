using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities
{
    [Group("voicerole"), Aliases("vcrole")]
    [Description("cmd_voicerole")]
    [RequireGuild, RequirePermissions(Permissions.ManageRoles | Permissions.ManageChannels)]
    public class VoiceRoles : AkkoCommandModule
    {
        private readonly VoiceRoleService _service;

        public VoiceRoles(VoiceRoleService service)
            => _service = service;

        [Command("add")]
        [Description("cmd_voicerole_add")]
        public async Task AddVoiceRole(CommandContext context, [Description("arg_discord_role")] DiscordRole role, [Description("arg_discord_channel")] DiscordChannel channel = null)
        {
            channel ??= context.Member.VoiceState?.Channel;
            var success = channel?.Type is ChannelType.Voice && await _service.AddVoiceRoleAsync(context.Guild, channel, role);

            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("remove"), Aliases("rm")]
        [Description("cmd_voicerole_remove")]
        public async Task RemoveVoiceRole(CommandContext context, [Description("arg_discord_role")] DiscordRole role, [Description("arg_discord_channel")] DiscordChannel channel = null)
        {
            channel ??= context.Member.VoiceState?.Channel;
            var success = await _service.RemoveVoiceRoleAsync(context.Guild, x => x.ChannelId == channel.Id && x.RoleId == role.Id);

            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("removeall"), Aliases("rmall")]
        [Description("cmd_voicerole_removeall")]
        public async Task RemoveAllVoiceRoles(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
        {
            channel ??= context.Member.VoiceState?.Channel;
            var success = await _service.RemoveVoiceRoleAsync(context.Guild, x => x.ChannelId == channel.Id);

            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_voicerole_list")]
        public async Task ListVoiceRoles(CommandContext context)
        {
            var voiceRoles = await _service.GetVoiceRolesAsync(context.Guild);

            var embed = new DiscordEmbedBuilder();
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