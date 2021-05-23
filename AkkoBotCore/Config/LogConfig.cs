using AkkoBot.Common;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Config
{
    /// <summary>
    /// Stores data and settings related to how the bot logs command usage.
    /// </summary>
    public class LogConfig : Settings
    {
        /// <summary>
        /// The minimum severity level of logs that should be registered.
        /// </summary>
        public LogLevel LogLevel { get; set; } = LogLevel.Information;

        /// <summary>
        /// The formatting to be used on logs.
        /// </summary>
        public string LogFormat { get; set; } = "Default";

        /// <summary>
        /// The time format to be used on timestamps.
        /// </summary>
        public string LogTimeFormat { get; set; }

        /// <summary>
        /// The time format to be used on log file names.
        /// </summary>
        public string LogTimeStamp { get; set; }

        /// <summary>
        /// Determines whether logs should be written to a file.
        /// </summary>
        public bool IsLoggedToFile { get; set; } = false;

        /// <summary>
        /// Determines the maximum size of a log file, in megabytes.
        /// </summary>
        public double LogSizeMb { get; set; } = 1.0;
    }
}