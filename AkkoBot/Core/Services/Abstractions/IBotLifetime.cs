namespace AkkoBot.Core.Services.Abstractions;

/// <summary>
/// Manages the shutdown of a <see cref="Bot"/>.
/// </summary>
public interface IBotLifetime
{
    /// <summary>
    /// Determines whether the bot should be restarted when
    /// <see cref="StopToken"/> is cancelled.
    /// </summary>
    bool IsRestarting { get; }

    /// <summary>
    /// The cancellation token for when the bot should stop running.
    /// </summary>
    CancellationToken StopToken { get; }

    /// <summary>
    /// Terminates the bot.
    /// </summary>
    void StopApplication();

    /// <summary>
    /// Restarts the bot.
    /// </summary>
    void RestartApplication();
}