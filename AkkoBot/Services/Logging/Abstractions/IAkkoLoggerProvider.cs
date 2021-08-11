using AkkoCore.Config;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Services.Logging.Abstractions
{
    /// <summary>
    /// Represents an object that creates and updates <see cref="ILogger"/> objects from database settings.
    /// </summary>
    public interface IAkkoLoggerProvider : ILoggerProvider
    {
        /// <summary>
        /// Updates the file logger of all loggers registered in this provider with the specified instance.
        /// </summary>
        /// <param name="fileLogger">The new file logger, <see langword="null"/> to disable file logging.</param>
        /// <remarks>Pass in <see langword="null"/> to disable file logging.</remarks>
        void UpdateFileLogger(IFileLogger fileLogger);

        /// <summary>
        /// Updates the loggers registered in this provider with the specified settings.
        /// </summary>
        /// <param name="logConfig">The log settings.</param>
        void UpdateLoggers(LogConfig logConfig);
    }
}