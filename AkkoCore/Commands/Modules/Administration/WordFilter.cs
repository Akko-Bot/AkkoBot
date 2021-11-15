using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration;

[RequireGuild]
[Group("filterword"), Aliases("fw")]
[Description("cmd_fw")]
public sealed class WordFilter : AkkoCommandModule
{
    private readonly WordFilterService _service;

    public WordFilter(WordFilterService service)
        => _service = service;

    [Command("add")]
    [Description("cmd_fw_add")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task AddWordAsync(CommandContext context, [RemainingText, Description("arg_fw_word")] string word)
    {
        var success = await _service.SetWordFilterAsync(context.Guild.Id, x =>
        {
            var contains = x.Words.Contains(word);

            if (!contains)
                x.Words.Add(word);

            return !contains;
        });

        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("addmany")]
    [Description("cmd_fw_addmany")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task AddManyWordsAsync(CommandContext context, [Description("arg_fw_words")] params string[] words)
    {
        var success = await _service.SetWordFilterAsync(context.Guild.Id, x =>
        {
            var amount = x.Words.Count;

            foreach (var word in words)
            {
                if (!x.Words.Contains(word))
                    x.Words.Add(word);
            }

            return amount != x.Words.Count;
        });

        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("message")]
    [Description("cmd_fw_message")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task SetNotificationMessageAsync(CommandContext context, [RemainingText, Description("arg_fw_message")] string message)
    {
        await _service.SetWordFilterAsync(context.Guild.Id, x => x.NotificationMessage = message);
        await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
    }

    [Command("notify")]
    [Description("cmd_fw_notify")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task ToggleNotificationAsync(CommandContext context)
    {
        var isEnabled = await _service.SetWordFilterAsync(context.Guild.Id, x => x.Behavior = x.Behavior.ToggleFlag(WordFilterBehavior.NotifyOnDelete));
        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("fw_notify", (isEnabled.HasFlag(WordFilterBehavior.NotifyOnDelete)) ? "enabled" : "disabled"));

        await context.RespondLocalizedAsync(embed);
    }

    [Command("warn")]
    [Description("cmd_fw_warn")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task ToggleWarnOnDeleteAsync(CommandContext context)
    {
        var isEnabled = await _service.SetWordFilterAsync(context.Guild.Id, x => x.Behavior = x.Behavior.ToggleFlag(WordFilterBehavior.WarnOnDelete));
        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("fw_warn", (isEnabled.HasFlag(WordFilterBehavior.WarnOnDelete)) ? "enabled" : "disabled"));

        await context.RespondLocalizedAsync(embed);
    }

    [Command("addignore")]
    [Description("cmd_fw_addignore")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task AddIgnoredIdAsync(CommandContext context, [Description("arg_fw_ids")] params ulong[] ids)
    {
        var success = await _service.SetWordFilterAsync(context.Guild.Id, x =>
        {
            var amount = x.IgnoredIds.Count;

            foreach (var id in ids)
            {
                if (!x.IgnoredIds.Contains((long)id))
                    x.IgnoredIds.Add((long)id);
            }

            return amount != x.IgnoredIds.Count;
        });

        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("removeignore"), Aliases("rmignore")]
    [Description("cmd_fw_removeignore")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task RemoveIgnoredIdAsync(CommandContext context, [Description("arg_fw_ids")] params ulong[] ids)
    {
        var result = await _service.SetWordFilterAsync(context.Guild.Id, x => x.IgnoredIds.RemoveAll(x => ids.Contains((ulong)x)) is not 0);
        await context.Message.CreateReactionAsync((result) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("remove"), Aliases("rm")]
    [Description("cmd_fw_remove")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task RemoveWordAsync(CommandContext context, [RemainingText, Description("arg_fw_word")] string word)
    {
        var success = await _service.SetWordFilterAsync(context.Guild.Id, x => x.Words.Remove(word));
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("removemany"), Aliases("rmmany")]
    [Description("cmd_fw_removemany")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task RemoveManyWordsAsync(CommandContext context, [Description("arg_fw_words")] params string[] words)
    {
        var success = await _service.SetWordFilterAsync(context.Guild.Id, x => x.Words.RemoveAll(y => words.Contains(y)) is not 0);
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("clear")]
    [Description("cmd_fw_clear")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task ClearWordsAsync(CommandContext context)
    {
        var success = await _service.SetWordFilterAsync(context.Guild.Id, x =>
        {
            var amount = x.Words.Count;
            x.Words.Clear();

            return amount != x.Words.Count;
        });

        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("enable")]
    [Description("cmd_fw_enable")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task EnableAsync(CommandContext context)
    {
        var success = await _service.SetWordFilterAsync(context.Guild.Id, x => x.IsActive = !x.IsActive);

        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("fw_toggle", (success) ? "enabled" : "disabled"));

        await context.RespondLocalizedAsync(embed);
    }

    [GroupCommand, Command("list"), Aliases("show")]
    [Description("cmd_fw_list")]
    public async Task ListFilteredWordsAsync(CommandContext context)
    {
        var dbEntry = _service.GetFilteredWords(context.Guild.Id);
        var words = dbEntry?.Words
            .OrderBy(x => x.Trim('*'))
            .Select(x => Formatter.InlineCode(x))
            .ToArray() ?? Array.Empty<string>();

        var isEmpty = dbEntry is null || words.Length is 0;

        var embed = new SerializableDiscordEmbed()
            .WithDescription((isEmpty) ? "fw_list_empty" : string.Join(", ", words).MaxLength(AkkoConstants.MaxEmbedDescriptionLength, AkkoConstants.EllipsisTerminator));

        if (!isEmpty)
        {
            embed.WithTitle("fw_list_title");

            var channels = context.Guild.Channels.Values
                .Where(x => dbEntry!.IgnoredIds.Contains((long)x.Id))
                .OrderBy(x => x.Name)
                .Select(x => '#' + x.Name)
                .ToArray();

            var roles = context.Guild.Roles.Values
                .Where(x => dbEntry!.IgnoredIds.Contains((long)x.Id))
                .OrderBy(x => x.Name)
                .Select(x => x.Name)
                .ToArray();

            var members = context.Guild.Members.Values
                .Where(x => dbEntry!.IgnoredIds.Contains((long)x.Id))
                .OrderBy(x => x.Username)
                .Select(x => x.GetFullname())
                .ToArray();

            if (channels.Length != 0)
                embed.AddField("fw_ignored_channels", string.Join(", ", channels).MaxLength(AkkoConstants.MaxEmbedFieldLength, AkkoConstants.EllipsisTerminator));

            if (roles.Length != 0)
                embed.AddField("fw_ignored_roles", string.Join(", ", roles).MaxLength(AkkoConstants.MaxEmbedFieldLength, AkkoConstants.EllipsisTerminator));

            if (members.Length != 0)
                embed.AddField("fw_ignored_users", string.Join(", ", members).MaxLength(AkkoConstants.MaxEmbedFieldLength, AkkoConstants.EllipsisTerminator));

            if (dbEntry!.Behavior.HasOneFlag(WordFilterBehavior.FilterInvite | WordFilterBehavior.FilterSticker))
            {
                var extraFilters = dbEntry.GetSettings()
                    .Where(x => x.Key is "filter_invites" or "filter_stickers" && x.Value is "True")
                    .Select(x => context.FormatLocalized(x.Key));

                embed.WithFooter($"{context.FormatLocalized("fw_extra_filters")}: {string.Join(", ", extraFilters)}");
            }
        }

        await context.RespondLocalizedAsync(embed, isEmpty, isEmpty);
    }
}