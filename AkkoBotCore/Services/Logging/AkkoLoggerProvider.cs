using AkkoBot.Services.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AkkoBot.Services.Logging
{
    public class AkkoLoggerProvider : ILoggerProvider
    {
        private bool _isDisposed = false;
        private readonly LogLevel _minLogLevel;
        private readonly string _timeFormat;
        private readonly List<Type> _loggerRegister;
        private readonly IFileLogger _fileLogger;

        /// <summary>
        /// Creates a provider of <see cref="ILogger"/> objects.
        /// </summary>
        /// <param name="minLogLevel">Defines the minimum level of logging. Default is <see cref="LogLevel.Information"/>.</param>
        /// <param name="fileLogger">Defines the file logger for logging to a text file. Default is <see langword="null"/> for no logging.</param>
        /// <param name="logFormat">Defines the type of <see cref="ILogger"/> that should be created. Default is "Default".</param>
        public AkkoLoggerProvider(LogLevel minLogLevel, string timeFormat, IFileLogger fileLogger = null)
        {
            _minLogLevel = minLogLevel;
            _timeFormat = timeFormat;
            _loggerRegister = GeneralService.GetImplementables(typeof(ILogger)).ToList();
            _fileLogger = fileLogger;
        }

        /// <summary>
        /// Creates an <see cref="ILogger"/> instance whose name has been specified in <paramref name="categoryName"/>.
        /// </summary>
        /// <param name="categoryName">The name of the <see cref="ILogger"/> to be created.</param>
        /// <returns>An <see cref="ILogger"/> object, <see langword="null"/> if none was found.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            foreach (var logger in _loggerRegister)
            {
                if (logger.Name.Contains(categoryName, StringComparison.OrdinalIgnoreCase))
                    return Activator.CreateInstance(logger, _minLogLevel, _timeFormat, _fileLogger) as ILogger;
            }

            return null;
        }

        /// <summary>
        /// Releases the allocated resources for this logger provider.
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _fileLogger?.Dispose();
                    _loggerRegister.Clear();
                    _loggerRegister.TrimExcess();
                }

                _isDisposed = true;
            }
        }
    }
}