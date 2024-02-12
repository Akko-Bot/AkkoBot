using AkkoBot.Common;
using AkkoBot.Config;
using AkkoBot.Core;
using AkkoBot.Core.Services;
using AkkoBot.Core.Services.Abstractions;

namespace AkkoBot;

/// <summary>
/// The entry point of the application.
/// </summary>
internal sealed class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new() { Args = args });

        builder.Logging
            .AddSimpleConsole()
            .AddDebug();

        builder.Services
            .AddHostedService<Bot>()
            .AddSingleton<IBotLifetime, BotLifetime>()
            .AddSingleton<IConfigLoader, ConfigLoader>()
            .AddSingleton(x => x.GetRequiredService<ConfigLoader>().LoadCredentials(AkkoEnvironment.CredsPath))
            .AddSingleton(x => x.GetRequiredService<ConfigLoader>().LoadConfig<BotConfig>(AkkoEnvironment.BotConfigPath));

        using var host = builder.Build();
        await host.RunAsync();
    }
}