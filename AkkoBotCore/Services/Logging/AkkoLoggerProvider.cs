using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System;
using System.Linq;
using AkkoBot.Services.Logging.Abstractions;

namespace AkkoBot.Services.Logging
{
    public class AkkoLoggerProvider : ILoggerProvider
    {
        private bool _isDisposed = false;
        private readonly LogLevel _minLogLevel;
        private readonly string _timeFormat;
        private readonly List<Type> _loggerRegister;
        private readonly IAkkoFileLogger _fileLogger;

        public AkkoLoggerProvider(LogLevel minLogLevel, string timeFormat, IAkkoFileLogger fileLogger = null)
        {
            _minLogLevel = minLogLevel;
            _timeFormat = timeFormat;
            _loggerRegister = GeneralService.GetImplementables(typeof(ILogger)).ToList();
            _fileLogger = fileLogger;
        }

        public ILogger CreateLogger(string categoryName)
        {
            foreach (var logger in _loggerRegister)
            {
                if (logger.Name.Contains(categoryName, StringComparison.OrdinalIgnoreCase))
                    return Activator.CreateInstance(logger, _minLogLevel, _timeFormat, _fileLogger) as ILogger;
            }

            return null;
        }

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
