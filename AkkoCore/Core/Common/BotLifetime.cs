using AkkoCore.Core.Abstractions;
using System;
using System.Threading;

namespace AkkoCore.Core.Common
{
    /// <summary>
    /// Manages the shutdown of a <see cref="Bot"/>.
    /// </summary>
    internal record BotLifetime : IBotLifetime
    {
        private readonly CancellationTokenSource _cTokenSource;
        private bool _restartBot;

        public bool RestartBot
            => _cTokenSource.IsCancellationRequested && _restartBot;

        public BotLifetime(CancellationTokenSource cTokenSource)
            => _cTokenSource = cTokenSource;

        public void Shutdown()
        {
            _restartBot = false;
            _cTokenSource.Cancel();
        }

        public void Shutdown(TimeSpan time)
        {
            _restartBot = false;
            _cTokenSource.CancelAfter(time);
        }

        public void Restart()
        {
            _restartBot = true;
            _cTokenSource.Cancel();
        }

        public void Restart(TimeSpan time)
        {
            _restartBot = true;
            _cTokenSource.CancelAfter(time);
        }

        public void Dispose()
        {
            _cTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}