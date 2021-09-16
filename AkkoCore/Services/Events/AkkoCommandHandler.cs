using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Common;
using AkkoCore.Config.Models;
using AkkoCore.Core.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.EventArgs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events
{
    /// <summary>
    /// Handles command execution.
    /// </summary>
    internal class AkkoCommandHandler : ICommandHandler
    {
        private readonly IDbCache _dbCache;
        private readonly IPrefixResolver _prefixResolver;
        private readonly BotConfig _botConfig;

        public AkkoCommandHandler(IDbCache dbCache, IPrefixResolver prefixResolver, BotConfig botConfig)
        {
            _dbCache = dbCache;
            _prefixResolver = prefixResolver;
            _botConfig = botConfig;
        }

        public async Task HandleCommandAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            var cmdHandler = client.GetCommandsNext();
            var prefixPos = await _prefixResolver.ResolvePrefixAsync(eventArgs.Message).ConfigureAwait(false);

            if (prefixPos is -1)
                return;

            var command = cmdHandler.FindCommand(eventArgs.Message.Content[prefixPos..], out var args);

            if (command is null)
                return;

            var context = cmdHandler.CreateContext(eventArgs.Message, eventArgs.Message.Content[..prefixPos], command, args);

            eventArgs.Handled = CheckAndExecuteAsync(context) is not null;
        }

        public Task HandleCommandAliasAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            var aliasExists = _dbCache.Aliases.TryGetValue(eventArgs.Guild?.Id ?? default, out var aliases)
                & _dbCache.Aliases.TryGetValue(default, out var globalAliases);

            // If message is from a bot or there aren't any global or server aliases, quit.
            if (eventArgs.Author.IsBot && !aliasExists)
                return Task.CompletedTask;

            // Get the context prefix
            var prefix = (eventArgs.Guild is null)
                ? _botConfig.BotPrefix
                : _dbCache.Guilds[eventArgs.Guild.Id].Prefix;

            var cmdHandler = client.GetCommandsNext();
            var dummyCtx = cmdHandler.CreateContext(eventArgs.Message, prefix, null);

            // Local function to determine the correct alias from the user input
            bool AliasSelector(AliasEntity alias)
            {
                var parsedMsg = SmartString.Parse(dummyCtx, alias.Alias);

                return (alias.IsDynamic && eventArgs.Message.Content.StartsWith(parsedMsg, StringComparison.InvariantCultureIgnoreCase))
                    || (!alias.IsDynamic && eventArgs.Message.Content.Equals(parsedMsg, StringComparison.InvariantCultureIgnoreCase));
            }

            // Find the command represented by the alias
            var alias = aliases?.FirstOrDefault(x => AliasSelector(x)) ?? globalAliases?.FirstOrDefault(x => AliasSelector(x));

            if (alias is null)
                return Task.CompletedTask;

            var cmd = cmdHandler.FindCommand(
                (!alias.IsDynamic)
                    ? alias.FullCommand
                    : alias.ParseAliasInput(SmartString.Parse(dummyCtx, alias.Alias), eventArgs.Message.Content),
                out var args
            );

            // Execute the command
            if (cmd is not null)
            {
                var context = cmdHandler.CreateContext(eventArgs.Message, prefix, cmd, args);
                _ = CheckAndExecuteAsync(context);
            }            

            return Task.CompletedTask;
        }

        public Task CheckAndExecuteAsync(CommandContext context)
        {
            return (GetActiveOverride(context.Guild?.Id, context.Command, out var permOverride) && IsAllowedContext(context, permOverride))
                ? Task.Run(async () => await context.Command.ExecuteAndLogAsync(context))           // Execute command with overriden permissions.
                : (permOverride is null || !permOverride.IsActive)
                    ? Task.Run(async () => await context.CommandsNext.ExecuteCommandAsync(context)) // Execute command with default permissions. This method automatically performs the command checks.
                    : null;                                                                         // Command with overriden permission that failed to execute
        }

        public bool GetActiveOverride(ulong? sid, Command cmd, out PermissionOverrideEntity permOverride)
        {
            if (cmd is null || (!_dbCache.PermissionOverrides.TryGetValue(sid ?? default, out var permOverrides) & !_dbCache.PermissionOverrides.TryGetValue(default, out var globalOverrides)))
            {
                permOverride = default;
                return false;
            }

            permOverride = permOverrides?.FirstOrDefault(x => x.Command.Equals(cmd.QualifiedName, StringComparison.OrdinalIgnoreCase))
                ?? globalOverrides?.FirstOrDefault(x => x.Command.Equals(cmd.QualifiedName, StringComparison.OrdinalIgnoreCase));

            return permOverride is not null;
        }

        /// <summary>
        /// Checks if the current context is allowed to run the command for the overriden permissions.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="permOverride">The command permission overrides.</param>
        /// <returns><see langword="true"/> if the command can run in the current context, <see langword="false"/> otherwise.</returns>
        private bool IsAllowedContext(CommandContext context, PermissionOverrideEntity permOverride)
        {
            // Enforce certain permission attributes, no matter what
            static bool IsContextValid(CheckBaseAttribute att, CommandContext context) 
                => ((att.TypeId as Type) != typeof(BotOwnerAttribute) || GeneralService.IsOwner(context, context.User.Id))
                    && ((att.TypeId as Type) != typeof(RequireOwnerInDmAttribute) || (context.Guild is null && GeneralService.IsOwner(context, context.User.Id)))
                    && ((att.TypeId as Type) != typeof(RequireDirectMessageAttribute) || context.Guild is null)
                    && ((att.TypeId as Type) != typeof(RequireGuildAttribute) || context.Guild is not null);

            // Check the user's roles, permissions, user ID, channel ID and the permissions enforced above
            return permOverride.IsActive
                && (context.Member is null || permOverride.AllowedRoleIds.Count is 0 || permOverride.AllowedRoleIds.Any(x => context.Member.Roles.Select(y => (long)y.Id).Contains(x)))
                && (context.Member is null || permOverride.Permissions is Permissions.None || context.Member.PermissionsIn(context.Channel).HasOneFlag(permOverride.Permissions))
                && (permOverride.AllowedUserIds.Count is 0 || permOverride.AllowedUserIds.Contains((long)context.User.Id))
                && (permOverride.AllowedChannelIds.Count is 0 || permOverride.AllowedChannelIds.Contains((long)context.Channel.Id))
                && !context.Command.ExecutionChecks.Any(x => !IsContextValid(x, context));
        }
    }
}
