using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Common;
using AkkoCore.Commands.Modules.Utilities.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kotz.Collections.Extensions;
using Kotz.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Utilities;

[Group("tag"), Aliases("tags")]
[Description("cmd_tag")]
public sealed class Tags : AkkoCommandModule
{
    private readonly IDbCache _dbCache;
    private readonly TagsService _service;
    private readonly UtilitiesService _utilitiesService;

    public Tags(IDbCache dbCache, TagsService service, UtilitiesService utilitiesService)
    {
        _dbCache = dbCache;
        _service = service;
        _utilitiesService = utilitiesService;
    }

    [Command("add")]
    [Description("cmd_tag_add")]
    [RequireUserPermissions(Permissions.ManageMessages)]
    public async Task CreateTagAsync(CommandContext context, [Description("arg_tag_trigger")] string trigger, [Description("arg_tag_response"), RemainingText] string response)
    {
        var result = await _service.AddTagAsync(context, trigger, response, false);
        await context.Message.CreateReactionAsync((result) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("react"), Aliases("emoji")]
    [Description("cmd_tag_react")]
    [RequireUserPermissions(Permissions.ManageMessages)]
    public async Task CreateEmojiTagAsync(CommandContext context, [Description("arg_tag_trigger")] string trigger, [Description("arg_emoji")] DiscordEmoji emoji)
    {
        var result = await _service.AddTagAsync(context, trigger, (emoji.Id is default(ulong)) ? emoji.GetDiscordName() : emoji.ToString(), true);
        await context.Message.CreateReactionAsync((result) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("remove"), Aliases("rm")]
    [Description("cmd_tag_remove")]
    [RequireUserPermissions(Permissions.ManageMessages)]
    public async Task RemoveTagAsync(CommandContext context, [Description("arg_uint")] int tagId)
    {
        var result = await _service.RemoveTagAsync(context, tagId);
        await context.Message.CreateReactionAsync((result) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("addignore")]
    [Description("cmd_tag_addignore")]
    [RequireUserPermissions(Permissions.ManageMessages | Permissions.ManageGuild)]
    public async Task AddIgnoredIdsAsync(CommandContext context, [Description("arg_uint")] int tagId, [Description("arg_snowflakes")] params SnowflakeObject[] ids)
    {
        var result = await _service.SetPropertyAsync(context.Guild, tagId, x =>
        {
            var amount = x.IgnoredIds.Count;

            foreach (long id in ids.Select(x => x.Id))
            {
                if (!x.IgnoredIds.Contains(id))
                    x.IgnoredIds.Add(id);
            }

            return x.IgnoredIds.Count - amount;
        });

        var embed = new SerializableDiscordEmbed()
            .WithDescription((result is 0) ? "ignored_ids_empty" : context.FormatLocalized("ignored_ids_add", result));

        await context.RespondLocalizedAsync(embed, isError: result is 0);
    }

    [Command("removeignore"), Aliases("rmignore")]
    [Description("cmd_tag_removeignore")]
    [RequireUserPermissions(Permissions.ManageMessages | Permissions.ManageGuild)]
    public async Task RemoveIgnoredIdsAsync(CommandContext context, [Description("arg_uint")] int tagId, [Description("arg_snowflakes")] params SnowflakeObject[] ids)
    {
        var result = await _service.SetPropertyAsync(context.Guild, tagId, x =>
        {
            var amount = x.IgnoredIds.Count;

            foreach (long id in ids.Select(x => x.Id))
                x.IgnoredIds.Remove(id);

            return amount - x.IgnoredIds.Count;
        });

        var embed = new SerializableDiscordEmbed()
            .WithDescription((result is 0) ? "ignored_ids_empty" : context.FormatLocalized("ignored_ids_remove", result));

        await context.RespondLocalizedAsync(embed, isError: result is 0);
    }

    [Command("editresponse"), Aliases("response", "er")]
    [Description("cmd_tag_editresponse")]
    [RequireUserPermissions(Permissions.ManageMessages | Permissions.ManageGuild)]
    public async Task EditResponseAsync(CommandContext context, [Description("arg_uint")] int tagId, [Description("arg_tag_response"), RemainingText] string response)
    {
        await _service.SetPropertyAsync(context.Guild, tagId, x => x.Response = response);
        await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
    }

    [Command("edittrigger"), Aliases("trigger", "et")]
    [Description("cmd_tag_edittrigger")]
    [RequireUserPermissions(Permissions.ManageMessages | Permissions.ManageGuild)]
    public async Task EditTriggerAsync(CommandContext context, [Description("arg_uint")] int tagId, [Description("arg_tag_trigger"), RemainingText] string trigger)
    {
        await _service.SetPropertyAsync(context.Guild, tagId, x => x.Trigger = trigger);
        await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
    }

    [Command("editbehavior"), Aliases("behavior", "eb")]
    [Description("cmd_tag_behavior")]
    [RequireUserPermissions(Permissions.ManageMessages | Permissions.ManageGuild)]
    public async Task ToggleBehaviorAsync(CommandContext context, [Description("arg_uint")] int tagId, [Description("arg_tag_behavior")] TagBehavior behavior)
    {
        var behaviorEmoji = DiscordEmoji.FromUnicode(behavior.ToEmojiString());
        var result = await _service.SetPropertyAsync(context.Guild, tagId, x =>
            x.Behavior = (x.Behavior.HasFlag(behavior)
                ? x.Behavior & ~behavior
                : x.Behavior | behavior)
        );

        await context.Message.CreateReactionsAsync(behaviorEmoji, (result.HasFlag(behavior)) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("permission"), Aliases("perms", "perm")]
    [Description("cmd_tag_permission")]
    [RequireUserPermissions(Permissions.ManageMessages | Permissions.ManageGuild)]
    public async Task TogglePermissionAsync(CommandContext context, [Description("arg_uint")] int tagId, [Description("arg_permission"), RemainingText] Permissions permission)
    {
        if (permission is Permissions.None)
        {
            await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
            return;
        }

        var localizedPerm = Formatter.Bold(permission.ToLocalizedStrings(context).FirstOrDefault());
        var result = await _service.SetPropertyAsync(context.Guild, tagId, x =>
            x.AllowedPerms = (x.AllowedPerms.HasFlag(permission))
                ? x.AllowedPerms & ~permission
                : x.AllowedPerms | permission
        );

        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized((result.HasFlag(permission) ? "tag_permission_add" : "tag_permission_remove"), localizedPerm, Formatter.InlineCode($"#{tagId}")));

        await context.RespondLocalizedAsync(embed);
    }

    [Command("info")]
    [Description("cmd_tag_info")]
    public async Task InfoTagAsync(CommandContext context, [Description("arg_uint")] int tagId)
    {
        var tag = _service.GetTags(context.Guild?.Id)
            .FirstOrDefault(x => x.Id == tagId);

        var embed = new SerializableDiscordEmbed();

        if (tag is null)
            embed.WithDescription("tag_not_found");
        else
        {
            var author = (_dbCache.Users.TryGetValue(tag.AuthorId, out var dbUser))
                ? default
                : (context.Guild is null)
                    ? await context.Client.GetUserSafelyAsync(tag.AuthorId)
                    : await context.Guild.GetMemberSafelyAsync(tag.AuthorId) ?? await context.Client.GetUserSafelyAsync(tag.AuthorId);

            embed.WithTitle(context.FormatLocalized("{0} {1}", "tag", $"#{tag.Id}"))
                .WithDescription((tag.IsEmoji) ? tag.Response : Formatter.BlockCode(tag.Response, "yaml"))
                .AddField("trigger", tag.Trigger, true)
                .AddField("author", dbUser?.FullName ?? author?.GetFullname() ?? "DeletedUser", true)
                .WithFooter(context.FormatLocalized("{0}: {1}", "reaction", (tag.IsEmoji) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji));

            if (tag.Behavior is not TagBehavior.None)
                embed.AddField("behaviors", string.Join(", ", tag.Behavior.ToEmojiString()), true);

            if (tag.AllowedPerms is not Permissions.None)
                embed.AddField("requires", string.Join(", ", tag.AllowedPerms.ToLocalizedStrings(context)), false);
        }

        await context.RespondLocalizedAsync(embed, tag is null, tag is null);
    }

    [Command("export")]
    [Description("cmd_tag_export")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task ExportTagsAsync(CommandContext context, [RemainingText, Description("arg_tag_ids")] params int[] tagIds)
    {
        using var tags = _service.GetTags(context.Guild?.Id)
            .When(_ => tagIds.Length is not 0, x => x.Where(y => tagIds.Contains(y.Id)))
            .Select(x => new SerializableTagEntity(x))
            .ToRentedArray();

        if (tags.Count is 0)
        {
            await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
            return;
        }

        using var fileStream = new MemoryStream(Encoding.UTF8.GetBytes(tags.ToYaml()));

        var message = new DiscordMessageBuilder()
            .WithContent(context.FormatLocalized("tag_export_content", tags.Count))
            .AddFile($"{context.Guild?.Name ?? "global"}_tags.yaml", fileStream);

        await context.Channel.SendMessageAsync(message);
    }

    [Command("import")]
    [Description("cmd_tag_import")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task ImportTagsAsync(CommandContext context, [RemainingText, Description("arg_tag_import")] string? tags = default)
    {
        if (string.IsNullOrWhiteSpace(tags))
        {
            var content = await context.Message.Attachments
                .Select(x => _utilitiesService.GetOnlineStringAsync(x.Url))
                .WhenAllAsync();

            tags = string.Join('\n', content);
        }

        try
        {
            var parsedTags = tags.FromYaml<SerializableTagEntity[]>();
            var result = await _service.ImportTagsAsync(context, parsedTags);

            await context.Message.CreateReactionAsync((result > 0) ? AkkoStatics.SuccessEmoji : AkkoStatics.WarningEmoji);
        }
        catch
        {
            await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
        }
    }

    #region Clear

    [Command("clear")]
    [Description("cmd_tag_clear")]
    [RequireUserPermissions(Permissions.ManageMessages | Permissions.ManageGuild)]
    public async Task ClearTagsAsync(CommandContext context)
    {
        var question = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("q_are_you_sure", "tag_delete_all_tags", "q_yes", "q_no"));

        await context.RespondInteractiveAsync(question, "q_yes", async () =>
        {
            var result = await _service.RemoveTagAsync(context);

            var embed = new SerializableDiscordEmbed()
                .WithDescription((result is 0) ? "tag_list_empty" : context.FormatLocalized("tag_delete_success", result));

            await context.RespondLocalizedAsync(embed, isError: result is 0);
        });
    }

    [Command("clear")]
    [RequireUserPermissions(Permissions.ManageMessages | Permissions.ManageGuild)]
    public async Task ClearTagsAsync(CommandContext context, [Description("arg_discord_user")] DiscordUser author)
    {
        var question = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("q_are_you_sure", context.FormatLocalized("tag_delete_all_user_tags", Formatter.Bold(author.GetFullname())), "q_yes", "q_no"));

        await context.RespondInteractiveAsync(question, "q_yes", async () =>
        {
            var result = await _service.RemoveTagAsync(context, x => x.AuthorId == author.Id);

            var embed = new SerializableDiscordEmbed()
                .WithDescription((result is 0) ? "tag_list_empty" : context.FormatLocalized("tag_delete_success", result));

            await context.RespondLocalizedAsync(embed, isError: result is 0);
        });
    }

    [Command("clear")]
    [RequireUserPermissions(Permissions.ManageMessages | Permissions.ManageGuild)]
    public async Task ClearTagsAsync(CommandContext context, [Description("arg_discord_user")] DiscordUser author, [Description("arg_tag_trigger"), RemainingText] string trigger)
    {
        var smartString = new SmartString(context, string.Empty);
        var tagIds = _service.GetTags(context.Guild?.Id)
            .Select(x => (Tag: x, ParsedTrigger: smartString.Parse(x.Trigger)))
            .Where(x => x.Tag.AuthorId == author.Id && x.ParsedTrigger.Equals(trigger, StringComparison.Ordinal))
            .Select(x => x.Tag.Id);

        if (!tagIds.Any())
        {
            var error = new SerializableDiscordEmbed()
                .WithDescription("tag_list_empty");

            await context.RespondLocalizedAsync(error, isError: true);
            return;
        }

        smartString.Content = trigger;

        var question = new SerializableDiscordEmbed()
            .WithDescription(
                context.FormatLocalized(
                    "q_are_you_sure",
                    context.FormatLocalized("tag_delete_trigger_user_tags", Formatter.Bold(author.GetFullname()), Formatter.InlineCode(smartString)),
                    "q_yes",
                    "q_no")
            );

        await context.RespondInteractiveAsync(question, "q_yes", async () =>
        {
            var result = await _service.RemoveTagAsync(context, x => tagIds.Contains(x.Id));

            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized("tag_delete_success", result));

            await context.RespondLocalizedAsync(embed, isError: result is 0);
        });
    }

    [Command("clear")]
    [RequireUserPermissions(Permissions.ManageMessages | Permissions.ManageGuild)]
    public async Task ClearTagsAsync(CommandContext context, [Description("arg_tag_ids")] params int[] ids)
    {
        if (ids.Length is 0)
        {
            var error = new SerializableDiscordEmbed()
                .WithDescription("tag_list_empty");

            await context.RespondLocalizedAsync(error, isError: true);
            return;
        }

        var question = new SerializableDiscordEmbed()
            .WithDescription(
                context.FormatLocalized(
                    "q_are_you_sure",
                    context.FormatLocalized("tag_delete_id_tags", Formatter.InlineCode(ids.Length.ToString())),
                    "q_yes",
                    "q_no")
            );

        await context.RespondInteractiveAsync(question, "q_yes", async () =>
        {
            var result = await _service.RemoveTagAsync(context, x => ids.Contains(x.Id));

            var embed = new SerializableDiscordEmbed()
                .WithDescription((result is 0) ? "tag_list_empty" : context.FormatLocalized("tag_delete_success", result));

            await context.RespondLocalizedAsync(embed, isError: result is 0);
        });
    }

    [Command("clear")]
    [RequireUserPermissions(Permissions.ManageMessages | Permissions.ManageGuild)]
    public async Task ClearTagsAsync(CommandContext context, [Description("arg_tag_trigger"), RemainingText] string trigger)
    {
        var smartString = new SmartString(context, string.Empty);
        var tags = _service.GetTags(context.Guild?.Id)
            .Select(x => (Tag: x, ParsedTrigger: smartString.Parse(x.Trigger)))
            .Where(x => x.ParsedTrigger.Equals(trigger, StringComparison.Ordinal))
            .Select(x => x.Tag.Id);

        if (!tags.Any())
        {
            var error = new SerializableDiscordEmbed()
                .WithDescription("tag_list_empty");

            await context.RespondLocalizedAsync(error, isError: true);
            return;
        }

        smartString.Content = trigger;

        var question = new SerializableDiscordEmbed()
            .WithDescription(
                context.FormatLocalized(
                    "q_are_you_sure",
                    context.FormatLocalized("tag_delete_trigger_tags", Formatter.InlineCode(smartString)),
                    "q_yes",
                    "q_no")
            );

        await context.RespondInteractiveAsync(question, "q_yes", async () =>
        {
            var result = await _service.RemoveTagAsync(context, x => tags.Contains(x.Id));

            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized("tag_delete_success", result));

            await context.RespondLocalizedAsync(embed, isError: result is 0);
        });
    }

    [Command("clearold")]
    [Description("cmd_tag_clearold")]
    [BotOwner]
    public async Task ClearOldTagsAsync(CommandContext context, [Description("arg_tag_clearold_time")] TimeSpan time)
    {
        var question = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("q_are_you_sure", context.FormatLocalized("tag_delete_old_tags", time.TotalDays.ToString("0.00")), "q_yes", "q_no"));

        await context.RespondInteractiveAsync(question, "q_yes", async () =>
        {
            var result = await _service.RemoveOldTagsAsync(time);

            var embed = new SerializableDiscordEmbed()
                .WithDescription((result is 0) ? "tag_list_empty" : context.FormatLocalized("tag_delete_success", result));

            await context.RespondLocalizedAsync(embed, isError: result is 0);
        });
    }

    #endregion Clear

    #region List and Search

    [GroupCommand, Command("list"), Aliases("show")]
    [Description("cmd_tag_list")]
    public async Task ListTagsAsync(CommandContext context)
    {
        var smartString = new SmartString(context, string.Empty);
        await SendTagListAsync(context, smartString, _service.GetTags(context.Guild?.Id));
    }

    [Command("list")]
    public async Task ListTagsAsync(CommandContext context, [Description("arg_discord_user")] DiscordUser author)
    {
        var smartString = new SmartString(context, string.Empty);
        await SendTagListAsync(context, smartString, _service.GetTags(context.Guild?.Id).Where(x => x.AuthorId == author.Id));
    }

    [Command("list")]
    public async Task ListTagsAsync(CommandContext context, [Description("arg_discord_user")] DiscordUser author, [Description("arg_tag_trigger"), RemainingText] string trigger)
    {
        var smartString = new SmartString(context, string.Empty);
        var tags = _service.GetTags(context.Guild?.Id)
            .Select(x => (Tag: x, ParsedTrigger: smartString.Parse(x.Trigger)))
            .Where(x => x.Tag.AuthorId == author.Id && x.ParsedTrigger.Equals(trigger, StringComparison.Ordinal))
            .Select(x => x.Tag);

        await SendTagListAsync(context, smartString, tags);
    }

    [Command("list")]
    public async Task ListTagsAsync(CommandContext context, [Description("arg_tag_trigger"), RemainingText] string trigger)
    {
        var smartString = new SmartString(context, string.Empty);
        var tags = _service.GetTags(context.Guild?.Id)
            .Select(x => (Tag: x, ParsedTrigger: smartString.Parse(x.Trigger)))
            .Where(x => x.ParsedTrigger.Equals(trigger, StringComparison.Ordinal))
            .Select(x => x.Tag);

        await SendTagListAsync(context, smartString, tags);
    }

    [Command("search")]
    [Description("cmd_tag_search")]
    public async Task SearchTagsAsync(CommandContext context, [Description("arg_discord_user")] DiscordUser author, [Description("arg_tag_keyword"), RemainingText] string keyword)
    {
        var tags = _service.GetTags(context.Guild?.Id)
            .Where(x => x.AuthorId == author.Id && x.Response.Contains(keyword, StringComparison.InvariantCultureIgnoreCase));

        await SendTagListAsync(context, new SmartString(context, string.Empty), tags);
    }

    [Command("search")]
    public async Task SearchTagsAsync(CommandContext context, [Description("arg_tag_keyword"), RemainingText] string keyword)
    {
        var tags = _service.GetTags(context.Guild?.Id)
            .Where(x => x.Response.Contains(keyword, StringComparison.InvariantCultureIgnoreCase));

        await SendTagListAsync(context, new SmartString(context, string.Empty), tags);
    }

    #endregion List and Search

    /// <summary>
    /// Sends the list of tags of the current context.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="smartString">An empty smart string.</param>
    /// <param name="tags">The collection of tags.</param>
    private async Task SendTagListAsync(CommandContext context, SmartString smartString, IEnumerable<TagEntity> tags)
    {
        if (tags.Any())
        {
            var embed = GetTagList(smartString, tags)
                .WithTitle((context.Guild is null) ? "tag_global_list_title" : "tag_list_title");

            await context.RespondPaginatedByFieldsAsync(embed, 2);
            return;
        }

        var errorEmbed = new SerializableDiscordEmbed()
            .WithDescription("tag_list_empty");

        await context.RespondLocalizedAsync(errorEmbed, isError: true);
    }

    /// <summary>
    /// Gets a Discord message with a list of tag triggers.
    /// </summary>
    /// <param name="smartString">An empty smart string.</param>
    /// <param name="tags">The collection of tags.</param>
    /// <returns>A Discord message.</returns>
    private SerializableDiscordEmbed GetTagList(SmartString smartString, IEnumerable<TagEntity> tags)
    {
        var result = new SerializableDiscordEmbed();

        foreach (var column in tags.OrderBy(x => x.Id).Chunk(AkkoConstants.LinesPerPage))
        {
            result.AddField(
                AkkoConstants.ValidWhitespace,
                string.Join('\n', column.Select(x => $"{Formatter.InlineCode($"{x.Id}.")} {smartString.Parse(x.Trigger)}")),
                true);
        }

        return result;
    }
}