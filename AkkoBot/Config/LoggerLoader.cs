using AkkoBot.Config.Abstractions;
using AkkoBot.Config.Models;
using AkkoBot.Events.Logging.Services;
using Kotz.DependencyInjection;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System.Globalization;

namespace AkkoBot.Config;

/// <summary>
/// Contains data and methods relevants for configuring a Serilog logger for Akko.
/// </summary>
[Service<ILoggerLoader>(ServiceLifetime.Singleton)]
internal sealed class LoggerLoader : ILoggerLoader
{
    private readonly LogConfig _config;

    /// <inheritdoc />
    public AnsiConsoleTheme ConsoleTheme { get; } = new(
        new Dictionary<ConsoleThemeStyle, string>()
        {
            [ConsoleThemeStyle.Text] = string.Empty,
            [ConsoleThemeStyle.SecondaryText] = string.Empty,
            [ConsoleThemeStyle.TertiaryText] = string.Empty,
            [ConsoleThemeStyle.Invalid] = AnsiColor.Red,
            [ConsoleThemeStyle.Null] = string.Empty,
            [ConsoleThemeStyle.Name] = string.Empty,
            [ConsoleThemeStyle.String] = string.Empty,
            [ConsoleThemeStyle.Number] = string.Empty,
            [ConsoleThemeStyle.Boolean] = string.Empty,
            [ConsoleThemeStyle.Scalar] = string.Empty,
            [ConsoleThemeStyle.LevelVerbose] = AnsiColor.Gray,
            [ConsoleThemeStyle.LevelDebug] = AnsiColor.Green,
            [ConsoleThemeStyle.LevelInformation] = AnsiColor.Blue,
            [ConsoleThemeStyle.LevelWarning] = AnsiColor.Purple,
            [ConsoleThemeStyle.LevelError] = AnsiColor.Red,
            [ConsoleThemeStyle.LevelFatal] = AnsiColor.WhiteOnRed
        }
    );

    /// <inheritdoc />
    public string LogTemplate { get; }

    /// <inheritdoc />
    public string LogMessageTemplate { get; }

    /// <summary>
    /// Contains data and methods relevants for configuring a Serilog logger for Akko.
    /// </summary>
    /// <param name="config">The log settings.</param>
    public LoggerLoader(LogConfig config)
    {
        _config = config;

        LogTemplate = config.LogTemplate.ToLowerInvariant() switch
        {
            "default" => AkkoLogging.DefaultLogTemplate,
            "simple" => AkkoLogging.SimpleLogTemplate,
            "minimalist" => AkkoLogging.MinimalistLogTemplate,
            _ => config.LogTemplate
        };

        LogMessageTemplate = config.LogMessageTemplate.ToLowerInvariant() switch
        {
            "default" => AkkoLogging.DefaultLogMessageTemplate,
            "simple" => AkkoLogging.SimpleLogMessageTemplate,
            "minimalist" => AkkoLogging.MinimalistLogMessageTemplate,
            _ => config.LogMessageTemplate
        };
    }

    /// <inheritdoc />   
    public LoggerConfiguration ConfigureLogger(LoggerConfiguration logBuilder)
    {
        logBuilder
            .Enrich.With<SourceContextEnricher>()
            .WriteTo.Console(outputTemplate: LogTemplate, formatProvider: CultureInfo.InvariantCulture, theme: ConsoleTheme);

        if (_config.IsLoggedToFile)
            logBuilder.WriteTo.File(Path.Join(AkkoEnvironment.LogsDirectory, "AkkoBot_.txt"), outputTemplate: LogTemplate, rollingInterval: RollingInterval.Day);

        return logBuilder;
    }
}