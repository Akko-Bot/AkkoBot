﻿using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Logging.Abstractions;
using AkkoBot.Services.Logging.Loggers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace AkkoBot.Services.Logging
{
    /// <summary>
    /// Creates and updates logger objects.
    /// </summary>
    public class AkkoLoggerProvider : ILoggerProvider
    {
        private bool _isDisposed = false;
        private IFileLogger _fileLogger;
        private LogLevel _minLogLevel;
        private string _logFormat;
        private string _timeFormat;
        private readonly List<ILogger> _loggers = new();

        /// <summary>
        /// Creates a provider of <see cref="ILogger"/> objects.
        /// </summary>
        /// <param name="minLogLevel">Defines the minimum level of logging. Default is <see cref="LogLevel.Information"/>.</param>
        /// <param name="fileLogger">Defines the file logger for logging to a text file. Default is <see langword="null"/> for no logging.</param>
        /// <param name="logFormat">Defines the type of <see cref="ILogger"/> that should be created. Default is "Default".</param>
        public AkkoLoggerProvider(LogLevel minLogLevel, string logFormat, string timeFormat, IFileLogger fileLogger = null)
        {
            _minLogLevel = minLogLevel;
            _logFormat = logFormat;
            _timeFormat = timeFormat;
            _fileLogger = fileLogger;
        }

        /// <summary>
        /// Creates an <see cref="ILogger"/> instance for the namespace specified in <paramref name="categoryName"/>.
        /// </summary>
        /// <param name="categoryName">Fullname of the namespace to create a log for.</param>
        /// <returns>An <see cref="ILogger"/>.</returns>
        public ILogger CreateLogger(string categoryName)
        {
            ILogger logger = (categoryName.Equals(DbLoggerCategory.Database.Command.Name))
                ? new DebugLogger(_minLogLevel, _fileLogger, _logFormat, _timeFormat)   // Log EF Core queries as debug logs
                : new AkkoLogger(_minLogLevel, _fileLogger, _logFormat, _timeFormat);   // Log everything else as normal

            _loggers.Add(logger);
            return logger;
        }

        /// <summary>
        /// Updates the loggers with the specified settings.
        /// </summary>
        /// <param name="logConfig">The log settings.</param>
        public void UpdateLoggers(LogConfigEntity logConfig)
        {
            _minLogLevel = logConfig.LogLevel;
            _logFormat = logConfig.LogFormat;
            _timeFormat = logConfig.LogTimeFormat;
            
            foreach (var logger in _loggers)
                logger.BeginScope(logConfig);
        }

        /// <summary>
        /// Updates the file loggers with a new instance.
        /// </summary>
        /// <param name="fileLogger">The new file logger, <see langword="null"/> to disable file logging.</param>
        public void UpdateFileLogger(IFileLogger fileLogger)
        {
            _fileLogger = fileLogger;
            _loggers[0].BeginScope(null)?.Dispose();

            if (fileLogger is null)
                return;

            foreach (var logger in _loggers)
                logger.BeginScope(fileLogger);
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
                    _loggers?.Clear();
                    _fileLogger?.Dispose();
                }

                _isDisposed = true;
            }
        }
    }
}