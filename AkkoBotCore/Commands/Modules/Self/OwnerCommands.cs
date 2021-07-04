using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Commands.Common;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Models;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace AkkoBot.Commands.Modules.Self
{
    [BotOwner]
    public class OwnerCommands : AkkoCommandModule
    {
        private readonly UtilitiesService _utilities;
        private readonly ILocalizer _localizer;

        public OwnerCommands(UtilitiesService utilities, ILocalizer localizer)
        {
            _utilities = utilities;
            _localizer = localizer;
        }

        [Command("senddirectmessage"), Aliases("senddm")]
        [Description("cmd_senddm")]
        public async Task SendMessageAsync(CommandContext context, [Description("arg_discord_user")] DiscordUser user, [RemainingText, Description("arg_say")] SmartString message)
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

            var dm = (_utilities.DeserializeEmbed(message, out var dMsg))
                ? await member.SendMessageSafelyAsync(dMsg)
                : await member.SendMessageSafelyAsync(message);

            await context.Message.CreateReactionAsync((dm is not null) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("sendmessage"), Aliases("send")]
        [Description("cmd_send")]
        public async Task SendMessageAsync(CommandContext context, [Description("arg_channel_id")] ulong cid, [RemainingText, Description("arg_say")] SmartString message)
        {
            var server = context.Services.GetService<DiscordShardedClient>().ShardClients.Values
                .SelectMany(x => x.Guilds.Values)
                .FirstOrDefault(x => x.Channels.ContainsKey(cid));

            if (server is null || !server.Channels.TryGetValue(cid, out var channel) || channel.Type is ChannelType.Voice)
            {
                await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
                return;
            }

            var dm = (_utilities.DeserializeEmbed(message, out var dMsg))
                ? await channel.SendMessageSafelyAsync(dMsg)
                : await channel.SendMessageSafelyAsync(message);

            await context.Message.CreateReactionAsync((dm is not null) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("exportcommands"), Aliases("exportcmds")]
        [Description("cmd_exportcommands")]
        public async Task ExportCommandsAsync(CommandContext context, [Description("arg_exportcommand_format")] string format = "yaml")
        {
            var locale = context.GetLocaleKey();
            var result = context.CommandsNext.RegisteredCommands.Values
                .Distinct()
                .Where(x => !x.CustomAttributes.Any(y => y.GetType() == typeof(HiddenOverloadAttribute)))
                .Select(x => new SerializableCommand(_localizer, x, locale))
                .OrderBy(x => x.Name)
                .ToArray();

            var isJson = format.Equals("json", StringComparison.InvariantCultureIgnoreCase);
            var text = (isJson)
                ? JsonConvert.SerializeObject(result, Formatting.Indented)
                : result.ToYaml(new Serializer());

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

            var message = new DiscordMessageBuilder()
                .WithContent(context.FormatLocalized("exportcommands_description", Formatter.Bold(result.Length.ToString())))
                .WithFile($"commands.{((isJson) ? "json" : "yaml")}", stream);

            await context.Channel.SendMessageAsync(message);
        }
    }
}