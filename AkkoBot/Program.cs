using AkkoBot.Common;
using AkkoBot.Core;
using Kotz.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Serilog;

namespace AkkoBot;

/// <summary>
/// The entry point of the application.
/// </summary>
internal sealed class Program
{
    private static async Task Main(string[] args)
    {
        Log.Logger = AkkoLogging.BaseLogBuilder.CreateLogger();
        var builder = Host.CreateEmptyApplicationBuilder(new() { Args = args });

        builder.Logging.AddSerilog();
        builder.Services
            .AddHostedService<Bot>()
            .RegisterServices()
            .AddSingleton(x => x.GetRequiredService<IConfigLoader>().LoadCredentials(AkkoEnvironment.CredsPath))
            .AddSingleton(x => x.GetRequiredService<IConfigLoader>().LoadConfig<BotConfig>(AkkoEnvironment.BotConfigPath));

        using var host = builder.Build();
        await host.RunAsync();

        await Log.CloseAndFlushAsync();
    }
}