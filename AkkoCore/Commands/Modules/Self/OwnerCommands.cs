using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Common;
using AkkoCore.Commands.Modules.Utilities.Services;
using AkkoCore.Common;
using AkkoCore.Core.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Models.Serializable.EmbedParts;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kotz.Collections.Extensions;
using Kotz.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace AkkoCore.Commands.Modules.Self;

[BotOwner]
public sealed class OwnerCommands : AkkoCommandModule
{
    private readonly DiscordShardedClient _shardedClient;
    private readonly UtilitiesService _utilitiesService;
    private readonly ILocalizer _localizer;

    public OwnerCommands(DiscordShardedClient shardedClient, UtilitiesService utilities, ILocalizer localizer)
    {
        _shardedClient = shardedClient;
        _utilitiesService = utilities;
        _localizer = localizer;
    }

    [Command("shutdown"), Aliases("die")]
    [Description("cmd_shutdown")]
    public async Task DieAsync(CommandContext context)
    {
        var embed = new SerializableDiscordEmbed()
            .WithDescription("shutdown");

        await context.RespondLocalizedAsync(embed);

        // Clean-up
        foreach (var client in _shardedClient.ShardClients.Values)
            await client.DisconnectAsync();

        context.Services.GetRequiredService<IBotLifetime>().Shutdown();
    }

    [Command("restart")]
    [Description("cmd_restart")]
    public async Task RestartAsync(CommandContext context)
    {
        var embed = new SerializableDiscordEmbed()
            .WithDescription("restart");

        await context.RespondLocalizedAsync(embed);

        // Clean-up
        foreach (var client in _shardedClient.ShardClients.Values)
            await client.DisconnectAsync();

        context.Services.GetRequiredService<IBotLifetime>().Restart();
    }

    [Command("senddirectmessage"), Aliases("senddm")]
    [Description("cmd_senddm")]
    public async Task SendMessageAsync(CommandContext context, [Description("arg_discord_user")] DiscordUser user, [RemainingText, Description("arg_say")] SmartString message)
    {
        var server = _shardedClient.ShardClients.Values
            .SelectMany(x => x.Guilds.Values)
            .FirstOrDefault(x => x.Members.ContainsKey(user.Id));

        if (server is null)
        {
            await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
            return;
        }

        var member = await server.GetMemberSafelyAsync(user.Id);

        if (member is null)
        {
            await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
            return;
        }

        var dm = (_utilitiesService.DeserializeMessage(message, out var dMsg))
            ? await member.SendMessageSafelyAsync(dMsg!)
            : await member.SendMessageSafelyAsync(message);

        await context.Message.CreateReactionAsync((dm is not null) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("sendmessage"), Aliases("send")]
    [Description("cmd_send")]
    public async Task SendMessageAsync(CommandContext context, [Description("arg_channel_id")] ulong cid, [RemainingText, Description("arg_say")] SmartString message)
    {
        var server = _shardedClient.ShardClients.Values
            .SelectMany(x => x.Guilds.Values)
            .FirstOrDefault(x => x.Channels.ContainsKey(cid));

        if (server is null || !server.Channels.TryGetValue(cid, out var channel) || channel.Type is ChannelType.Voice)
        {
            await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
            return;
        }

        var dm = (_utilitiesService.DeserializeMessage(message, out var dMsg))
            ? await channel.SendMessageSafelyAsync(dMsg!)
            : await channel.SendMessageSafelyAsync(message);

        await context.Message.CreateReactionAsync((dm is not null) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("exportcommands"), Aliases("exportcmds")]
    [Description("cmd_exportcommands")]
    public async Task ExportCommandsAsync(CommandContext context, [Description("arg_exportcommand_format")] string format = "yaml")
    {
        var locale = context.GetMessageSettings().Locale;
        using var result = context.CommandsNext.RegisteredCommands.Values
            .Distinct()
            .Select(x => new SerializableCommand(_localizer, x, locale))
            .OrderBy(x => x.Name)
            .ToRentedArray();

        var isJson = format.Equals("json", StringComparison.InvariantCultureIgnoreCase);
        var text = (isJson)
            ? JsonConvert.SerializeObject(result, Formatting.Indented)
            : result.ToYaml(new Serializer());

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

        var message = new DiscordMessageBuilder()
            .WithContent(context.FormatLocalized("exportcommands_description", Formatter.Bold(result.Count.ToString())))
            .AddFile($"commands.{((isJson) ? "json" : "yaml")}", stream);

        await context.Channel.SendMessageAsync(message);
    }

    [Command("setname")]
    public async Task SetNameAsync(CommandContext context, [RemainingText] string newName)
    {
        var result = await context.Client.UpdateCurrentUserAsync(newName).RunAndGetTaskAsync();
        await context.Message.CreateReactionAsync((result.IsCompletedSuccessfully) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("setavatar"), HiddenOverload]
    public async Task SetAvatarAsync(CommandContext context)
        => await SetAvatarAsync(context, context.Message.Attachments.TryGetValue(0, out var attachment) ? attachment.Url : context.Guild.CurrentMember.DefaultAvatarUrl);

    [Command("setavatar")]
    [Description("cmd_setavatar")]
    public async Task SetAvatarAsync(CommandContext context, [Description("arg_emoji_url")] string link)
    {
        var stream = await _utilitiesService.GetOnlineStreamAsync(link);

        if (stream is null)
        {
            await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
            return;
        }

        var result = await context.Client.UpdateCurrentUserAsync(null, stream).RunAndGetTaskAsync();
        await context.Message.CreateReactionAsync((result.IsCompletedSuccessfully) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("listservers")]
    [Description("cmd_listservers")]
    public async Task ListServersAsync(CommandContext context, [Description("arg_uint")] int shard = -1)
    {
        if (shard is -1)
            shard = context.Client.ShardId;

        if (!_shardedClient.ShardClients.TryGetValue(shard, out var client))
        {
            await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
            return;
        }

        var fields = new List<SerializableEmbedField>();
        var guilds = client.Guilds.Values
            .OrderBy(x => x.Name)
            .Chunk(AkkoConstants.LinesPerPage);

        foreach (var group in guilds)
            fields.Add(new(AkkoConstants.ValidWhitespace, string.Join("\n", group.Select(x => $"{Formatter.InlineCode(x.Id.ToString())} {x.Name}")), true));

        var embed = new SerializableDiscordEmbed()
            .WithTitle(context.FormatLocalized("listservers_title", context.Client.ShardId));

        await context.RespondPaginatedByFieldsAsync(embed, fields, 2);
    }

    [Command("leave")]
    [Description("cmd_leave")]
    public async Task ListServersAsync(CommandContext context, DiscordGuild? server = default)
    {
        server ??= context.Guild;

        await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
        await server.LeaveAsync();
    }
}