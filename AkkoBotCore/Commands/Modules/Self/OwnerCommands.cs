using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Commands.Common;
using AkkoBot.Commands.Modules.Self.Services;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Self
{
    [BotOwner]
    public class OwnerCommands : AkkoCommandModule
    {
        private readonly UtilitiesService _utilities;

        public OwnerCommands(UtilitiesService utilities)
            => _utilities = utilities;

        [Command("senddirectmessage"), Aliases("senddm")]
        [Description("cmd_senddm")]
        public async Task SendMessage(CommandContext context, [Description("arg_discord_user")] DiscordUser user, [RemainingText, Description("arg_say")] SmartString message)
        {
            var server = context.Services.GetService<DiscordShardedClient>().ShardClients.Values
                .SelectMany(x => x.Guilds.Values)
                .FirstOrDefault(x => x.Members.ContainsKey(user.Id));

            var member = await server?.GetMemberSafelyAsync(user.Id);

            if (server is null || member is null)
            {
                await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
                return;
            }

            var dm = (_utilities.DeserializeEmbed(message.Content, out var dMsg))
                ? await member.SendMessageSafelyAsync(dMsg)
                : await member.SendMessageSafelyAsync(message.Content);

            await context.Message.CreateReactionAsync((dm is not null) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("sendmessage"), Aliases("send")]
        [Description("cmd_send")]
        public async Task SendMessage(CommandContext context, [Description("arg_channel_id")] ulong cid, [RemainingText, Description("arg_say")] SmartString message)
        {
            var server = context.Services.GetService<DiscordShardedClient>().ShardClients.Values
                .SelectMany(x => x.Guilds.Values)
                .FirstOrDefault(x => x.Channels.ContainsKey(cid));

            if (server is null || !server.Channels.TryGetValue(cid, out var channel) || channel.Type is ChannelType.Voice)
            {
                await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
                return;
            }

            var dm = (_utilities.DeserializeEmbed(message.Content, out var dMsg))
                ? await channel.SendMessageSafelyAsync(dMsg)
                : await channel.SendMessageSafelyAsync(message.Content);

            await context.Message.CreateReactionAsync((dm is not null) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }
    }
}