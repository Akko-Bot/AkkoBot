using System;
using System.Collections.Generic;
using AkkoBot.Services.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Services.Logging
{
    public class AkkoLoggerFactory : ILoggerFactory
    {
        private readonly List<ILoggerProvider> _providers = new();
        private readonly IFileLogger _fileLogger;
        public bool IsDisposed { get; private set; } = false;
        public LogLevel MinLogLevel { get; init; }
        public string TimeFormat { get; init; }
        public string LogFormat { get; init; }

        /// <summary>
        /// Creates a factory of <see cref="ILogger"/> objects.
        /// </summary>
        /// <param name="minLogLevel">Defines the minimum level of logging. Default is <see cref="LogLevel.Information"/>.</param>
        /// <param name="fileLogger">Defines the file logger for logging to a text file. Default is <see langword="null"/> for no logging.</param>
        /// <param name="logFormat">Defines the type of <see cref="ILogger"/> that should be created. Default is "Default".</param>
        /// <param name="timeFormat">Defines the format the timestamps should be printed on. Default is defined by the implementation of <see cref="ILogger"/>.</param>
        public AkkoLoggerFactory(LogLevel? minLogLevel = null, IFileLogger fileLogger = null, string logFormat = null, string timeFormat = null)
        {
            MinLogLevel = minLogLevel ?? LogLevel.Information;
            _fileLogger = fileLogger;
            LogFormat = (string.IsNullOrWhiteSpace(logFormat)) ? "Default" : logFormat;
            TimeFormat = timeFormat;
        }

        /// <summary>
        /// Registers an <see cref="ILogger"/> provider.
        /// </summary>
        /// <param name="provider">The provider to be registered on the factory.</param>
        public void AddProvider(ILoggerProvider provider)
            => _providers.Add(provider);

        /// <summary>
        /// Creates an <see cref="ILogger"/> instance whose name has been specified in <paramref name="categoryName"/>.
        /// </summary>
        /// <param name="categoryName">The name of the <see cref="ILogger"/> to be created.</param>
        /// <returns>An <see cref="ILogger"/> object.</returns>
        /// <exception cref="InvalidOperationException"/>
        public ILogger CreateLogger(string categoryName)
        {
            if (_providers.Count == 0)
                RegisterProviders();

            foreach (var provider in _providers)
            {
                var logger = provider.CreateLogger(LogFormat);

                if (logger is not null)
                    return logger;
            }

            throw new InvalidOperationException($"There is no logger named \"{LogFormat}\".");
        }

        /// <summary>
        /// Releases the allocated resources for this logger factory.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    foreach (var provider in _providers)
                        provider.Dispose();

                    _fileLogger?.Dispose();
                    _providers.Clear();
                    _providers.TrimExcess();
                }

                IsDisposed = true;
            }
        }

        /// <summary>
        /// Registers all objects that implement <see cref="ILoggerProvider"/> in the factory.
        /// </summary>
        private void RegisterProviders()
        {
            var providers = GeneralService.GetImplementables(typeof(ILoggerProvider));

            foreach (var provider in providers)
                _providers.Add(Activator.CreateInstance(provider, MinLogLevel, TimeFormat, _fileLogger) as ILoggerProvider);
        }
    }
}
