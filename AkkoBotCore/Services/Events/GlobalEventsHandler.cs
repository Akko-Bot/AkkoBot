using AkkoBot.Commands.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Services.Events
{
    /// <summary>
    /// Handles global events.
    /// </summary>
    internal class GlobalEventsHandler : IGlobalEventsHandler
    {
        private readonly IGuildEventsHandler _guildEventsHandler;
        private readonly IDbCache _dbCache;

        public GlobalEventsHandler(IGuildEventsHandler guildEventsHandler, IDbCache dbCache)
        {
            _guildEventsHandler = guildEventsHandler;
            _dbCache = dbCache;
        }

        public Task BlockBlacklistedAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            if (_dbCache.Blacklist.Contains(eventArgs.Author.Id)
                || _dbCache.Blacklist.Contains(eventArgs.Channel.Id)
                || _dbCache.Blacklist.Contains(eventArgs.Guild?.Id ?? default))
            {
                eventArgs.Handled = true;
                return Task.Run(async () =>
                {
                    if (!(await _guildEventsHandler.FilterWordAsync(client, eventArgs) || await _guildEventsHandler.FilterInviteAsync(client, eventArgs) || await _guildEventsHandler.FilterStickerAsync(client, eventArgs)))
                        await _guildEventsHandler.FilterContentAsync(client, eventArgs);
                });
            }

            return Task.CompletedTask;
        }

        public Task DefaultPrefixAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            if (!eventArgs.Message.Content.StartsWith("!prefix", StringComparison.InvariantCultureIgnoreCase))
                return Task.CompletedTask;

            var prefix = _dbCache.Guilds.TryGetValue(eventArgs.Guild?.Id ?? default, out var dbGuild)
                    ? dbGuild.Prefix
                    : _dbCache.BotConfig.BotPrefix;

            if (eventArgs.Guild is not null && prefix.Equals("!"))
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
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

        public Task HandleCommandAliasAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
        {
            var aliasExists = _dbCache.Aliases.TryGetValue(eventArgs.Guild?.Id ?? default, out var aliases)
                & _dbCache.Aliases.TryGetValue(default, out var globalAliases);

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

                var cmd = cmdHandler.FindCommand(
                    (parsedMsg.IsParsed && !alias.IsDynamic)
                        ? alias.FullCommand
                        : alias.ParseAliasInput(parsedMsg, eventArgs.Message.Content),
                    out var args
                );

                if (cmd is null)
                    return;

                // Execute the command
                var context = cmdHandler.CreateContext(eventArgs.Message, prefix, cmd, args);

                if (!(await cmd.RunChecksAsync(context, false)).Any())
                    await cmd.ExecuteAndLogAsync(context);
            });
        }
    }
}