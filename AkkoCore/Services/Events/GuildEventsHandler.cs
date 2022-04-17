using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Models.Serializable.EmbedParts;
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
using LinqToDB.EntityFrameworkCore;
using Kotz.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events;

/// <summary>
/// Handles guild-related events.
/// </summary>
[CommandService<IGuildEventsHandler>(ServiceLifetime.Singleton)]
internal sealed class GuildEventsHandler : IGuildEventsHandler
{
    private readonly ConcurrentDictionary<(ulong, ulong, ulong), (int, DateTimeOffset)> _slowmodeRegister = new();
    private readonly TimeSpan _30seconds = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDbCache _dbCache;
    private readonly RoleService _roleService;
    private readonly WarningService _warningService;

    public GuildEventsHandler(IServiceScopeFactory scopeFactory, IDbCache dbCache, RoleService roleService, WarningService warningService)
    {
        _scopeFactory = scopeFactory;
        _dbCache = dbCache;
        _roleService = roleService;
        _warningService = warningService;
    }

    public async Task RemuteAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs)
    {
        using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);
        var botHasManageRoles = eventArgs.Guild.CurrentMember.Roles.Any(role => role.Permissions.HasPermission(Permissions.ManageRoles));

        // Check if user is in the database
        var dbGuild = await _dbCache.GetDbGuildAsync(eventArgs.Guild.Id);
        var mutedUserExists = await db.MutedUsers.AnyAsyncEF(x => x.UserId == eventArgs.Member.Id);

        if (mutedUserExists && botHasManageRoles)
        {
            if (eventArgs.Guild.Roles.TryGetValue(dbGuild.MuteRoleId ?? default, out var muteRole))
            {
                // If mute role exists, apply to the user
                await eventArgs.Member.GrantRoleAsync(muteRole);
            }
            else
            {
                // If mute role doesn't exist anymore, delete the mute from the database
                await db.MutedUsers.DeleteAsync(x => x.UserId == eventArgs.Member.Id);
            }
        }
    }

    public async Task AddJoinRolesAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs)
    {
        var dbGuild = await _dbCache.GetDbGuildAsync(eventArgs.Guild.Id);

        if (dbGuild.JoinRoles.Count is 0
            || !eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.ManageRoles)))
            return;

        _ = Task.Run(async () =>
        {
            var toRemove = new HashSet<ulong>();

            foreach (ulong roleId in dbGuild.JoinRoles)
            {
                if (!eventArgs.Guild.Roles.TryGetValue(roleId, out var role))
                {
                    toRemove.Add(roleId);
                    continue;
                }

                if (eventArgs.Guild.CurrentMember.Hierarchy > role.Position)
                {
                    await eventArgs.Member.GrantRoleAsync(role);
                    await Task.Delay(AkkoStatics.SafetyDelay).ConfigureAwait(false);
                }
            }

            if (toRemove.Count is not 0)
            {
                using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

                dbGuild.JoinRoles.RemoveAll(x => toRemove.Contains((ulong)x));

                await db.GuildConfig.UpdateAsync(
                    x => x.Id == dbGuild.Id,
                    _ => new GuildConfigEntity() { JoinRoles = dbGuild.JoinRoles }
                );
            }
        });
    }

    public Task AutoSlowmodeAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null
            || eventArgs.Channel.PerUserRateLimit is not 0
            || !eventArgs.Guild.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.ManageChannels)
            || !_dbCache.AutoSlowmode.TryGetValue(eventArgs.Guild.Id, out var slowmode) || !slowmode.IsActive
            || IsIgnoredContext(slowmode.IgnoredIds, eventArgs)
            || ((eventArgs.Author is DiscordMember member) && member.PermissionsIn(eventArgs.Channel).HasOneFlag(Permissions.Administrator | Permissions.ManageChannels | Permissions.ManageMessages)))
            return Task.CompletedTask;

        if (_slowmodeRegister.TryAdd((eventArgs.Guild.Id, eventArgs.Channel.Id, eventArgs.Author.Id), (2, DateTimeOffset.Now.Add(slowmode.SlowmodeTriggerTime))))
        {
            // If entry has beed registred for the first time, setup its removal.
            _ = DoWithDelayAsync(slowmode.SlowmodeTriggerTime, () =>
            {
                _slowmodeRegister.TryRemove((eventArgs.Guild.Id, eventArgs.Channel.Id, eventArgs.Author.Id), out _);
                return Task.CompletedTask;
            });
        }
        else if (_slowmodeRegister.TryGetValue((eventArgs.Guild.Id, eventArgs.Channel.Id, eventArgs.Author.Id), out var stats) && slowmode.MessageAmount > stats.Item1 && DateTimeOffset.Now < stats.Item2)
        {
            // If entry already exists but is still within the server's limits, update its message counter
            _slowmodeRegister.TryUpdate((eventArgs.Guild.Id, eventArgs.Channel.Id, eventArgs.Author.Id), (stats.Item1 + 1, stats.Item2), stats);
        }
        else
        {
            // If entry already exists and has exceeded the server's limits, trigger the slow mode
            if (slowmode.SlowmodeDuration > TimeSpan.Zero)
                _ = DoWithDelayAsync(slowmode.SlowmodeDuration, () => eventArgs.Channel.ModifyAsync(x => x.PerUserRateLimit = 0));

            return eventArgs.Channel.ModifyAsync(x => x.PerUserRateLimit = (int)slowmode.SlowmodeInterval.TotalSeconds);
        }

        return Task.CompletedTask;
    }

    public async Task<bool> FilterWordAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        // If message starts with the server prefix or bot has no permission to delete messages or server has no filtered words, quit
        if (eventArgs.Author.IsBot
            || !_dbCache.FilteredWords.TryGetValue(eventArgs.Guild?.Id ?? default, out var filteredWords)
            || filteredWords.Words.Count is 0
            || !filteredWords.IsActive
            || !eventArgs.Guild!.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.ManageMessages)
            || IsIgnoredContext(filteredWords.IgnoredIds, eventArgs))
            return false;

        var matches = filteredWords.Words.Where(word => eventArgs.Message.Content.Contains(word.Trim('*'), StringComparison.InvariantCultureIgnoreCase));

        // If message doesn't contain any of the filtered words, quit
        if (!matches.Any())
            return false;

        var cmdHandler = client.GetCommandsNext();

        // Do not delete legitimate commands
        if (cmdHandler.FindCommand(eventArgs.Message.Content[_dbCache.Guilds[eventArgs.Guild.Id].Prefix.Length..], out _) is not null)
            return false;

        // Delete the message
        var isDeleted = false;

        foreach (var match in matches)
        {
            if (await DeleteFilteredMessageAsync(eventArgs.Message, match))
            {
                isDeleted = true;
                break;
            }
        }

        // Send notification message, if enabled
        if (isDeleted && filteredWords.Behavior.HasOneFlag(WordFilterBehavior.NotifyOnDelete | WordFilterBehavior.WarnOnDelete))
            await SendFilterWordNotificationAsync(filteredWords, cmdHandler, eventArgs);

        eventArgs.Handled = isDeleted;

        return isDeleted;
    }

    public Task<bool> FilterInviteAsync(DiscordClient _, MessageCreateEventArgs eventArgs)
    {
        if (!_dbCache.FilteredWords.TryGetValue(eventArgs.Guild?.Id ?? default, out var filteredWords)
            || !filteredWords.Behavior.HasFlag(WordFilterBehavior.FilterInvite) || !HasInvite(eventArgs.Message)
            || !eventArgs.Guild!.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.ManageMessages)
            || filteredWords.IgnoredIds.Contains((long)eventArgs.Channel.Id)    // Do not delete from ignored users, channels and roles
            || filteredWords.IgnoredIds.Contains((long)eventArgs.Author.Id)
            || eventArgs.Author.Id == eventArgs.Guild.CurrentMember.Id
            || (eventArgs.Author as DiscordMember)?.Roles.Any(role => filteredWords.IgnoredIds.Contains((long)role.Id)) is true)
            return Task.FromResult(false);

        eventArgs.Handled = true;
        eventArgs.Message.DeleteAsync();

        return Task.FromResult(true);
    }

    public async Task<bool> FilterStickerAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (!_dbCache.FilteredWords.TryGetValue(eventArgs.Guild?.Id ?? default, out var filteredWords)
            || !filteredWords.Behavior.HasFlag(WordFilterBehavior.FilterSticker) || eventArgs.Message.Stickers.Count is 0
            || !eventArgs.Guild!.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.ManageMessages)
            || filteredWords.IgnoredIds.Contains((long)eventArgs.Channel.Id)    // Do not delete from ignored users, channels and roles
            || filteredWords.IgnoredIds.Contains((long)eventArgs.Author.Id)
            || eventArgs.Author.Id == eventArgs.Guild.CurrentMember.Id
            || (eventArgs.Author as DiscordMember)?.Roles.Any(role => filteredWords.IgnoredIds.Contains((long)role.Id)) is true)
            return false;

        // Delete the message
        await eventArgs.Message.DeleteAsync();

        // Send the notification
        if (!filteredWords.Behavior.HasFlag(WordFilterBehavior.NotifyOnDelete))
            return true;

        var cmdHandler = client.GetCommandsNext();

        var embed = new SerializableDiscordEmbed()
            .WithDescription("fw_stickers_notification");

        var fakeContext = cmdHandler.CreateContext(eventArgs.Message, null!, null);

        var notification = await fakeContext.RespondLocalizedAsync(embed, false, true, eventArgs.Author.Mention);

        // Delete the notification message after some time
        _ = notification.DeleteWithDelayAsync(TimeSpan.FromSeconds(30));

        eventArgs.Handled = true;

        return true;
    }

    public Task FilterContentAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.Author.IsBot
            || !_dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild)
            || !_dbCache.FilteredContent.TryGetValue(eventArgs.Guild.Id, out var filters)
            || (_dbCache.FilteredWords.TryGetValue(eventArgs.Guild?.Id ?? default, out var filteredWords) && filteredWords.IgnoredIds.Contains((long)eventArgs.Author.Id))
            || !eventArgs.Guild!.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.ManageMessages))
            return Task.CompletedTask;

        var filter = filters.FirstOrDefault(x => x.ChannelId == eventArgs.Channel.Id);

        // Check if message contains a valid content. If it does, leave it alone.
        if (filter is null || !filter.IsActive
            || (filter.ContentType.HasFlag(ContentFilter.Attachment) && eventArgs.Message.Attachments.Count is not 0)
            || (filter.ContentType.HasFlag(ContentFilter.Url) && AkkoRegexes.Url.IsMatch(eventArgs.Message.Content))
            || (filter.ContentType.HasFlag(ContentFilter.Invite) && HasInvite(eventArgs.Message))
            || (filter.ContentType.HasFlag(ContentFilter.Image) && HasImage(eventArgs.Message))
            || (filter.ContentType.HasFlag(ContentFilter.Sticker) && eventArgs.Message.Stickers.Count is not 0)
            || (filter.ContentType.HasFlag(ContentFilter.Command) && client.GetCommandsNext().FindCommand(eventArgs.Message.Content[dbGuild.Prefix.Length..], out _) is not null))
            return Task.CompletedTask;

        eventArgs.Handled = true;
        return eventArgs.Message.DeleteAsync();
    }

    public async Task PollVoteAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (eventArgs.Author.IsBot || !int.TryParse(eventArgs.Message.Content, out var vote) || vote <= 0 || !_dbCache.Polls.TryGetValue(eventArgs.Guild.Id, out var polls))
            return;

        var poll = polls.FirstOrDefault(x => x.ChannelId == eventArgs.Channel.Id && x.Type is PollType.Anonymous);

        if (poll is null || vote > poll.Answers.Length || poll.Voters.Contains((long)eventArgs.Author.Id))
            return;

        using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

        // Update the poll
        poll.Votes[vote - 1]++;
        poll.Voters.Add((long)eventArgs.Author.Id);

        await db.Polls.UpdateAsync(
            x => x.Id == poll.Id,
            _ => new PollEntity() { Votes = poll.Votes, Voters = poll.Voters }
        );

        // Delete the vote, if it was cast in the server
        if (eventArgs.Guild.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.ManageMessages))
            await eventArgs.Message.DeleteAsync();

        eventArgs.Handled = true;
    }

    public async Task DeleteCommandOnMessageAsync(CommandsNextExtension _, CommandExecutionEventArgs eventArgs)
    {
        if (eventArgs.Context.Guild is null || _dbCache.IsDisposed)
            return;

        var dbGuild = await _dbCache.GetDbGuildAsync(eventArgs.Context.Guild.Id);
        var isIgnoredContext = IsDelOnCmdIgnoredContext(dbGuild, eventArgs.Context);

        if ((dbGuild.Behavior.HasFlag(GuildConfigBehavior.DeleteCmdOnMessage) && isIgnoredContext)
            || (!dbGuild.Behavior.HasFlag(GuildConfigBehavior.DeleteCmdOnMessage) && !isIgnoredContext))
            return;

        await eventArgs.Context.Message.DeleteAsync();
    }

    /* Utilities */

    /// <summary>
    /// Checks if a Discord message contains images.
    /// </summary>
    /// <param name="message">The Discord message.</param>
    /// <returns><see langword="true"/> if it contains an image, <see langword="false"/> otherwise.</returns>
    private bool HasImage(DiscordMessage message)
        => AkkoRegexes.ImageUrl.Matches(message.Content + string.Join("\n", message.Attachments.Select(x => x.Url))).Count is not 0;

    /// <summary>
    /// Checks if a Discord message contains a server invite.
    /// </summary>
    /// <param name="message">The Discord message.</param>
    /// <returns><see langword="true"/> if it contains an invite, <see langword="false"/> otherwise.</returns>
    private bool HasInvite(DiscordMessage message)
        => AkkoRegexes.DiscordInvite.Matches(message.Content).Count is not 0;

    /// <summary>
    /// Performs an action after the specified amount of time.
    /// </summary>
    /// <param name="delay">Waiting time.</param>
    /// <param name="action">Action to be performed.</param>
    private async Task DoWithDelayAsync(TimeSpan delay, Func<Task> action)
    {
        await Task.Delay(delay).ConfigureAwait(false);
        await action().ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes a <see cref="DiscordMessage"/> if its content matches the specified filtered word.
    /// </summary>
    /// <param name="message">The message to be deleted.</param>
    /// <param name="match">The filtered word from the database cache.</param>
    /// <returns><see langword="true"/> if the message got deleted, <see langword="false"/> otherwise.</returns>
    private async Task<bool> DeleteFilteredMessageAsync(DiscordMessage message, string match)
    {
        var left = match.StartsWith('*');
        var right = match.EndsWith('*');

        if (left && right)
        {
            // Match already occurred, just delete the message
            await message.DeleteAsync();
            return true;
        }
        else if (left)
        {
            // Check if any word ends with "match"
            if (message.Content.Split(' ').Any(x => x.EndsWith(match.Trim('*'), StringComparison.InvariantCultureIgnoreCase)))
            {
                await message.DeleteAsync();
                return true;
            }
        }
        else if (right)
        {
            // Check if any word starts with "match"
            if (message.Content.Split(' ').Any(x => x.StartsWith(match.Trim('*'), StringComparison.InvariantCultureIgnoreCase)))
            {
                await message.DeleteAsync();
                return true;
            }
        }
        else
        {
            // One of the words must be an exact match
            if (message.Content.Split(' ').Any(x => x.Equals(match, StringComparison.InvariantCultureIgnoreCase)))
            {
                await message.DeleteAsync();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines whether a command has been executed in a DelMsgOnCmd ignored context.
    /// </summary>
    /// <param name="dbGuild">The database guild settings.</param>
    /// <param name="context">The command context.</param>
    /// <returns><see langword="true"/> if the command was executed in an ignored context, <see langword="false"/> otherwise.</returns>
    private bool IsDelOnCmdIgnoredContext(GuildConfigEntity dbGuild, CommandContext context)
    {
        return dbGuild.DelCmdBlacklist.Contains((long)context.User.Id) || dbGuild.DelCmdBlacklist.Contains((long)context.Channel.Id)
            || dbGuild.DelCmdBlacklist.Any(x => context.Member!.Roles.Select(x => (long)x.Id).Contains(x));
    }

    /// <summary>
    /// Determines whether a message was sent in an ignored context.
    /// </summary>
    /// <param name="ignoredIds">The list of ignored ids.</param>
    /// <param name="eventArgs">The message event.</param>
    /// <returns><see langword="true"/> if the message was sent in an ignored context, <see langword="false"/> otherwise.</returns>
    private bool IsIgnoredContext(List<long> ignoredIds, MessageCreateEventArgs eventArgs)
    {
        return ignoredIds.Contains((long)eventArgs.Channel.Id)
            || ignoredIds.Contains((long)eventArgs.Author.Id)
            || (eventArgs.Guild is not null && (eventArgs.Author as DiscordMember)?.Roles.Any(role => ignoredIds.Contains((long)role.Id)) is true);
    }

    /// <summary>
    /// Sends a notification message for a filtered message and warns the user, if applicable.
    /// </summary>
    /// <param name="filteredWords">The filtered words for the current guild.</param>
    /// <param name="cmdHandler">The command handler.</param>
    /// <param name="eventArgs">The event arguments.</param>
    private async Task SendFilterWordNotificationAsync(FilteredWordsEntity filteredWords, CommandsNextExtension cmdHandler, MessageCreateEventArgs eventArgs)
    {
        var dummyCtx = cmdHandler.CreateContext(eventArgs.Message, null!, null);
        var toWarn = eventArgs.Message.Author is DiscordMember member
            && filteredWords.Behavior.HasFlag(WordFilterBehavior.WarnOnDelete)
            && _roleService.CheckHierarchy(eventArgs.Guild.CurrentMember, member);

        var embed = new SerializableDiscordEmbed()
        {
            Body = new()
            {
                Description = (string.IsNullOrWhiteSpace(filteredWords.NotificationMessage))
                    ? "fw_default_notification"
                    : filteredWords.NotificationMessage
            },

            Footer = (toWarn)
                ? new SerializableEmbedFooter("fw_warn_footer")
                : null
        };

        var notification = await dummyCtx.RespondLocalizedAsync(embed, false, true, eventArgs.Author.Mention);

        // Apply warning, if enabled
        if (toWarn)
            await _warningService.SaveWarnAsync(dummyCtx, eventArgs.Guild.CurrentMember, dummyCtx.FormatLocalized("fw_default_warn"));

        // Delete the notification message after some time
        _ = notification.DeleteWithDelayAsync(_30seconds);
    }
}