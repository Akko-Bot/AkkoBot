using Microsoft.Extensions.DependencyInjection;
using System;

namespace AkkoBot.Commands.Abstractions
{
    /// <summary>
    /// Represents a command service with a scoped service.
    /// </summary>
    public abstract class AkkoCommandService : ICommandService, IDisposable
    {
        private bool _isDisposed;

        /// <summary>
        /// Scope to fetch scoped services.
        /// </summary>
        protected IServiceScope Scope { get; private set; }

        /// <summary>
        /// Initializes a command service with a local scope.
        /// </summary>
        /// <param name="services">The IoC container.</param>
        protected AkkoCommandService(IServiceProvider services)
            => Scope = services.CreateScope();

        /// <summary>
        /// Method to free up the resources held by this object.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if it's called from <see cref="Dispose()"/>, <see langword="false"/> if it's called from the finalizer.</param>
        /// <remarks>Call the base in your override before executing your own logic.</remarks>
        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                    Scope?.Dispose();

                Scope = null;
                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}