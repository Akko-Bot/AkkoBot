using AkkoBot.Core.Logging.Models;

namespace AkkoBot.Common;

/// <summary>
/// Groups constants that are used for logging.
/// </summary>
public static class AkkoLogging
{
    /// <summary>
    /// The default log template.
    /// </summary>
    public const string DefaultLogTemplate = "{Level:w4}: [{Timestamp}] [{SourceContext}] {Message:l}{NewLine}{Exception}";

    /// <summary>
    /// The simple log template.
    /// </summary>
    public const string SimpleLogTemplate = "{Level:w4}: [{Timestamp:HH:mm}] [{SourceContext}] {Message:l}{NewLine}{Exception}";

    /// <summary>
    /// The minimalist log template.
    /// </summary>
    public const string MinimalistLogTemplate = "{Level:w4}: [{Timestamp:HH:mm}] {Message:l}{NewLine}{Exception}";

    /// <summary>
    /// Default log message template.
    /// </summary>
    public const string DefaultLogMessageTemplate =
        $$"""
        [Shard {{{nameof(CommandLogArguments.ShardId)}}}]
              User: {{{nameof(CommandLogArguments.Username)}}} [{{{nameof(CommandLogArguments.UserId)}}}]
              Server: {{{nameof(CommandLogArguments.GuildName)}}} [{{{nameof(CommandLogArguments.GuildId)}}}]
              Channel: #{{{nameof(CommandLogArguments.ChannelName)}}} [{{{nameof(CommandLogArguments.ChannelId)}}}]
              Command: {{{nameof(CommandLogArguments.Command)}}}{{{nameof(CommandLogArguments.CommandException)}}}
        """;

    /// <summary>
    /// Simple log message template.
    /// </summary>
    public const string SimpleLogMessageTemplate =
        $"[{{{nameof(CommandLogArguments.ShardId)}}}] | " +
        $"g: {{{nameof(CommandLogArguments.GuildId)}}} | " +
        $"c: {{{nameof(CommandLogArguments.ChannelId)}}} | " +
        $"u: {{{nameof(CommandLogArguments.UserId)}}} | " +
        $"msg: {{{nameof(CommandLogArguments.Command)}}}{{{nameof(CommandLogArguments.CommandException)}}}";

    /// <summary>
    /// Minimalist log message template.
    /// </summary>
    public const string MinimalistLogMessageTemplate =
        $"[{{{nameof(CommandLogArguments.ShardId)}}}] | " +
        $"{{{nameof(CommandLogArguments.GuildName)}}} | " +
        $"#{{{nameof(CommandLogArguments.ChannelName)}}} | " +
        $"{{{nameof(CommandLogArguments.Username)}}}: " +
        $"{{{nameof(CommandLogArguments.Command)}}}{{{nameof(CommandLogArguments.CommandException)}}}";
}