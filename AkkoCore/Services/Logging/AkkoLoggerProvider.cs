using AkkoCore.Config.Models;
using AkkoCore.Services.Logging.Abstractions;
using AkkoCore.Services.Logging.Loggers;
using Kotz.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace AkkoCore.Services.Logging;

/// <summary>
/// Creates and updates logger objects.
/// </summary>
public sealed class AkkoLoggerProvider : IAkkoLoggerProvider
{
    private bool _isDisposed = false;
    private IFileLogger? _fileLogger;
    private LogLevel _minLogLevel;
    private string? _logFormat;
    private string? _timeFormat;
    private readonly List<ILogger> _loggers = new();

    /// <summary>
    /// Creates a provider of <see cref="ILogger"/> objects.
    /// </summary>
    /// <param name="minLogLevel">Defines the minimum level of logging. Default is <see cref="LogLevel.Information"/>.</param>
    /// <param name="fileLogger">Defines the file logger for logging to a text file. Default is <see langword="null"/> for no logging.</param>
    /// <param name="logFormat">Defines the type of <see cref="ILogger"/> that should be created. Default is "Default".</param>
    /// <param name="timeFormat">The time format to be used on timestamps.</param>
    public AkkoLoggerProvider(LogLevel minLogLevel, IFileLogger? fileLogger = default, string? logFormat = default, string? timeFormat = default)
    {
        _minLogLevel = minLogLevel;
        _logFormat = logFormat;
        _timeFormat = timeFormat;
        _fileLogger = fileLogger;
    }

    public ILogger CreateLogger(string categoryName)
    {
        ILogger logger = (categoryName.EqualsAny(DbLoggerCategory.Database.Command.Name, "LinqToDB"))
            ? new DebugLogger(_minLogLevel, _fileLogger, _logFormat, _timeFormat)   // Log database queries as debug logs
            : new DefaultLogger(_minLogLevel, _fileLogger, _logFormat, _timeFormat);   // Log everything else as normal

        _loggers.Add(logger);
        return logger;
    }

    public void UpdateLoggers(LogConfig logConfig)
    {
        _minLogLevel = logConfig.LogLevel;
        _logFormat = logConfig.LogFormat;
        _timeFormat = logConfig.LogTimeFormat;

        foreach (AkkoLogger logger in _loggers)
            logger.UpdateConfig(logConfig);
    }

    public void UpdateFileLogger(IFileLogger? fileLogger)
    {
        _fileLogger = fileLogger;

        foreach (AkkoLogger logger in _loggers)
            logger.FileLogger = fileLogger;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
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