using AkkoBot.Config.Abstractions;
using AkkoBot.Config.Models;
using AkkoBot.Core;
using Kotz.DependencyInjection.Extensions;
using Serilog;

namespace AkkoBot;

/// <summary>
/// The entry point of the application.
/// </summary>
internal sealed class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateEmptyApplicationBuilder(new() { Args = args });
        builder.Services
            .AddHostedService<Bot>()
            .RegisterServices()
            .AddSingleton(x => x.GetRequiredService<IConfigLoader>().LoadCredentials(AkkoEnvironment.CredsPath))
            .AddSingleton(x => x.GetRequiredService<IConfigLoader>().LoadConfig<BotConfig>(AkkoEnvironment.BotConfigPath))
            .AddSingleton(x => x.GetRequiredService<IConfigLoader>().LoadConfig<LogConfig>(AkkoEnvironment.LogConfigPath))
            .AddSerilog((ioc, logBuilder) => ioc.GetRequiredService<ILoggerLoader>().ConfigureLogger(logBuilder));

        using var host = builder.Build();
        await host.RunAsync();

        await Log.CloseAndFlushAsync();
    }
}