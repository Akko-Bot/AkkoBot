using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Commands.Common;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities
{
    [RequireGuild]
    public class GuildUtilities : AkkoCommandModule
    {
        private readonly UtilitiesService _service;
        private readonly DiscordShardedClient _clients;

        public GuildUtilities(UtilitiesService service, DiscordShardedClient clients)
        {
            _service = service;
            _clients = clients;
        }

        [Command("say"), HiddenOverload]
        [Priority(0)]
        public async Task Say(CommandContext context, [RemainingText] SmartString message)
            => await Say(context, context.Channel, message);

        [Command("say")]
        [Description("cmd_say")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Priority(1)]
        public async Task Say(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel, [RemainingText, Description("arg_say")] SmartString message)
        {
            if (string.IsNullOrWhiteSpace(message?.Content))    // If command only contains a channel name
                await context.RespondAsync(channel.Name);
            else if (_service.DeserializeEmbed(message.Content, out var parsedMessage)) // If command contains an embed in yaml format
                await channel.SendMessageAsync(parsedMessage);
            else    // If command is just plain text
                await channel.SendMessageAsync(message.Content);
        }

        [Command("serverinfo"), Aliases("sinfo")]
        [Description("cmd_serverinfo")]
        public async Task ServerInfo(CommandContext context)
            => await context.RespondLocalizedAsync(_service.GetServerInfo(context, context.Guild), false);

        [Command("serverinfo"), HiddenOverload]
        public async Task ServerInfo(CommandContext context, DiscordGuild server)
        {
            if (!GeneralService.IsOwner(context, context.Member.Id) || server.Channels.Count == 0)
                return;

            await context.RespondLocalizedAsync(_service.GetServerInfo(context, server), false);
        }

        [Command("channelinfo"), Aliases("cinfo")]
        [Description("cmd_channelinfo")]
        public async Task ChannelInfo(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
        {
            channel ??= context.Channel;

            var embed = _service.GetChannelInfo(new DiscordEmbedBuilder(), channel)
                .WithFooter(context.FormatLocalized("{0}: {1}", "created_at", channel.CreationTimestamp.ToString("d")));

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("userinfo"), Aliases("uinfo")]
        [Description("cmd_userinfo")]
        public async Task UserInfo(CommandContext context, [Description("arg_discord_user")] DiscordMember user = null)
        {
            user ??= context.Member;
            var isMod = user.Hierarchy is int.MaxValue || user.Roles.Any(role => role.Permissions.HasOneFlag(Permissions.Administrator | Permissions.KickMembers | Permissions.BanMembers));

            var embed = new DiscordEmbedBuilder()
                .WithThumbnail(user.AvatarUrl ?? user.DefaultAvatarUrl)
                .AddField("name", user.GetFullname(), true)
                .AddField("nickname", user.Nickname ?? "-", true)
                .AddField("id", user.Id.ToString(), true)
                .AddField("is_mod", (isMod) ? AkkoEntities.SuccessEmoji.Name : AkkoEntities.FailureEmoji.Name, true)
                .AddField("roles", user.Roles.Count().ToString(), true)
                .AddField("position", user.Hierarchy.ToString(), true)
                .AddField("created_at", user.CreationTimestamp.DateTime.ToString(), true)
                .AddField("joined_at", user.JoinedAt.DateTime.ToString(), true);

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("userinfo"), HiddenOverload]
        public async Task UserInfo(CommandContext context, DiscordUser user)
        {
            var embed = new DiscordEmbedBuilder()
                .WithThumbnail(user.AvatarUrl ?? user.DefaultAvatarUrl)
                .AddField("name", user.GetFullname(), true)
                .AddField("id", user.Id.ToString(), true)
                .AddField("created_at", user.CreationTimestamp.DateTime.ToString(), false);

            await context.RespondLocalizedAsync(embed, false);
        }
    }
}