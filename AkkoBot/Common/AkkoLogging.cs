using AkkoBot.Events.Logging.Services;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using System.Globalization;

namespace AkkoBot.Common;

/// <summary>
/// Contains logging-related objects and methods.
/// </summary>
public static class AkkoLogging
{
    private const string _logTemplate = "{Level:w4}: [{Timestamp}] [{SourceContext}] {Message:l}{NewLine}";

    private readonly static AnsiConsoleTheme _consoleTheme = new(
        new Dictionary<ConsoleThemeStyle, string>()
        {
            [ConsoleThemeStyle.Text] = string.Empty,
            [ConsoleThemeStyle.SecondaryText] = string.Empty,
            [ConsoleThemeStyle.TertiaryText] = string.Empty,
            [ConsoleThemeStyle.Invalid] = string.Empty,
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

    /// <summary>
    /// The base Serilog log builder.
    /// </summary>
    public static LoggerConfiguration BaseLogBuilder { get; } = new LoggerConfiguration()
        .Enrich.With<SourceContextEnricher>()
        .WriteTo.Console(outputTemplate: _logTemplate, formatProvider: CultureInfo.InvariantCulture, theme: _consoleTheme);
}