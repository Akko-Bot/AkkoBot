using AkkoCore.Config.Models;
using Microsoft.Extensions.Logging;
using System;

namespace AkkoCore.Services.Logging.Abstractions;

/// <summary>
/// Represents an object that logs bot events.
/// </summary>
public abstract class AkkoLogger : ILogger
{
    /// <summary>
    /// The object responsible for saving log data to a file.
    /// </summary>
    /// <remarks>Set it to <see langword="null"/> to disable file logging.</remarks>
    public IFileLogger? FileLogger { get; set; }

    /// <summary>
    /// Dummy object used for locking purposes.
    /// </summary>
    protected static object LockObject { get; } = new();

    /// <summary>
    /// Updates the internal configuration of this logger.
    /// </summary>
    /// <param name="logConfig">The log settings.</param>
    public abstract void UpdateConfig(LogConfig logConfig);

    public abstract bool IsEnabled(LogLevel logLevel);

    public abstract void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter);

    /// <summary>
    /// This method just returns <see langword="null"/> for <see cref="AkkoLogger"/>.
    /// It may return something else on derived types.
    /// </summary>
    /// <typeparam name="TState">The type of scope.</typeparam>
    /// <param name="state">The metadata to be passed to the logger.</param>
    /// <returns>A disposable object or <see langword="null"/> if the logger doesn't support this operation.</returns>
    public virtual IDisposable BeginScope<TState>(TState state) where TState : notnull
        => default!;

    /// <summary>
    /// Changes the text color of the console according to the specified log level.
    /// </summary>
    /// <param name="logLevel">The level of the current log.</param>
    protected virtual void ChangeConsoleTextColor(LogLevel logLevel)
    {
        Console.ForegroundColor = logLevel switch
        {
            LogLevel.Trace => ConsoleColor.Gray,
            LogLevel.Debug => ConsoleColor.DarkGreen,
            LogLevel.Warning => ConsoleColor.DarkMagenta,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.Black,
            _ => Console.ForegroundColor
        };

        if (logLevel is LogLevel.Critical)
            Console.BackgroundColor = ConsoleColor.Red;
    }
}