using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace AkkoBot.Config.Abstractions;

/// <summary>
/// Contains data and methods relevants for configuring a Serilog logger.
/// </summary>
public interface ILoggerLoader
{
    /// <summary>
    /// The theme used by the console sink.
    /// </summary>
    AnsiConsoleTheme ConsoleTheme { get; }

    /// <summary>
    /// The log template in use for the current session.
    /// </summary>
    string LogTemplate { get; }

    /// <summary>
    /// The message log template in use for the current session.
    /// </summary>
    string LogMessageTemplate { get; }

    /// <summary>
    /// Configures the specified <paramref name="logBuilder"/> with Serilog sinks used by the application.
    /// </summary>
    /// <param name="logBuilder">The log builder from Serilog.</param>
    /// <returns>The configured <paramref name="logBuilder"/>.</returns>
    LoggerConfiguration ConfigureLogger(LoggerConfiguration logBuilder);
}