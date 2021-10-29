using AkkoCore.Core.Abstractions;
using AkkoCore.Models.EventArgs;
using Emzi0767.Utilities;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AkkoCore.Core.Common
{
    /// <summary>
    /// Manages the shutdown of a <see cref="Bot"/>.
    /// </summary>
    internal sealed record BotLifetime : IBotLifetime
    {
        private readonly CancellationTokenSource _cTokenSource;
        private readonly AsyncEvent<IBotLifetime, ShutdownEventArgs> _onShutdownEvent;
        private bool _restartBot;

        public bool RestartBot
            => _cTokenSource.IsCancellationRequested && _restartBot;

        public event AsyncEventHandler<IBotLifetime, ShutdownEventArgs> OnShutdown
        {
            add => _onShutdownEvent.Register(value);
            remove => _onShutdownEvent.Unregister(value);
        }

        public BotLifetime(CancellationTokenSource cTokenSource)
        {
            _cTokenSource = cTokenSource;
            _onShutdownEvent = new("SHUTDOWN", TimeSpan.FromSeconds(2), ExceptionHandler);
        }

        public void Shutdown()
        {
            _restartBot = false;
            
            _ = _onShutdownEvent.InvokeAsync(this, GetEventArguments(TimeSpan.Zero, _restartBot));
            _cTokenSource.Cancel();
        }

        public void Shutdown(TimeSpan time)
        {
            _restartBot = false;

            _ = _onShutdownEvent.InvokeAsync(this, GetEventArguments(time, _restartBot));
            _cTokenSource.CancelAfter(time);
        }

        public void Restart()
        {
            _restartBot = true;

            _ = _onShutdownEvent.InvokeAsync(this, GetEventArguments(TimeSpan.Zero, _restartBot));
            _cTokenSource.Cancel();
        }

        public void Restart(TimeSpan time)
        {
            _restartBot = true;

            _ = _onShutdownEvent.InvokeAsync(this, GetEventArguments(time, _restartBot));
            _cTokenSource.CancelAfter(time);
        }

        public void Dispose()
        {
            _cTokenSource?.Dispose();
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Get the event arguments for the <see cref="OnShutdown"/> event.
        /// </summary>
        /// <param name="time">How long should it take for the shutdown to be performed.</param>
        /// <param name="isRestarting">Defines whether the bot is going to start after shutdown.</param>
        /// <returns>The shutdown event arguments.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ShutdownEventArgs GetEventArguments(TimeSpan time, bool isRestarting)
            => new(DateTimeOffset.Now, time, isRestarting);

        /// <summary>
        /// Handles exceptions that occurs on <see cref="_onShutdownEvent"/>.
        /// </summary>
        private void ExceptionHandler<TArgs>(
            AsyncEvent<IBotLifetime, TArgs> asyncEvent,
            Exception ex,
            AsyncEventHandler<IBotLifetime, TArgs> handler,
            IBotLifetime sender,
            TArgs eventArgs) where TArgs : AsyncEventArgs
        {
            if (ex is AsyncEventTimeoutException)
            {
                Console.WriteLine(
                    $"An event handler for {asyncEvent.Name} took too long to execute. " +
                    $"Defined as \"{handler.Method.ToString()?.Replace(handler.Method.ReturnType.ToString(), string.Empty).TrimStart()}\" " +
                    $"located in \"{handler.Method.DeclaringType}\"."
                );
            }
            else
            {
                Console.WriteLine(
                    $"Event handler exception for event {asyncEvent.Name} thrown from {handler.Method} " +
                    $"(defined in {handler.Method.DeclaringType})"
                );
            }
        }
    }
}