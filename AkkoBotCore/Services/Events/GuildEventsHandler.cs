﻿using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using LinqToDB;
using LinqToDB.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static DSharpPlus.Entities.DiscordEmbedBuilder;

namespace AkkoBot.Services.Events
{
    /// <summary>
    /// Handles guild-related events.
    /// </summary>
    internal class GuildEventsHandler : IGuildEventsHandler
    {
        private readonly Regex _imageUrlRegex = new(
            @"http\S*?\.(png|jpg|jpeg|gif)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        private readonly Regex _inviteRegex = new(
            @"discord(?:\.gg|\.io|\.me|\.li|(?:app)?\.com\/invite)\/(\w+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

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

        public async Task SanitizeNameOnUpdateAsync(DiscordClient _, GuildMemberUpdateEventArgs eventArgs)
        {
            var dbGuild = await _dbCache.GetDbGuildAsync(eventArgs.Guild.Id);

            if (!dbGuild.SanitizeNames
                || (!char.IsPunctuation(eventArgs.Member.DisplayName[0]) && !char.IsSymbol(eventArgs.Member.DisplayName[0]))
                || !eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.ManageNicknames)))
                return;

            await eventArgs.Member.ModifyAsync(user =>
                    user.Nickname = (string.IsNullOrWhiteSpace(dbGuild.CustomSanitizedName))
                        ? EnsureNameSanitization(eventArgs.Member)
                        : dbGuild.CustomSanitizedName
            );
        }

        public async Task SanitizeNameOnJoinAsync(DiscordClient _, GuildMemberAddEventArgs eventArgs)
        {
            var dbGuild = await _dbCache.GetDbGuildAsync(eventArgs.Guild.Id);

            if (!dbGuild.SanitizeNames
                || (!char.IsPunctuation(eventArgs.Member.DisplayName[0]) && !char.IsSymbol(eventArgs.Member.DisplayName[0]))
                || !eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.ManageNicknames)))
                return;

            await eventArgs.Member.ModifyAsync(user =>
                    user.Nickname = (string.IsNullOrWhiteSpace(dbGuild.CustomSanitizedName))
                        ? EnsureNameSanitization(eventArgs.Member)
                        : dbGuild.CustomSanitizedName
            );
        }

        public Task RemuteAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs)
        {
            return Task.Run(async () =>
            {
                using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);
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
            });
        }

        public Task<bool> FilterWordAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            // If message starts with the server prefix or bot has no permission to delete messages or server has no filtered words, quit
            if (eventArgs.Author.IsBot
                || !_dbCache.FilteredWords.TryGetValue(eventArgs.Guild?.Id ?? default, out var filteredWords)
                || filteredWords.Words.Count == 0
                || !filteredWords.IsActive
                || !eventArgs.Guild.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.ManageMessages))
                return Task.FromResult(false);

            return Task.Run(async () =>
            {
                // Do not delete from ignored users, channels and roles
                if (filteredWords.IgnoredIds.Contains((long)eventArgs.Channel.Id)
                || filteredWords.IgnoredIds.Contains((long)eventArgs.Author.Id)
                || (eventArgs.Author as DiscordMember).Roles.Any(role => filteredWords.IgnoredIds.Contains((long)role.Id)))
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
                if (!isDeleted || (!filteredWords.NotifyOnDelete && !filteredWords.WarnOnDelete))
                    return true;

                var dummyCtx = cmdHandler.CreateContext(eventArgs.Message, null, null);
                var toWarn = filteredWords.WarnOnDelete && _roleService.CheckHierarchyAsync(eventArgs.Guild.CurrentMember, eventArgs.Message.Author as DiscordMember);

                var embed = new DiscordEmbedBuilder
                {
                    Description = (string.IsNullOrWhiteSpace(filteredWords.NotificationMessage))
                        ? "fw_default_notification"
                        : filteredWords.NotificationMessage,

                    Footer = (toWarn)
                        ? new EmbedFooter() { Text = "fw_warn_footer" }
                        : null
                };

                var notification = await dummyCtx.RespondLocalizedAsync(eventArgs.Author.Mention, embed, false, true);

                // Apply warning, if enabled
                if (toWarn)
                    await _warningService.SaveWarnAsync(dummyCtx, eventArgs.Guild.CurrentMember, dummyCtx.FormatLocalized("fw_default_warn"));

                // Delete the notification message after some time
                _ = notification.DeleteWithDelayAsync(TimeSpan.FromSeconds(30));

                eventArgs.Handled = true;

                return true;
            });
        }

        public Task<bool> FilterInviteAsync(DiscordClient _, MessageCreateEventArgs eventArgs)
        {
            if (!_dbCache.FilteredWords.TryGetValue(eventArgs.Guild?.Id ?? default, out var filteredWords)
                || !filteredWords.FilterInvites || !HasInvite(eventArgs.Message)
                || !eventArgs.Guild.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.ManageMessages)
                || filteredWords.IgnoredIds.Contains((long)eventArgs.Channel.Id)    // Do not delete from ignored users, channels and roles
                || filteredWords.IgnoredIds.Contains((long)eventArgs.Author.Id)
                || (eventArgs.Author as DiscordMember).Roles.Any(role => filteredWords.IgnoredIds.Contains((long)role.Id)))
                return Task.FromResult(false);

            eventArgs.Message.DeleteAsync();
            eventArgs.Handled = true;

            return Task.FromResult(true);
        }

        public Task<bool> FilterStickerAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            if (!_dbCache.FilteredWords.TryGetValue(eventArgs.Guild?.Id ?? default, out var filteredWords)
                || !filteredWords.FilterStickers || eventArgs.Message.Stickers.Count == 0
                || !eventArgs.Guild.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.ManageMessages)
                || filteredWords.IgnoredIds.Contains((long)eventArgs.Channel.Id)    // Do not delete from ignored users, channels and roles
                || filteredWords.IgnoredIds.Contains((long)eventArgs.Author.Id)
                || (eventArgs.Author as DiscordMember).Roles.Any(role => filteredWords.IgnoredIds.Contains((long)role.Id)))
                return Task.FromResult(false);

            return Task.Run(async () =>
            {
                // Delete the message
                await eventArgs.Message.DeleteAsync();

                // Send the notification
                if (!filteredWords.NotifyOnDelete)
                    return true;

                var cmdHandler = client.GetCommandsNext();

                var embed = new DiscordEmbedBuilder()
                    .WithDescription("fw_stickers_notification");

                var fakeContext = cmdHandler.CreateContext(eventArgs.Message, null, null);

                var notification = await fakeContext.RespondLocalizedAsync(eventArgs.Author.Mention, embed, false, true);

                // Delete the notification message after some time
                _ = notification.DeleteWithDelayAsync(TimeSpan.FromSeconds(30));

                eventArgs.Handled = true;

                return true;
            });
        }

        public Task FilterContentAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            if (eventArgs.Guild is null || eventArgs.Author.IsBot
                || !_dbCache.FilteredContent.TryGetValue(eventArgs.Guild.Id, out var filters)
                || _dbCache.FilteredWords.TryGetValue(eventArgs.Guild?.Id ?? default, out var filteredWords) && filteredWords.IgnoredIds.Contains((long)eventArgs.Author.Id)
                || !eventArgs.Guild.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.ManageMessages))
                return Task.CompletedTask;

            var filter = filters.FirstOrDefault(x => x.ChannelId == eventArgs.Channel.Id);

            if (filter is null || !filter.IsActive)
                return Task.CompletedTask;

            var prefix = _dbCache.Guilds[eventArgs.Guild.Id].Prefix;

            // Check if message contains a valid content. If it doesn't, delete it.
            if ((filter.IsAttachmentOnly && eventArgs.Message.Attachments.Count == 0)
                || (filter.IsUrlOnly && !eventArgs.Message.Content.Contains(new string[2] { "http://", "https://" }, StringComparison.OrdinalIgnoreCase))
                || (filter.IsInviteOnly && !HasInvite(eventArgs.Message))
                || (filter.IsImageOnly && !HasImage(eventArgs.Message))
                || (filter.IsCommandOnly && client.GetCommandsNext().FindCommand(eventArgs.Message.Content[prefix.Length..], out _) is null))
            {
                eventArgs.Handled = true;
                return eventArgs.Message.DeleteAsync();
            }

            return Task.CompletedTask;
        }

        public Task PollVoteAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            if (!int.TryParse(eventArgs.Message.Content, out var vote) || vote <= 0 || !_dbCache.Polls.TryGetValue(eventArgs.Guild.Id, out var polls))
                return Task.CompletedTask;

            var poll = polls.FirstOrDefault(x => x.ChannelId == eventArgs.Channel.Id && x.Type is PollType.Anonymous);

            if (poll is null || vote > poll.Answers.Length || poll.Voters.Contains((long)eventArgs.Author.Id))
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                using var scope = _scopeFactory.GetScopedService<AkkoDbContext>(out var db);

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
            });
        }

        /* Utilities */

        /// <summary>
        /// Checks if a Discord message contains images.
        /// </summary>
        /// <param name="message">The Discord message.</param>
        /// <returns><see langword="true"/> if it contains an image, <see langword="false"/> otherwise.</returns>
        private bool HasImage(DiscordMessage message)
            => _imageUrlRegex.Matches(message.Content + string.Join("\n", message.Attachments.Select(x => x.Url))).Count is not 0;

        /// <summary>
        /// Checks if a Discord message contains a server invite.
        /// </summary>
        /// <param name="message">The Discord message.</param>
        /// <returns><see langword="true"/> if it contains an invite, <see langword="false"/> otherwise.</returns>
        private bool HasInvite(DiscordMessage message)
            => _inviteRegex.Matches(message.Content).Count is not 0;

        /// <summary>
        /// Returns a valid display name for a user.
        /// </summary>
        /// <param name="user">The user to have its name sanitized.</param>
        /// <returns>
        /// The sanitized display name or "No Symbols Allowed" if their nickname AND username
        /// are comprised of special characters only.
        /// </returns>
        private string EnsureNameSanitization(DiscordMember user)
        {
            var result = user.DisplayName.SanitizeUsername();

            return (string.IsNullOrWhiteSpace(result))
                ? (user.Username.All(x => char.IsPunctuation(x) || char.IsSymbol(x))) ? "No Symbols Allowed" : user.Username.SanitizeUsername()
                : result;
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
    }
}