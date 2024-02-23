namespace AkkoBot.Common;

/// <summary>
/// Groups data about the environment the bot is executing in.
/// </summary>
public static class AkkoEnvironment
{
    /// <summary>
    /// Gets the fully qualified path for the current directory of the application.
    /// </summary>
    public static string CurrentDirectory { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

    /// <summary>
    /// Gets the fully qualified path for the directory where the credentials file is stored.
    /// </summary>
    public static string ConfigDirectory { get; } = Path.Combine(CurrentDirectory, "Config");

    /// <summary>
    /// Gets the fully qualified path for the directory where the log files are stored.
    /// </summary>
    public static string LogsDirectory { get; } = Path.Combine(CurrentDirectory, "Logs");

    /// <summary>
    /// Gets the fully qualified path for the directory where the cog files are stored.
    /// </summary>
    public static string CogsDirectory { get; } = Path.Combine(CurrentDirectory, "Cogs");

    /// <summary>
    /// Gets the fully qualified path for the directory where the translated response strings are stored.
    /// </summary>
    public static string LocalesDirectory { get; } = Path.Combine(CurrentDirectory, "Localization");

    /// <summary>
    /// Gets the fully qualified path for the credentials file.
    /// </summary>
    public static string CredsPath { get; } = Path.Combine(ConfigDirectory, "credentials.yaml");

    /// <summary>
    /// Gets the fully qualified path for the bot's configuration file.
    /// </summary>
    public static string BotConfigPath { get; } = Path.Combine(ConfigDirectory, "bot_config.yaml");

    /// <summary>
    /// Gets the fully qualified path for the log configuration file.
    /// </summary>
    public static string LogConfigPath { get; } = Path.Combine(ConfigDirectory, "log_config.yaml");

    /// <summary>
    /// Gets the relative path from <see cref="CurrentDirectory"/> to <paramref name="path"/>.
    /// </summary>
    /// <param name="path">The specified path.</param>
    /// <returns>The relative path to <paramref name="path"/>.</returns>
    public static string GetRelativeAkkoPath(string path)
        => Path.GetRelativePath(CurrentDirectory, path);
}