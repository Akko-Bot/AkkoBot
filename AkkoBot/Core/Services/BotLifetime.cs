using AkkoBot.Core.Services.Abstractions;
using Kotz.DependencyInjection;

namespace AkkoBot.Core.Services;

/// <summary>
/// Manages the shutdown of a <see cref="Bot"/>.
/// </summary>
[Service<IBotLifetime>(ServiceLifetime.Singleton)]
internal sealed class BotLifetime : IBotLifetime, IDisposable
{
    private CancellationTokenSource _restartTokenSource = new();
    private readonly IHostApplicationLifetime _lifetime;

    /// <inheritdoc />
    public bool IsRestarting { get; private set; }

    /// <inheritdoc />
    public CancellationToken StopToken
        => _restartTokenSource.Token;

    /// <summary>
    /// Manages the shutdown of a <see cref="Bot"/>.
    /// </summary>
    /// <param name="lifetime">The host app lifetime.</param>
    public BotLifetime(IHostApplicationLifetime lifetime)
        => _lifetime = lifetime;

    /// <inheritdoc />
    public void StopApplication()
    {
        IsRestarting = false;
        _restartTokenSource.Cancel();
        _lifetime.StopApplication();
    }

    /// <inheritdoc />
    public void RestartApplication()
    {
        using var oldToken = _restartTokenSource;
        _restartTokenSource = new();
        IsRestarting = true;

        oldToken.Cancel();
    }

    /// <inheritdoc />
    public void Dispose()
        => _restartTokenSource.Dispose();
}