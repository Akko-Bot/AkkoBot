using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using YamlDotNet.Serialization;

namespace AkkoBot.Core.Config.Models;

/// <summary>
/// Stores data and settings related to how the bot logs command usage.
/// </summary>
public sealed class LogConfig
{
    /// <summary>
    /// The minimum severity level of logs that should be registered.
    /// </summary>
    [YamlMember(Description = @"The minimum severity level of logs that should be registered. Defaults to ""Information"". Values: Verbose, Debug, Information, Warning, Error, Fatal")]
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;

    /// <summary>
    /// Defines how logs should be formatted.
    /// </summary>
    [YamlMember(Description = @"Defines how logs should be formatted. Defaults to ""Default"". Values: Minimalist, Simple, Default, or a custom Serilog template: https://github.com/serilog/serilog/wiki/Formatting-Output#formatting-plain-text")]
    public string LogTemplate { get; set; } = "Default";

    /// <summary>
    /// Defines how the message of command logs should be formatted.
    /// </summary>
    [YamlMember(Description = @"Defines how the message of command logs should be formatted. Defaults to ""Default"". Values: Minimalist, Simple, Default")] // TODO: documentation for custom template
    public string LogMessageTemplate { get; set; } = "Default";

    /// <summary>
    /// Determines whether logs should be written to a file.
    /// </summary>
    [YamlMember(Description = @"Determines whether logs should be written to a file. Defaults to ""false"". Values: true, false")]
    public bool IsLoggedToFile { get; set; } = false;

    /// <summary>
    /// Defines how frequently log files are saved.
    /// </summary>
    [YamlMember(Description = @"Defines how frequently log files are saved. Defaults to ""Day"". Values: Minute, Hour, Day, Month, Year, Infinite")]
    public RollingInterval LogFileSaveInterval { get; set; } = RollingInterval.Day;

    /// <summary>
    /// Determines the maximum size of a log file, in megabytes.
    /// </summary>
    [YamlMember(Description = @"Determines the maximum size of a log file, in megabytes. Defaults to ""0.0"" for no size limit.")]
    public double LogSizeMb { get; set; } = 0.0;
}