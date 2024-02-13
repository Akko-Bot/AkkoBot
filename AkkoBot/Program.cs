using AkkoBot.Common;
using AkkoBot.Core;
using Kotz.DependencyInjection.Extensions;

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
            .RegisterServices()
            .AddSingleton(x => x.GetRequiredService<IConfigLoader>().LoadCredentials(AkkoEnvironment.CredsPath))
            .AddSingleton(x => x.GetRequiredService<IConfigLoader>().LoadConfig<BotConfig>(AkkoEnvironment.BotConfigPath));

        using var host = builder.Build();
        await host.RunAsync();
    }
}