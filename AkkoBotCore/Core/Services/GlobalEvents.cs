using AkkoBot.Commands.Common;
using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Core.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using static DSharpPlus.Entities.DiscordEmbedBuilder;

namespace AkkoBot.Core.Services
{
    internal class GlobalEvents
    {
        private readonly IServiceProvider _services;
        private readonly IDbCacher _dbCache;
        private readonly AliasService _aliasService;
        private readonly WarningService _warningService;
        private readonly RoleService _roleService;
        private readonly BotCore _botCore;

        internal GlobalEvents(BotCore botCore, IServiceProvider services)
        {
            _services = services;
            _dbCache = services.GetService<IDbCacher>();
            _aliasService = services.GetService<AliasService>();
            _warningService = services.GetService<WarningService>();
            _roleService = services.GetService<RoleService>();
            _botCore = botCore;
        }

        /// <summary>
        /// Defines the behaviors the bot should have for specific user actions.
        /// </summary>
        internal void RegisterEvents()
        {
            // Prevent mute evasion
            _botCore.BotShardedClient.GuildMemberAdded += Remute;

            // Show prefix regardless of current config
            _botCore.BotShardedClient.MessageCreated += DefaultPrefix;

            // Catch aliased commands and execute them
            _botCore.BotShardedClient.MessageCreated += HandleCommandAlias;

            // Delete filtered words
            _botCore.BotShardedClient.MessageCreated += FilterWord;

            // On command
            foreach (var cmdHandler in _botCore.CommandExt.Values)
                cmdHandler.CommandExecuted += SaveUserOnUpdate;
        }

        /* Event Methods */

        /// <summary>
        /// Saves a user to the database on command execution.
        /// </summary>
        private Task SaveUserOnUpdate(CommandsNextExtension cmdHandler, CommandExecutionEventArgs eventArgs)
        {
            return Task.Run(async () =>
            {
                using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

                // Track the user who triggered the command
                var isTracking = await db.DiscordUsers.CreateOrUpdateAsync(eventArgs.Context.User);

                // Track the mentioned users in the message, if any
                foreach (var mentionedUser in eventArgs.Context.Message.MentionedUsers)
                    isTracking = isTracking || await db.DiscordUsers.CreateOrUpdateAsync(mentionedUser);

                // Save if there is at least one user being tracked
                if (isTracking)
                    await db.SaveChangesAsync();
            });
        }

        /// <summary>
        /// Mutes a user that has been previously muted.
        /// </summary>
        private Task Remute(DiscordClient client, GuildMemberAddEventArgs eventArgs)
        {
            return Task.Run(async () =>
            {
                using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

                var anyChannel = eventArgs.Guild.Channels.FirstOrDefault().Value;
                var botHasManageRoles = eventArgs.Guild.CurrentMember.Roles.Any(role => role.Permissions.HasFlag(Permissions.ManageRoles));

                // Check if user is in the database
                var guildSettings = await db.GuildConfig.GetGuildWithMutesAsync(eventArgs.Guild.Id);
                var mutedUser = guildSettings.MutedUserRel.FirstOrDefault(x => x.UserId == eventArgs.Member.Id);

                if (mutedUser is not null && botHasManageRoles)
                {
                    if (eventArgs.Guild.Roles.TryGetValue(guildSettings.MuteRoleId ?? 0, out var muteRole))
                    {
                        // If mute role exists, apply to the user
                        muteRole = eventArgs.Guild.GetRole(guildSettings.MuteRoleId.Value);
                        await eventArgs.Member.GrantRoleAsync(muteRole);
                    }
                    else
                    {
                        // If mute role doesn't exist anymore, delete the mute from the database
                        guildSettings.MutedUserRel.Remove(mutedUser);

                        db.GuildConfig.Update(guildSettings);
                        await db.SaveChangesAsync();
                    }
                }
            });
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
                var dbCache = _services.GetService<IDbCacher>();
                var prefix = dbCache.Guilds.TryGetValue(eventArgs.Guild?.Id ?? default, out var dbGuild)
                    ? dbGuild.Prefix
                    : dbCache.BotConfig.BotPrefix;

                if (eventArgs.Guild is not null && prefix.Equals("!"))
                    return;

                // Get command handler and prefix command
                var cmdHandler = client.GetExtension<CommandsNextExtension>();
                var cmd = cmdHandler.FindCommand(eventArgs.Message.Content.Remove(0, prefix.Length), out var cmdArgs)
                    ?? cmdHandler.FindCommand(eventArgs.Message.Content[1..], out cmdArgs);

                // Create the context and execute the command
                if (string.IsNullOrWhiteSpace(cmdArgs) || eventArgs.Guild is not null)
                {
                    var context = cmdHandler.CreateContext(eventArgs.Message, prefix, cmd, cmdArgs);
                    await cmd.ExecuteAndLogAsync(context);
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
                var cmdHandler = client.GetExtension<CommandsNextExtension>();
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

                var cmdHandler = client.GetExtension<CommandsNextExtension>();

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
                _ = DeleteWithDelayAsync(notification, TimeSpan.FromSeconds(30));
            });
        }

        /* Utility Methods */

        /// <summary>
        /// Deletes a <see cref="DiscordMessage"/> if its content match the specified filtered word.
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
        /// Deletes a <see cref="DiscordMessage"/> after the specified time.
        /// </summary>
        /// <param name="message">The message to be deleted.</param>
        /// <param name="delay">How long to wait before the message is deleted.</param>
        private async Task DeleteWithDelayAsync(DiscordMessage message, TimeSpan delay)
        {
            await Task.Delay(delay).ConfigureAwait(false);
            try { await message.DeleteAsync(); } catch { }  // Message might get deleted by someone else in the meantime
        }
    }
}