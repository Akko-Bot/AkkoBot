using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Common;
using AkkoCore.Commands.Modules.Utilities.Services;
using AkkoCore.Common;
using AkkoCore.Config.Models;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LinqToDB;
using Kotz.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events;

/// <summary>
/// Handles execution of global and guild tags.
/// </summary>
[CommandService<ITagEventHandler>(ServiceLifetime.Singleton)]
internal sealed class TagEventHandler : ITagEventHandler
{
    private const string _remainingTextPlaceholder = "{remaining.text}";
    private readonly TimeSpan _updateTime = TimeSpan.FromDays(1);
    private readonly EventId _tagLogEvent = new(98, nameof(TagEventHandler));

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDbCache _dbCache;
    private readonly BotConfig _botConfig;
    private readonly UtilitiesService _utilitiesService;

    public TagEventHandler(IServiceScopeFactory scopeFactory, IDbCache dbCache, BotConfig botConfig, UtilitiesService utilitiesService)
    {
        _scopeFactory = scopeFactory;
        _dbCache = dbCache;
        _botConfig = botConfig;
        _utilitiesService = utilitiesService;
    }

    public Task ExecuteGlobalTagAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        _dbCache.Guilds.TryGetValue(eventArgs.Guild?.Id ?? default, out var dbGuild);

        if (eventArgs.Author.IsBot || dbGuild?.Behavior.HasFlag(GuildConfigBehavior.IgnoreGlobalTags) is true
            || !_dbCache.Tags.TryGetValue(default, out var globalTags)
            || !HasMinimumPermission(dbGuild, eventArgs))
            return Task.CompletedTask;

        // Get the correct tag
        var dummyContext = GetCommandContext(client, eventArgs, dbGuild?.Prefix ?? _botConfig.Prefix);
        var tag = globalTags
            .Where(x => FilterTags(SmartString.Parse(dummyContext, x.Trigger), eventArgs, x))
            .RandomElementOrDefault();

        return (tag is null || IsIgnoredContext(eventArgs, tag) || HasLocalOverride(eventArgs, tag))
            ? Task.CompletedTask
            : SendTagAsync(dummyContext, eventArgs, tag);
    }

    public Task ExecuteGuildTagAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.Author.IsBot
            || !_dbCache.Tags.TryGetValue(eventArgs.Guild.Id, out var dbTags)
            || !_dbCache.Guilds.TryGetValue(eventArgs.Guild?.Id ?? default, out var dbGuild)
            || !HasMinimumPermission(dbGuild, eventArgs))
            return Task.CompletedTask;

        // Get the correct tag
        var dummyContext = GetCommandContext(client, eventArgs, dbGuild.Prefix);
        var tag = dbTags
            .Where(x => FilterTags(SmartString.Parse(dummyContext, x.Trigger), eventArgs, x))
            .RandomElementOrDefault();

        return (tag is null || IsIgnoredContext(eventArgs, tag))
            ? Task.CompletedTask
            : SendTagAsync(dummyContext, eventArgs, tag);
    }

    public Task ExecuteGlobalEmojiTagAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        _dbCache.Guilds.TryGetValue(eventArgs.Guild?.Id ?? default, out var dbGuild);

        if (eventArgs.Author.IsBot || dbGuild?.Behavior.HasFlag(GuildConfigBehavior.IgnoreGlobalTags) is true
            || !_dbCache.Tags.TryGetValue(default, out var globalTags)

            || !HasMinimumPermission(dbGuild, eventArgs))
            return Task.CompletedTask;

        // Get the correct tag
        var dummyContext = GetCommandContext(client, eventArgs, dbGuild?.Prefix ?? _botConfig.Prefix);
        var tag = globalTags
            .Where(x => FilterEmojiTags(SmartString.Parse(dummyContext, x.Trigger), eventArgs, x))
            .RandomElementOrDefault();

        return (tag is null || IsIgnoredContext(eventArgs, tag) || HasLocalOverride(eventArgs, tag))
            ? Task.CompletedTask
            : SendEmojiTagAsync(client, eventArgs, tag);
    }

    public Task ExecuteGuildEmojiTagAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.Author.IsBot
            || !_dbCache.Tags.TryGetValue(eventArgs.Guild.Id, out var dbTags)
            || !_dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild)
            || !HasMinimumPermission(dbGuild, eventArgs))
            return Task.CompletedTask;

        // Get the correct tag
        var dummyContext = GetCommandContext(client, eventArgs, dbGuild.Prefix);
        var tag = dbTags
            .Where(x => FilterEmojiTags(SmartString.Parse(dummyContext, x.Trigger), eventArgs, x))
            .RandomElementOrDefault();

        return (tag is null || IsIgnoredContext(eventArgs, tag))
            ? Task.CompletedTask
            : SendEmojiTagAsync(client, eventArgs, tag);
    }

    /// <summary>
    /// Checks if the current guild has a tag with the same trigger as a global <paramref name="tag"/>.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <param name="tag">The global tag to be checked.</param>
    /// <returns><see langword="true"/> if the trigger is already used by a guild tag, <see langword="false"/> otherwise.</returns>
    private bool HasLocalOverride(MessageCreateEventArgs eventArgs, TagEntity tag)
        => eventArgs.Guild is not null && _dbCache.Tags.TryGetValue(eventArgs.Guild.Id, out var guildTags) && guildTags.Any(x => x.Trigger.Equals(tag.Trigger));

    /// <summary>
    /// Checks if the user has the minimum permission needed to execute tags.
    /// </summary>
    /// <param name="dbGuild">The database guild.</param>
    /// <param name="eventArgs">The message event arguments.</param>
    /// <returns><see langword="true"/> if there are enough permissions, <see langword="false"/> otherwise.</returns>
    private bool HasMinimumPermission(GuildConfigEntity? dbGuild, MessageCreateEventArgs eventArgs)
    {
        return dbGuild is null || dbGuild.MinimumTagPermissions is Permissions.None
                || (eventArgs.Author as DiscordMember)?.PermissionsIn(eventArgs.Channel).HasPermission(dbGuild.MinimumTagPermissions) is true;
    }

    /// <summary>
    /// Reacts to the event message with the specified tag.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="eventArgs">The event argument.</param>
    /// <param name="dbTag">The tag to be executed.</param>
    private async Task SendTagAsync(CommandContext context, MessageCreateEventArgs eventArgs, TagEntity dbTag)
    {
        // Get the channel where the response should be sent
        var channel = (dbTag.Behavior.HasFlag(TagBehavior.DirectMessage) && eventArgs.Author is DiscordMember member)
            ? await member.CreateDmChannelAsync().ConfigureAwait(false)
            : eventArgs.Channel;

        // Parse the response
        var parsedTrigger = SmartString.Parse(context, dbTag.Trigger);
        var parsedResponse = new SmartString(context, dbTag.Response, dbTag.Behavior.HasFlag(TagBehavior.SanitizeRolePing));
        var triggerIndex = eventArgs.Message.Content.IndexOf(parsedTrigger);

        // This is needed to parse {remaining.text} correctly
        parsedResponse.Replace(
            _remainingTextPlaceholder,
            (triggerIndex <= 0 && eventArgs.Message.Content.Length > parsedTrigger.Length)
                ? eventArgs.Message.Content[(triggerIndex + parsedTrigger.Length + 1)..]
                : string.Empty
        );

        var response = (_utilitiesService.DeserializeMessage(parsedResponse, out var responseContent))
            ? responseContent.AppendDmSourceNote(context, channel, _botConfig, "dm_source_msg", "tag", Formatter.Bold(eventArgs.Guild.Name))
            : new DiscordMessageBuilder() { Content = parsedResponse }.AppendDmSourceNote(context, channel, _botConfig, "dm_source_msg", "tag", Formatter.Bold(eventArgs.Guild.Name));

        // Send the tag
        await channel.SendMessageAsync(response).ConfigureAwait(false);

        // Delete the trigger message
        if (dbTag.Behavior.HasFlag(TagBehavior.Delete) && eventArgs.Guild?.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.ManageMessages) is true)
            await eventArgs.Message.DeleteAsync().ConfigureAwait(false);

        _ = UpdateLastDayUsedAsync(dbTag, _updateTime);

        eventArgs.Handled = true;
    }

    /// <summary>
    /// Reacts to the event message with the specified emoji tag.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="eventArgs">The event argument.</param>
    /// <param name="dbTag">The tag to be executed.</param>
    private Task SendEmojiTagAsync(DiscordClient client, MessageCreateEventArgs eventArgs, TagEntity dbTag)
    {
        // Local function to send the reaction
        static Task SendReactionAsync(MessageCreateEventArgs eventArgs, DiscordEmoji emoji)
        {
            eventArgs.Handled = true;

            return (eventArgs.Guild is null || eventArgs.Guild.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.AddReactions))
                ? eventArgs.Message.CreateReactionAsync(emoji)
                : Task.CompletedTask;
        }

        var match = AkkoRegexes.DiscordEmoji.Match(dbTag.Response);

        if (match.Success
            && ulong.TryParse(match.Groups[2].Value, out var emojiId)
            && DiscordEmoji.TryFromGuildEmote(client, emojiId, out var guildEmoji)
            | DiscordEmoji.TryFromName(client, $":{match.Groups[1].Value}:", out var emoji))
        {
            _ = UpdateLastDayUsedAsync(dbTag, _updateTime);
            return SendReactionAsync(eventArgs, guildEmoji ?? emoji);
        }
        else if (!match.Success && DiscordEmoji.TryFromName(client, dbTag.Response, out emoji))
        {
            _ = UpdateLastDayUsedAsync(dbTag, _updateTime);
            return SendReactionAsync(eventArgs, emoji);
        }

        client.Logger.LogWarning(_tagLogEvent, "A tag emoji could not be found. Removing the tag from the database. Tag Trigger: {TagTrigger} | Tag Response: {TagResponse}", dbTag.Trigger, dbTag.Response);
        return DeleteTagAsync(dbTag);
    }

    /// <summary>
    /// Filters emoji tags.
    /// </summary>
    /// <param name="parsedTrigger">The parsed trigger.</param>
    /// <param name="eventArgs">The event argument.</param>
    /// <param name="dbTag">The tag.</param>
    /// <returns><see langword="true"/> if the tag is elegible for execution, <see langword="false"/> otherwise.</returns>
    private bool FilterEmojiTags(string parsedTrigger, MessageCreateEventArgs eventArgs, TagEntity dbTag)
    {
        if (!dbTag.IsEmoji
            || (eventArgs.Guild is not null
            && dbTag.AllowedPerms is not Permissions.None
            && !(eventArgs.Author as DiscordMember)!.PermissionsIn(eventArgs.Channel).HasOneFlag(Permissions.Administrator | dbTag.AllowedPerms)))
            return false;

        return (dbTag.Behavior.HasFlag(TagBehavior.Anywhere))
            ? eventArgs.Message.Content.Contains(parsedTrigger)
            : eventArgs.Message.Content.Equals(parsedTrigger);
    }

    /// <summary>
    /// Filters tags.
    /// </summary>
    /// <param name="parsedTrigger">The parsed trigger.</param>
    /// <param name="eventArgs">The event argument.</param>
    /// <param name="dbTag">The tag.</param>
    /// <returns><see langword="true"/> if the tag is elegible for execution, <see langword="false"/> otherwise.</returns>
    private bool FilterTags(string parsedTrigger, MessageCreateEventArgs eventArgs, TagEntity dbTag)
    {
        if (dbTag.IsEmoji
            || (eventArgs.Guild is not null
            && dbTag.AllowedPerms is not Permissions.None
            && !(eventArgs.Author as DiscordMember)!.PermissionsIn(eventArgs.Channel).HasOneFlag(Permissions.Administrator | dbTag.AllowedPerms)))
            return false;

        return (dbTag.Behavior.HasFlag(TagBehavior.Anywhere))
            ? eventArgs.Message.Content.Contains(parsedTrigger)
            : (dbTag.Response.Contains("remaining.text", StringComparison.Ordinal))
                ? eventArgs.Message.Content.HasFirstWordOf(parsedTrigger)
                : eventArgs.Message.Content.Equals(parsedTrigger);
    }

    /// <summary>
    /// Gets the command context for the created message.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="eventArgs">The event argument.</param>
    /// <param name="prefix">The prefix of the context.</param>
    /// <returns>The command context for the specified message event.</returns>
    private CommandContext GetCommandContext(DiscordClient client, MessageCreateEventArgs eventArgs, string prefix)
    {
        return client.GetCommandsNext()
            .CreateContext(eventArgs.Message, prefix, null);
    }

    /// <summary>
    /// Updates the last day a tag was used if the current usage exceeds <paramref name="updateTime"/>.
    /// </summary>
    /// <param name="dbTag">The tag to be updated.</param>
    /// <param name="updateTime">Time difference between now and the last day used for the tag to be eligible for update.</param>
    private Task UpdateLastDayUsedAsync(TagEntity dbTag, TimeSpan updateTime)
    {
        if (DateTimeOffset.Now.Subtract(dbTag.LastDayUsed) < updateTime)
            return Task.CompletedTask;

        using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

        db.Attach(dbTag);
        dbTag.LastDayUsed = DateTimeOffset.Now.StartOfDay();

        return db.SaveChangesAsync();
    }

    /// <summary>
    /// Removes the specified tag from the cache and the database.
    /// </summary>
    /// <param name="dbTag">The tag.</param>
    private Task DeleteTagAsync(TagEntity dbTag)
    {
        _dbCache.Tags.TryGetValue(dbTag.GuildIdFK ?? default, out var tags);
        using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

        tags!.TryRemove(dbTag);

        if (tags.Count is 0)
            _dbCache.Tags.TryRemove(dbTag.GuildIdFK ?? default, out _);

        return db.Tags.DeleteAsync(x => x.Id == dbTag.Id);
    }

    /// <summary>
    /// Determines whether the specified tag is being run in an ignored context.
    /// </summary>
    /// <param name="eventArgs">The event argument.</param>
    /// <param name="dbTag">The tag.</param>
    /// <returns><see langword="true"/> if it is an ignored context, <see langword="false"/> otherwise.</returns>
    private bool IsIgnoredContext(MessageCreateEventArgs eventArgs, TagEntity dbTag)
    {
        return dbTag.IgnoredIds.Contains((long)eventArgs.Channel.Id)
            || dbTag.IgnoredIds.Contains((long)dbTag.AuthorId)
            || (eventArgs.Author as DiscordMember)?.Roles.Any(x => dbTag.IgnoredIds.Contains((long)x.Id)) is true;
    }
}