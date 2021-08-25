using AkkoCore.Commands.Common;
using AkkoCore.Config.Models;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events
{
    /// <summary>
    /// Handles global events.
    /// </summary>
    internal class GlobalEventsHandler : IGlobalEventsHandler
    {
        private readonly IGuildEventsHandler _guildEventsHandler;
        private readonly IDbCache _dbCache;
        private readonly BotConfig _botConfig;

        public GlobalEventsHandler(IGuildEventsHandler guildEventsHandler, IDbCache dbCache, BotConfig botconfig)
        {
            _guildEventsHandler = guildEventsHandler;
            _dbCache = dbCache;
            _botConfig = botconfig;
        }

        public async Task BlockBlacklistedAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            if (_dbCache.Blacklist.Contains(eventArgs.Author.Id)
                || _dbCache.Blacklist.Contains(eventArgs.Channel.Id)
                || _dbCache.Blacklist.Contains(eventArgs.Guild?.Id ?? default))
            {
                eventArgs.Handled = true;

                if (!(await _guildEventsHandler.FilterWordAsync(client, eventArgs) || await _guildEventsHandler.FilterInviteAsync(client, eventArgs) || await _guildEventsHandler.FilterStickerAsync(client, eventArgs)))
                    await _guildEventsHandler.FilterContentAsync(client, eventArgs);
            }
        }

        public async Task DefaultPrefixAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            if (eventArgs.Author.IsBot || !eventArgs.Message.Content.StartsWith("!prefix", StringComparison.InvariantCultureIgnoreCase))
                return;

            var prefix = _dbCache.Guilds.TryGetValue(eventArgs.Guild?.Id ?? default, out var dbGuild)
                    ? dbGuild.Prefix
                    : _botConfig.BotPrefix;

            if (eventArgs.Guild is not null && prefix.Equals("!", StringComparison.Ordinal))
                return;

            // Get command handler and prefix command
            var cmdHandler = client.GetCommandsNext();
            var prefixCmd = cmdHandler.FindCommand(eventArgs.Message.Content[prefix.Length..], out var cmdArgs)
                ?? cmdHandler.FindCommand(eventArgs.Message.Content[1..], out cmdArgs);

            // Create the context and execute the command
            if (string.IsNullOrWhiteSpace(cmdArgs) || eventArgs.Guild is not null)
            {
                var context = cmdHandler.CreateContext(eventArgs.Message, prefix, prefixCmd, cmdArgs);
                await prefixCmd.ExecuteAndLogAsync(context);
                eventArgs.Handled = true;
            }
        }

        public async Task HandleCommandAliasAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            var aliasExists = _dbCache.Aliases.TryGetValue(eventArgs.Guild?.Id ?? default, out var aliases)
                & _dbCache.Aliases.TryGetValue(default, out var globalAliases);

            // If message is from a bot or there aren't any global or server aliases, quit.
            if (eventArgs.Author.IsBot && !aliasExists)
                return;

            // Get the context prefix
            var prefix = (eventArgs.Guild is null)
                ? _botConfig.BotPrefix
                : _dbCache.Guilds[eventArgs.Guild.Id].Prefix;

            // Get a string that parses its placeholders automatically
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
                return;

            var cmd = cmdHandler.FindCommand(
                (!alias.IsDynamic)
                    ? alias.FullCommand
                    : alias.ParseAliasInput(SmartString.Parse(dummyCtx, alias.Alias), eventArgs.Message.Content),
                out var args
            );

            if (cmd is null)
                return;

            // Execute the command
            var context = cmdHandler.CreateContext(eventArgs.Message, prefix, cmd, args);

            if (!(await cmd.RunChecksAsync(context, false)).Any())
                _ = cmd.ExecuteAndLogAsync(context);
        }
    }
}