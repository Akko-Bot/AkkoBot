using AkkoBot.Commands.Attributes;
using AkkoBot.Commands.Common;
using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Common;
using AkkoBot.Core.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Queries;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using static DSharpPlus.Entities.DiscordEmbedBuilder;

namespace AkkoBot.Core.Services
{
    /// <summary>
    /// Manages the behavior the bot should have for specific user actions.
    /// </summary>
    internal class GlobalEvents
    {
        private readonly IServiceScope _scope;
        private readonly IDbCache _dbCache;
        private readonly AliasService _aliasService;
        private readonly WarningService _warningService;
        private readonly RoleService _roleService;
        private readonly BotCore _botCore;

        internal GlobalEvents(BotCore botCore, IServiceProvider services)
        {
            _scope = services.CreateScope();
            _dbCache = services.GetService<IDbCache>();
            _aliasService = services.GetService<AliasService>();
            _warningService = services.GetService<WarningService>();
            _roleService = services.GetService<RoleService>();
            _botCore = botCore;
        }

        /// <summary>
        /// Registers events the bot should listen to in order to react to specific user actions.
        /// </summary>
        /// <exception cref="InvalidOperationException"/>
        internal void RegisterEvents()
        {
            if (_botCore is null)
                throw new InvalidOperationException("Bot core cannot be null.");

            // Prevent mute evasion
            _botCore.BotShardedClient.GuildMemberAdded += Remute;

            // Prevent events from running for blacklisted users, channels and servers
            _botCore.BotShardedClient.MessageCreated += BlockBlacklisted;

            // Show prefix regardless of current config
            _botCore.BotShardedClient.MessageCreated += DefaultPrefix;

            // Catch aliased commands and execute them
            _botCore.BotShardedClient.MessageCreated += HandleCommandAlias;

            // Delete filtered words
            _botCore.BotShardedClient.MessageCreated += FilterWord;

            // Delete messages with stickers
            _botCore.BotShardedClient.MessageCreated += FilterSticker;

            // Assign role on channel join/leave
            _botCore.BotShardedClient.VoiceStateUpdated += VoiceRole;

            // Save guild on join
            _botCore.BotShardedClient.GuildCreated += SaveGuildOnJoin;

            // Decache guild on leave
            _botCore.BotShardedClient.GuildDeleted += DecacheGuildOnLeave;

            foreach (var cmdHandler in _botCore.CommandExt.Values)
            {
                // Log command execution
                cmdHandler.CommandExecuted += LogCmdExecution;
                cmdHandler.CommandErrored += LogCmdError;

                // Save user on command
                cmdHandler.CommandExecuted += SaveUserOnCmd;
            }
        }

        /* Event Methods */

        /// <summary>
        /// Mutes a user that has been previously muted.
        /// </summary>
        private Task Remute(DiscordClient client, GuildMemberAddEventArgs eventArgs)
        {
            return Task.Run(async () =>
            {
                var db = _scope.ServiceProvider.GetService<AkkoDbContext>();

                var anyChannel = eventArgs.Guild.Channels.FirstOrDefault().Value;
                var botHasManageRoles = eventArgs.Guild.CurrentMember.Roles.Any(role => role.Permissions.HasOneFlag(Permissions.ManageRoles | Permissions.Administrator));

                // Check if user is in the database
                var dbGuild = await db.GuildConfig.GetGuildWithMutesAsync(eventArgs.Guild.Id);
                var mutedUser = dbGuild.MutedUserRel.FirstOrDefault(x => x.UserId == eventArgs.Member.Id);

                if (mutedUser is not null && botHasManageRoles)
                {
                    if (eventArgs.Guild.Roles.TryGetValue(dbGuild.MuteRoleId ?? 0, out var muteRole))
                    {
                        // If mute role exists, apply to the user
                        muteRole = eventArgs.Guild.GetRole(dbGuild.MuteRoleId.Value);
                        await eventArgs.Member.GrantRoleAsync(muteRole);
                    }
                    else
                    {
                        // If mute role doesn't exist anymore, delete the mute from the database
                        dbGuild.MutedUserRel.Remove(mutedUser);

                        db.GuildConfig.Update(dbGuild);
                        await db.SaveChangesAsync();
                    }
                }
            });
        }

        /// <summary>
        /// Stops the callback chain if the message comes from a blacklisted context.
        /// </summary>
        private Task BlockBlacklisted(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            if (_dbCache.Blacklist.Contains(eventArgs.Author.Id)
                || _dbCache.Blacklist.Contains(eventArgs.Channel.Id)
                || _dbCache.Blacklist.Contains(eventArgs.Guild?.Id ?? default))
            {
                eventArgs.Handled = true;
                return FilterWord(client, eventArgs);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Makes the bot always respond to "!prefix", regardless of the currently set prefix.
        /// </summary>
        private Task DefaultPrefix(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            if (!eventArgs.Message.Content.StartsWith("!prefix", StringComparison.InvariantCultureIgnoreCase))
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                var prefix = _dbCache.Guilds.TryGetValue(eventArgs.Guild?.Id ?? default, out var dbGuild)
                    ? dbGuild.Prefix
                    : _dbCache.BotConfig.BotPrefix;

                if (eventArgs.Guild is not null && prefix.Equals("!"))
                    return;

                // Get command handler and prefix command
                var cmdHandler = client.GetCommandsNext();
                var cmd = cmdHandler.FindCommand(eventArgs.Message.Content[prefix.Length..], out var cmdArgs)
                    ?? cmdHandler.FindCommand(eventArgs.Message.Content[1..], out cmdArgs);

                // Create the context and execute the command
                if (string.IsNullOrWhiteSpace(cmdArgs) || eventArgs.Guild is not null)
                {
                    var context = cmdHandler.CreateContext(eventArgs.Message, prefix, cmd, cmdArgs);
                    await cmd.ExecuteAndLogAsync(context);
                    eventArgs.Handled = true;
                }
            });
        }

        /// <summary>
        /// Executes commands mapped to aliases.
        /// </summary>
        private Task HandleCommandAlias(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            var aliasExists = _dbCache.Aliases.TryGetValue(eventArgs.Guild?.Id ?? default, out var aliases);
            aliasExists = _dbCache.Aliases.TryGetValue(default, out var globalAliases) && aliasExists;

            // If message is from a bot or there aren't any global or server aliases, quit.
            if (eventArgs.Author.IsBot && !aliasExists)
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                // Get the context prefix
                var prefix = (eventArgs.Guild is null)
                    ? _dbCache.BotConfig.BotPrefix
                    : _dbCache.Guilds[eventArgs.Guild.Id].Prefix;

                // Get a string that parses its placeholders automatically
                var cmdHandler = client.GetCommandsNext();
                var dummyCtx = cmdHandler.CreateContext(eventArgs.Message, prefix, null);
                var parsedMsg = new SmartString(dummyCtx, string.Empty);

                // Local function to determine the correct alias from the user input
                bool AliasSelector(AliasEntity alias)
                {
                    parsedMsg.Content = alias.Alias;

                    return (alias.IsDynamic && eventArgs.Message.Content.StartsWith(parsedMsg.Content, StringComparison.InvariantCultureIgnoreCase))
                        || (!alias.IsDynamic && eventArgs.Message.Content.Equals(parsedMsg.Content, StringComparison.InvariantCultureIgnoreCase));
                }

                // Find the command represented by the alias
                var alias = aliases?.FirstOrDefault(x => AliasSelector(x)) ?? globalAliases?.FirstOrDefault(x => AliasSelector(x));

                if (alias is null)
                    return;

                var cmd = cmdHandler.FindCommand(((parsedMsg.IsParsed && !alias.IsDynamic) ? alias.FullCommand : alias.ParseAliasInput(prefix, eventArgs.Message.Content)), out var args);

                if (cmd is null)
                    return;

                // Execute the command
                var context = cmdHandler.CreateContext(eventArgs.Message, prefix, cmd, args);

                if (!(await cmd.RunChecksAsync(context, false)).Any())
                    await cmd.ExecuteAndLogAsync(context);
            });
        }

        /// <summary>
        /// Deletes a user message if it contains a filtered word.
        /// </summary>
        private Task FilterWord(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            // If message starts with the server prefix or bot has no permission to delete messages or server has no filtered words, quit
            if (eventArgs.Author.IsBot
                || !_dbCache.FilteredWords.TryGetValue(eventArgs.Guild?.Id ?? default, out var filteredWords)
                || filteredWords.Words.Count == 0
                || !filteredWords.Enabled
                || !eventArgs.Guild.CurrentMember.PermissionsIn(eventArgs.Channel).HasFlag(Permissions.ManageMessages))
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                // Do not delete from ignored users, channels and roles
                if (filteredWords.IgnoredIds.Contains((long)eventArgs.Channel.Id)
                || filteredWords.IgnoredIds.Contains((long)eventArgs.Author.Id)
                || (eventArgs.Author as DiscordMember).Roles.Any(role => filteredWords.IgnoredIds.Contains((long)role.Id)))
                    return;

                var match = filteredWords.Words.FirstOrDefault(word => eventArgs.Message.Content.Contains(word.Trim('*'), StringComparison.InvariantCultureIgnoreCase));

                // If message doesn't contain any of the filtered words, quit
                if (match is null)
                    return;

                var cmdHandler = client.GetCommandsNext();

                // Do not delete legitimate commands
                if (cmdHandler.FindCommand(eventArgs.Message.Content[_dbCache.Guilds[eventArgs.Guild.Id].Prefix.Length..], out _) is not null)
                    return;

                // Delete the message
                if (!await DeleteFilteredMessageAsync(eventArgs.Message, match))
                    return;

                // Send notification message, if enabled
                if (!filteredWords.NotifyOnDelete && !filteredWords.WarnOnDelete)
                    return;

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
            });
        }

        /// <summary>
        /// Deletes a user message if it contains a sticker.
        /// </summary>
        private Task FilterSticker(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            if (!_dbCache.FilteredWords.TryGetValue(eventArgs.Guild?.Id ?? default, out var filteredWords)
                || !filteredWords.FilterStickers || eventArgs.Message.Stickers.Count == 0
                || !eventArgs.Guild.CurrentMember.PermissionsIn(eventArgs.Channel).HasFlag(Permissions.ManageMessages))
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                // Do not delete from ignored users, channels and roles
                if (filteredWords.IgnoredIds.Contains((long)eventArgs.Channel.Id)
                || filteredWords.IgnoredIds.Contains((long)eventArgs.Author.Id)
                || (eventArgs.Author as DiscordMember).Roles.Any(role => filteredWords.IgnoredIds.Contains((long)role.Id)))
                    return;

                // Delete the message
                await eventArgs.Message.DeleteAsync();

                // Send the notification
                if (!filteredWords.NotifyOnDelete)
                    return;

                var cmdHandler = client.GetCommandsNext();

                var embed = new DiscordEmbedBuilder()
                    .WithDescription("fw_stickers_notification");

                var fakeContext = cmdHandler.CreateContext(eventArgs.Message, null, null);

                var notification = await fakeContext.RespondLocalizedAsync(eventArgs.Author.Mention, embed, false, true);

                // Delete the notification message after some time
                _ = notification.DeleteWithDelayAsync(TimeSpan.FromSeconds(30));
            });
        }

        /// <summary>
        /// Saves a user to the database on command execution.
        /// </summary>
        private Task SaveUserOnCmd(CommandsNextExtension cmdHandler, CommandExecutionEventArgs eventArgs)
        {
            return Task.Run(async () =>
            {
                var db = _scope.ServiceProvider.GetService<AkkoDbContext>();

                // Track the user who triggered the command
                //var isTracking = await db.DiscordUsers.CreateOrUpdateAsync(eventArgs.Context.User);
                var isUnchanged = db.Upsert(eventArgs.Context.User).State is EntityState.Unchanged;

                // Track the mentioned users in the message, if any
                foreach (var mentionedUser in eventArgs.Context.Message.MentionedUsers)
                    isUnchanged &= db.Upsert(mentionedUser).State is EntityState.Unchanged;

                // Save if there is at least one user being tracked
                if (!isUnchanged)
                    await db.SaveChangesAsync();
            });
        }

        /// <summary>
        /// Assigns or revokes a role upon voice channel connection/disconnection
        /// </summary>
        private Task VoiceRole(DiscordClient client, VoiceStateUpdateEventArgs eventArgs)
        {
            // Check for role hierarchy, not just role perm
            if (eventArgs.Before == eventArgs.After || !eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasFlag(Permissions.ManageRoles)))
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                var db = _scope.ServiceProvider.GetService<AkkoDbContext>();
                var user = eventArgs.User as DiscordMember;

                var voiceRoles = await db.VoiceRoles.Fetch(
                    (user.VoiceState.Channel is null)
                            ? x => x.GuildIdFk == eventArgs.Guild.Id
                            : x => x.GuildIdFk == eventArgs.Guild.Id && x.ChannelId == eventArgs.Channel.Id
                )
                .ToArrayAsync();

                if (user.VoiceState.Channel is not null)
                {
                    // Connection
                    foreach (var voiceRole in voiceRoles.DistinctBy(x => x.RoleId))
                    {
                        if (eventArgs.Guild.Roles.TryGetValue(voiceRole.RoleId, out var role) && !user.Roles.Contains(role))
                            await user.GrantRoleAsync(role);
                        else if (role is null)
                            db.Remove(voiceRole);
                    }
                }
                else
                {
                    // Disconnection
                    foreach (var voiceRole in voiceRoles)
                    {
                        if (eventArgs.Guild.Roles.TryGetValue(voiceRole.RoleId, out var role) && user.Roles.Contains(role))
                            await user.RevokeRoleAsync(role);
                        else if (role is null)
                            db.Remove(voiceRole);
                    }
                }

                await db.SaveChangesAsync();
            });
        }

        /// <summary>
        /// Saves default guild settings to the database and caches when the bot joins a guild.
        /// </summary>
        private Task SaveGuildOnJoin(DiscordClient client, GuildCreateEventArgs eventArgs)
        {
            if (_dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild))
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                var db = _scope.ServiceProvider.GetService<AkkoDbContext>();

                dbGuild = await _dbCache.GetGuildAsync(eventArgs.Guild.Id);
                var filteredWords = await db.FilteredWords.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.GuildIdFK == eventArgs.Guild.Id);

                _dbCache.Guilds.TryAdd(dbGuild.GuildId, dbGuild);

                if (filteredWords is not null)
                    _dbCache.FilteredWords.TryAdd(filteredWords.GuildIdFK, filteredWords);
            });
        }

        /// <summary>
        /// Remove a guild from the cache when the bot is removed from it.
        /// </summary>
        private Task DecacheGuildOnLeave(DiscordClient client, GuildDeleteEventArgs eventArgs)
        {
            _dbCache.Guilds.TryRemove(eventArgs.Guild.Id, out _);
            _dbCache.FilteredWords.TryRemove(eventArgs.Guild.Id, out _);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs basic information about command execution.
        /// </summary>
        private Task LogCmdExecution(CommandsNextExtension cmdHandler, CommandExecutionEventArgs eventArgs)
        {
            cmdHandler.Client.Logger.LogCommand(LogLevel.Information, eventArgs.Context);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Logs exceptions thrown during command execution.
        /// </summary>
        private Task LogCmdError(CommandsNextExtension cmdHandler, CommandErrorEventArgs eventArgs)
        {
            if (eventArgs.Exception
                is not ArgumentException             // Ignore commands with invalid arguments and subcommands that do not exist
                and not ChecksFailedException        // Ignore command check fails
                and not CommandNotFoundException     // Ignore commands that do not exist
                and not InvalidOperationException)   // Ignore groups that are not commands themselves
            {
                // Log common errors
                cmdHandler.Client.Logger.LogCommand(
                    LogLevel.Error,
                    eventArgs.Context,
                    null,
                    eventArgs.Exception
                );
            }
            else if (eventArgs.Exception is ChecksFailedException ex && ex.FailedChecks[0].GetType() == typeof(GlobalCooldownAttribute))
            {
                cmdHandler.Client.Logger.LogCommand(LogLevel.Warning, eventArgs.Context, "Command execution has been cancelled due to an active cooldown.");
                return eventArgs.Context.Message.CreateReactionAsync(AkkoEntities.CooldownEmoji);
            }

            return Task.CompletedTask;
        }

        /* Utility Methods */

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