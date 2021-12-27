using AkkoCog.AntiPhishing.AntiPhishing.Abstractions;
using AkkoCore.Config.Abstractions;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;

namespace AkkoCog.AntiPhishing.AntiPhishingSetup;

/// <summary>
/// Initializes this cog's dependencies.
/// </summary>
public sealed class AntiPhishingCogSetup : ICogSetup
{
    public string Name { get; } = "Harmony Anti-Phishing";

    public string Version { get; } = "1.0.0";

    public string Author { get; } = "Kotz#7299";

    public string Description { get; } = "cog_antiphishing_desc";

    // Localization files inside the "../Localization" folder
    public string? LocalizationDirectory { get; } = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)?.FullName ?? string.Empty, "Localization");

    public void RegisterArgumentConverters(CommandsNextExtension cmdHandler)
    {
        // This cog doesn't need argument converters.
    }

    public void RegisterCallbacks(DiscordShardedClient shardedClient)
    {
        var cmdHandler = shardedClient.ShardClients[0].GetCommandsNext();
        var antiphishingHandler = cmdHandler.Services.GetRequiredService<IAntiPhishingHandler>();

        shardedClient.MessageCreated += antiphishingHandler.FilterPhishingLinksAsync;
    }

    public void RegisterComponentResponses(IInteractionResponseManager responseGenerator)
    {
        // This cog doesn't have interactive messages.
    }

    public void RegisterServices(IServiceCollection ioc)
    {
        // This cog doesn't register services with factories or concrete implementations.
    }

    public void RegisterSlashCommands(SlashCommandsExtension slashHandler)
    {
        // This cog doesn't have slash commands.
    }
}