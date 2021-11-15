using AkkoCore.Config.Abstractions;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;

namespace AkkoCore.Config.Models;

/// <summary>
/// Stores data and settings related to how the bot logs command usage.
/// </summary>
public sealed class LogConfig : Settings
{
    /// <summary>
    /// The minimum severity level of logs that should be registered.
    /// </summary>
    [YamlMember(Description = @"The minimum severity level of logs that should be registered. Defaults to ""Information"". Values: None, Error, Warning, Information, Debug")]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Defines how logs should be formatted.
    /// </summary>
    [YamlMember(Description = @"Defines how logs should be formatted. Defaults to ""Default"". Values: Minimalist, Simple, Default")]
    public string LogFormat { get; set; } = "Default";

    /// <summary>
    /// Defines the time format to be used in the logs.
    /// </summary>
    [YamlMember(Description = @"Defines the time format to be used in the logs. Leave it empty to use the default. Refer to: https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings")]
    public string? LogTimeFormat { get; set; }

    /// <summary>
    /// Defines time format to be used on log file names.
    /// </summary>
    [YamlMember(Description = @"Defines time format to be used on log file names. Leave it empty to use the default. Refer to: https://docs.microsoft.com/en-us/dotnet/standard/base-types/custom-date-and-time-format-strings")]
    public string? LogTimeStamp { get; set; }

    /// <summary>
    /// Determines whether logs should be written to a file.
    /// </summary>
    [YamlMember(Description = @"Determines whether logs should be written to a file. Defaults to ""false"". Values: true, false")]
    public bool IsLoggedToFile { get; set; } = false;

    /// <summary>
    /// Determines the maximum size of a log file, in megabytes.
    /// </summary>
    [YamlMember(Description = @"Determines the maximum size of a log file, in megabytes. Defaults to ""1.0"".")]
    public double LogSizeMb { get; set; } = 1.0;
}