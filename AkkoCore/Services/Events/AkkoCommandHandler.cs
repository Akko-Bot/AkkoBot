using AkkoCore.Config.Models;
using AkkoCore.Core.Abstractions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events
{
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
            var prefixPos = await _prefixResolver.ResolvePrefixAsync(eventArgs.Message);

            if (prefixPos is -1)
                return;

            var command = cmdHandler.FindCommand(eventArgs.Message.Content[prefixPos..], out var args);

            if (command is null)
                return;

            var context = cmdHandler.CreateContext(eventArgs.Message, eventArgs.Message.Content[..prefixPos], command, args);
            _ = Task.Run(async () => await cmdHandler.ExecuteCommandAsync(context));    // This automatically performs the checks on the command

            eventArgs.Handled = true;
        }
    }
}
