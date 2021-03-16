using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Core.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Core.Services
{
    internal class GlobalEvents
    {
        private readonly IServiceProvider _services;
        private readonly IDbCacher _dbCache;
        private readonly AliasService _aliasService;
        private readonly BotCore _botCore;

        internal GlobalEvents(BotCore botCore, IServiceProvider services)
        {
            _services = services;
            _dbCache = services.GetService<IDbCacher>();
            _aliasService = services.GetService<AliasService>();
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
        }

        /* Event Methods */

        /// <summary>
        /// Mutes a user that has been previously muted.
        /// </summary>
        private Task Remute(object sender, GuildMemberAddEventArgs eventArgs)
        {
            return Task.Run(async () =>
            {
                using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

                var anyChannel = eventArgs.Guild.Channels.FirstOrDefault().Value;
                var botHasManageRoles = eventArgs.Guild.CurrentMember.Roles.Any(role => role.Permissions.HasFlag(Permissions.ManageRoles));

                // Check if user is in the database
                var guildSettings = await db.GuildConfig.GetGuildWithMutesAsync(eventArgs.Guild.Id).ConfigureAwait(false);
                var mutedUser = guildSettings.MutedUserRel.FirstOrDefault(x => x.UserId == eventArgs.Member.Id);

                if (mutedUser is not null && botHasManageRoles)
                {
                    if (eventArgs.Guild.Roles.TryGetValue(guildSettings.MuteRoleId ?? 0, out var muteRole))
                    {
                        // If mute role exists, apply to the user
                        muteRole = eventArgs.Guild.GetRole(guildSettings.MuteRoleId.Value);
                        await eventArgs.Member.GrantRoleAsync(muteRole).ConfigureAwait(false);
                    }
                    else
                    {
                        // If mute role doesn't exist anymore, delete the mute from the database
                        guildSettings.MutedUserRel.Remove(mutedUser);

                        db.GuildConfig.Update(guildSettings);
                        await db.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
            });
        }

        /// <summary>
        /// Makes the bot always respond to "!prefix", regardless of the currently set prefix.
        /// </summary>
        private Task DefaultPrefix(object sender, MessageCreateEventArgs eventArgs)
        {
            if (!eventArgs.Message.Content.StartsWith("!prefix", StringComparison.InvariantCultureIgnoreCase))
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
                var prefix = db.GuildConfig.GetGuild(eventArgs.Guild?.Id ?? 0)?.Prefix
                    ?? db.BotConfig.Cache.BotPrefix;

                if (eventArgs.Guild is not null && prefix.Equals("!"))
                    return;

                // Get command handler and prefix command
                var cmdHandler = (sender as DiscordClient).GetExtension<CommandsNextExtension>();
                var cmd = cmdHandler.FindCommand(eventArgs.Message.Content.Remove(0, prefix.Length), out var cmdArgs)
                    ?? cmdHandler.FindCommand(eventArgs.Message.Content[1..], out cmdArgs);

                // Create the context and execute the command
                if (string.IsNullOrWhiteSpace(cmdArgs) || eventArgs.Guild is not null)
                {
                    var context = cmdHandler.CreateContext(eventArgs.Message, prefix, cmd, cmdArgs);
                    await cmd.ExecuteAndLogAsync(context).ConfigureAwait(false);
                }
            });
        }

        /// <summary>
        /// Executes commands mapped to aliases.
        /// </summary>
        private Task HandleCommandAlias(object sender, MessageCreateEventArgs eventArgs)
        {
            // If message is from a bot or there aren't any global or server aliases, quit. 
            if (eventArgs.Author.IsBot
                || !_dbCache.Aliases.TryGetValue(eventArgs.Guild?.Id ?? default, out var aliases)
                || !_dbCache.Aliases.TryGetValue(default, out var globalAliases))
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                // Get the context prefix
                var prefix = (eventArgs.Guild is null)
                    ? _dbCache.BotConfig.BotPrefix
                    : _dbCache.Guilds[eventArgs.Guild.Id].Prefix;

                // Local function to determine the correct alias from the user input
                bool AliasSelector(AliasEntity alias) 
                    => (alias.IsDynamic && eventArgs.Message.Content.StartsWith(alias.Alias.Replace("{p}", prefix), StringComparison.InvariantCultureIgnoreCase))
                        || (!alias.IsDynamic && eventArgs.Message.Content.Equals(alias.Alias.Replace("{p}", prefix), StringComparison.InvariantCultureIgnoreCase));

                // Find the command represented by the alias
                var alias = aliases?.FirstOrDefault(x => AliasSelector(x)) ?? globalAliases?.FirstOrDefault(x => AliasSelector(x));
                var cmdHandler = (sender as DiscordClient).GetExtension<CommandsNextExtension>();
                var cmd = cmdHandler.FindCommand(alias?.ParseAliasInput(prefix, eventArgs.Message.Content) ?? string.Empty, out var args);

                if (cmd is null)
                    return;

                // Execute the command
                var context = cmdHandler.CreateContext(eventArgs.Message, prefix, cmd, args);

                if (!(await cmd.RunChecksAsync(context, false).ConfigureAwait(false)).Any())
                    await cmd.ExecuteAndLogAsync(context).ConfigureAwait(false);
            });
        }
    }
}