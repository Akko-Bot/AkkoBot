using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Modules.Utilities.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kotz.Extensions;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Utilities;

[RequireGuild]
[Group("emoji"), Aliases("emote", "emojis", "emotes")]
[Description("cmd_emoji")]
public sealed class GuildEmojis : AkkoCommandModule
{
    private const double _additionalDelay = 1.4;
    private readonly UtilitiesService _service;

    public GuildEmojis(UtilitiesService service)
        => _service = service;

    [GroupCommand, Command("list")]
    [Description("cmd_emoji_list")]
    public async Task CheckGuildEmojiAsync(CommandContext context)
    {
        var embed = new SerializableDiscordEmbed()
            .WithTitle("emoji_list_title");

        var emojis = context.Guild.Emojis.Values
            .Where(emoji => emoji.IsAvailable)
            .OrderBy(x => x.Name)
            .OrderBy(x => x.IsAnimated) // ThenBy() doesn't do anything here, for some reason
            .Chunk(15);

        foreach (var emojiGroup in emojis)
            embed.AddField(AkkoConstants.ValidWhitespace, string.Join('\n', emojiGroup.Select(emoji => $"{emoji} {emoji.GetDiscordName()}")), true);

        if (embed.Fields?.Count > 0)
            await context.RespondPaginatedByFieldsAsync(embed, 2);
        else
            await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
    }

    [Command("show"), Aliases("showemoji", "se")]
    [Description("cmd_emoji_showemoji")]
    public async Task ShowEmojiAsync(CommandContext context, [Description("arg_emojis")] params DiscordEmoji[] emojis)
    {
        var result = new StringBuilder();

        foreach (var emoji in emojis.Where(emoji => emoji.Id is not 0))
            result.AppendLine($"{emoji} {Formatter.InlineCode(emoji.GetDiscordName())} {emoji.Url}");

        await context.RespondAsync(result.ToString());
    }

    [Command("add")]
    public async Task AddEmojiAttachmentsAsync(CommandContext context)
    {
        var success = await _service.AddGuildEmojisAsync(context, context.Message.Attachments);
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("add")]
    [Description("cmd_emoji_add")]
    [RequirePermissions(Permissions.ManageEmojis)]
    public async Task AddEmojisAsync(CommandContext context, [Description("arg_emojis")] params DiscordEmoji[] emojis)
    {
        var success = false;
        await context.TriggerTypingAsync();

        foreach (var emoji in emojis)
        {
            success |= await _service.AddGuildEmojiAsync(context, emoji, emoji.Name);
            await Task.Delay(AkkoStatics.SafetyDelay.Add(TimeSpan.FromSeconds(_additionalDelay)));
        }

        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("add")]
    public async Task AddEmojiAsync(
        CommandContext context,
        [Description("arg_emoji_url")] Uri url,
        [Description("arg_emoji_name")] string? name = default)
    {
        var success = await _service.AddGuildEmojiAsync(context, url, name);
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("add")]
    public async Task AddEmojiAsync(
        CommandContext context,
        [Description("arg_emoji")] DiscordEmoji emoji,
        [Description("arg_emoji_name")] string name)
    {
        var success = await _service.AddGuildEmojiAsync(context, emoji, name);
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("remove"), Aliases("rm")]
    [Description("cmd_emoji_remove")]
    [RequirePermissions(Permissions.ManageEmojis)]
    public async Task RemoveEmojisAsync(CommandContext context, [Description("arg_emojis")] params DiscordEmoji[] emojis)
    {
        var success = false;

        if (emojis.Length == 1)
        {
            var gEmoji = await emojis[0].ToGuildEmojiAsync(context.Guild);

            if (gEmoji is not null)
                await gEmoji.DeleteAsync();

            success = gEmoji is not null;
        }
        else
        {
            await context.TriggerTypingAsync();

            foreach (var emoji in await emojis.ToGuildEmojisAsync(context.Guild))
            {
                await Task.Delay(AkkoStatics.SafetyDelay.Add(TimeSpan.FromSeconds(_additionalDelay)));
                await emoji.DeleteAsync();

                success = true;
            }
        }

        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("rename"), Aliases("ren")]
    [Description("cmd_emoji_rename")]
    [RequirePermissions(Permissions.ManageEmojis)]
    public async Task RenameEmojiAsync(
        CommandContext context,
        [Description("arg_emoji")] DiscordEmoji emoji,
        [Description("arg_emoji_newname")] string newName)
    {
        var guildEmoji = await emoji.ToGuildEmojiAsync(context.Guild);

        if (guildEmoji is not null)
            await context.Guild.ModifyEmojiAsync(guildEmoji, newName.SanitizeEmojiName());

        await context.Message.CreateReactionAsync((guildEmoji is not null) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("setrole"), Aliases("setroles")]
    [Description("cmd_emoji_setrole")]
    [RequirePermissions(Permissions.ManageEmojis | Permissions.ManageRoles)]
    public async Task AddRolesAsync(
        CommandContext context,
        [Description("arg_emoji")] DiscordEmoji emoji,
        [Description("arg_discord_roles")] params DiscordRole[] roles)
    {
        var guildEmoji = await emoji.ToGuildEmojiAsync(context.Guild);

        if (guildEmoji is not null)
            await context.Guild.ModifyEmojiAsync(guildEmoji, guildEmoji.Name, roles);

        await context.Message.CreateReactionAsync((guildEmoji is not null) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("removerole"), Aliases("removeroles")]
    [Description("cmd_emoji_removerole")]
    [RequirePermissions(Permissions.ManageEmojis | Permissions.ManageRoles)]
    public async Task RemoveRolesAsync(CommandContext context, [Description("arg_emoji")] DiscordEmoji emoji)
    {
        var guildEmoji = await emoji.ToGuildEmojiAsync(context.Guild);

        if (guildEmoji is not null)
            await context.Guild.ModifyEmojiAsync(guildEmoji, guildEmoji.Name);

        await context.Message.CreateReactionAsync((guildEmoji is not null) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }
}