using AkkoCore.Config.Abstractions;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;

namespace AkkoCog.DangerousCommands.DangerousCommandsSetup;

/// <summary>
/// Initializes this cog's dependencies.
/// </summary>
internal sealed class DangerousCommandsCogSetup : ICogSetup
{
    public string Name { get; } = "DangerousCommands";

    public string Version { get; } = "1.0.0";

    public string Author { get; } = "Kotz#7922";

    public string Description { get; } = "cog_dangerouscommands_desc";

    // Localization files inside the "../Localization" folder
    public string? LocalizationDirectory { get; } = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)?.FullName ?? string.Empty, "Localization");

    public void RegisterServices(IServiceCollection ioc)
    {
        // This cog doesn't register services with factories or concrete implementations.
    }

    public void RegisterSlashCommands(SlashCommandsExtension slashHandler)
    {
        // This cog doesn't have slash commands.
    }

    public void RegisterArgumentConverters(CommandsNextExtension cmdHandler)
    {
        // This cog doesn't need argument converters.
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