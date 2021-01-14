using System;
using System.Collections.Generic;
using AkkoBot.Services.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Services.Logging
{
    public class AkkoLoggerFactory : ILoggerFactory
    {
        private readonly List<ILoggerProvider> _providers = new();
        private readonly IAkkoFileLogger _fileLogger;
        public bool IsDisposed { get; private set; } = false;
        public LogLevel MinLogLevel { get; init; }
        public string TimeFormat { get; init; }
        public string LogFormat { get; init; }

        public AkkoLoggerFactory(LogLevel? minLogLevel = null, IAkkoFileLogger fileLogger = null, string logFormat = null, string timeFormat = null)
        {
            MinLogLevel = minLogLevel ?? LogLevel.Information;
            _fileLogger = fileLogger;
            LogFormat = (string.IsNullOrWhiteSpace(logFormat)) ? "Default" : logFormat;
            TimeFormat = timeFormat;
        }

        public void AddProvider(ILoggerProvider provider)
        {
            _providers.Add(provider);
        }

        private void RegisterProviders()
        {
            var providers = GeneralService.GetImplementables(typeof(ILoggerProvider));

            foreach (var provider in providers)
                _providers.Add(Activator.CreateInstance(provider, MinLogLevel, TimeFormat, _fileLogger) as ILoggerProvider);
        }

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

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            foreach (var provider in _providers)
                provider.Dispose();

            _fileLogger?.Dispose();
            _providers.Clear();
            _providers.TrimExcess();
            GC.SuppressFinalize(this);
        }
    }
}
