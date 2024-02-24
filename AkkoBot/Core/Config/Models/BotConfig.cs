using YamlDotNet.Serialization;

namespace AkkoBot.Core.Config.Models;

/// <summary>
/// Stores settings related to the bot.
/// </summary>
public sealed class BotConfig
{
    private string _locale = AkkoConstants.DefaultLanguage;
    private string _okColor = "007FFF";
    private string _errorColor = "FB3D28";
    private string _defaultHelpMessage = string.Empty;

    /// <summary>
    /// Groups the qualified name of commands that have been disabled globally.
    /// </summary>
    [YamlMember(Description = @"Groups the qualified name of commands that have been disabled globally.")]
    public ConcurrentHashSet<string> DisabledCommands { get; init; } = [];

    /// <summary>
    /// The default bot locale.
    /// </summary>
    [YamlMember(Description = $@"The default bot locale. Defaults to ""{AkkoConstants.DefaultLanguage}"".")]
    public string Locale
    {
        get => _locale;
        set => _locale = value.MaxLength(10);
    }

    /// <summary>
    /// The default color for embeds, in hexadecimal.
    /// </summary>
    [YamlMember(Description = @"The default color for embeds, in hexadecimal. Defaults to ""007FFF"".")]
    public string OkColor
    {
        get => _okColor;
        set => _okColor = value.MaxLength(6).ToUpperInvariant();
    }

    /// <summary>
    /// The default color for error embeds, in hexadecimal.
    /// </summary>
    [YamlMember(Description = @"The default error color for embeds, in hexadecimal. Defaults to ""FB3D28"".")]
    public string ErrorColor
    {
        get => _errorColor;
        set => _errorColor = value.MaxLength(6).ToUpperInvariant();
    }

    /// <summary>
    /// The default bot prefix.
    /// </summary>
    [YamlMember(Description = @"The default bot prefix. Defaults to ""!"".")]
    public string Prefix { get; set; } = "!";

    /// <summary>
    /// Defines whether the bot responds to commands in direct message.
    /// </summary>
    [YamlMember(Description = @"Defines whether the bot responds to commands in direct message. Defaults to ""true"". Values: true, false")]
    public bool RespondToDms { get; set; } = true;

    /// <summary>
    /// Defines whether the bot responds to commands prefixed with a mention to the bot.
    /// </summary>
    [YamlMember(Description = @"Defines whether the bot responds to commands prefixed with a mention to the bot. Defaults to ""false"". Values: true, false")]
    public bool MentionPrefix { get; set; } = true;

    /// <summary>
    /// Defines whether the bot should respond to help commands.
    /// </summary>
    [YamlMember(Description = @"Defines whether the bot should respond to help commands. Defaults to ""true"". Values: true, false")]
    public bool EnableHelp { get; set; } = true;

    /// <summary>
    /// Defines whether the bot responds to help commands with no parameter.
    /// </summary>
    [YamlMember(Description = @"Defines whether the bot responds to help commands with no parameter. Defaults to ""true"". Values: true, false")]
    public bool EnableDefaultHelpMessage { get; set; } = true;

    /// <summary>
    /// Defines whether commands are case sensitive or not.
    /// </summary>
    [YamlMember(Description = @"Defines whether commands are case sensitive or not. Defaults to ""true"". Values: true, false")]
    public bool CaseSensitiveCommands { get; set; } = true;

    /// <summary>
    /// Defines the message cache size for every <see cref="DSharpPlus.DiscordClient"/>.
    /// </summary>
    [YamlMember(Description = @"Defines the message cache size for every Discord client. Defaults to ""200"".")]
    public int MessageSizeCache { get; set; } = 200;

    /// <summary>
    /// Defines the maximum amount of time that an interactive command waits for user input.
    /// </summary>
    [YamlMember(Description = @"Defines the maximum amount of time that an interactive command waits for user input. Defaults to ""00:01:00"".")]
    public TimeSpan? InteractiveTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Defines the message to be sent when the help command is used without parameters.
    /// </summary>
    /// <remarks>Set it to <see cref="string.Empty"/> to use the built-in default help message.</remarks>
    [YamlMember(Description = @"Defines the message to be sent when the help command is used without parameters. Default is '' (uses the default Akko help message).")]
    public string DefaultHelpMessage
    {
        get => _defaultHelpMessage;
        set => _defaultHelpMessage = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
    }

    /// <summary>
    /// Defines the Id of the guild to be used for debugging the bot.
    /// </summary>
    /// <remarks>The value is 0 if no guild should be defined for debugging.</remarks>
    [YamlMember(Description = @"Defines the Id of the Discord server to be used for debugging the bot. Defaults to ""0"" for no debug server.")]
    public ulong DebugGuildId { get; set; } = 0;
}