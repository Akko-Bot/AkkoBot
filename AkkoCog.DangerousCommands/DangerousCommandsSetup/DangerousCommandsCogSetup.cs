using AkkoCog.DangerousCommands.Services;
using AkkoCore.Config.Abstractions;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;

namespace AkkoCog.DangerousCommandsSetup
{
    /// <summary>
    /// Initializes this cog's dependencies.
    /// </summary>
    internal class DangerousCommandsCogSetup : ICogSetup
    {
        public string Name { get; } = "DangerousCommands";

        public string Author { get; } = "Kotz#7922";

        // Localization files inside the "../Localization" folder
        public string LocalizationDirectory { get; } = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)?.FullName ?? string.Empty, "Localization");

        public void RegisterServices(IServiceCollection ioc)
            => ioc.AddSingleton<QueryService>();

        public void RegisterSlashCommands(SlashCommandsExtension slashHandler)
        {
            // This cog doesn't have slash commands
        }

        public void RegisterArgumentConverters(CommandsNextExtension cmdHandler)
        {
            // This cog doesn't need argument converters
        }

        public void RegisterCallbacks(DiscordShardedClient shardedClient)
        {
            // This cog doesn't need events.
        }

        public void RegisterComponentResponses(IInteractionResponseManager responseManager)
        {
            // This cog doesn't have interactive messages.
        }
    }
}