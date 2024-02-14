using DSharpPlus.Commands.Trees;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AkkoBot.Core.Logging.Models;

/// <summary>
/// Defines all arguments for message log templates.
/// </summary>
public sealed partial record CommandLogArguments
{
    private static readonly Regex _argumentRegex = GenerateArgumentRegex();
    private static readonly PropertyInfo[] _classProperties = typeof(CommandLogArguments).GetProperties();

    /// <summary>
    /// The shard Id where the command was executed.
    /// </summary>
    public int ShardId { get; }

    /// <summary>
    /// The name of the user who executed the command.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// The Id of the user who executed the command.
    /// </summary>
    public ulong UserId { get; }

    /// <summary>
    /// The name of the guild the command was executed in.
    /// </summary>
    public string GuildName { get; }

    /// <summary>
    /// The Id of the guild the command was executed in.
    /// </summary>
    public ulong? GuildId { get; }

    /// <summary>
    /// The name of the channel the command was executed in.
    /// </summary>
    public string ChannelName { get; }

    /// <summary>
    /// The Id of the channel the command was executed in.
    /// </summary>
    public ulong ChannelId { get; }

    /// <summary>
    /// The command that was executed.
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// The exception that the command generated or
    /// <see cref="string.Empty"/> if there was no exception.
    /// </summary>
    public string CommandException { get; } = string.Empty;

    /// <summary>
    /// Groups all arguments for message log templates.
    /// </summary>
    /// <param name="context">The context the bot command.</param>
    /// <param name="exception">The exception thrown by the command.</param>
    public CommandLogArguments(CommandContext context, Exception exception) : this(context)
        => CommandException = Environment.NewLine + AnsiColor.Red + exception;

    /// <summary>
    /// Groups all arguments for message log templates.
    /// </summary>
    /// <param name="context">The context the bot command.</param>
    public CommandLogArguments(CommandContext context)
    {
        ShardId = context.Client.ShardId;
        Username = context.User.Username;
        UserId = context.User.Id;
        GuildName = context.Guild?.Name ?? "Private";
        GuildId = context.Guild?.Id;
        ChannelName = context.Channel.Name ?? "Private";
        ChannelId = context.Channel.Id;
        Command = $"{context.Command.Name} {string.Join(' ', context.Arguments.Select(x => x.Value))}";
    }

    /// <summary>
    /// Gets all log arguments for the specified message log template.
    /// </summary>
    /// <param name="logTemplate">The message log template.</param>
    /// <returns>A collection of values to be used in the template.</returns>
    public object?[] GetLogArguments(string logTemplate)
    {
        var matches = _argumentRegex.Matches(logTemplate)
            .Select(x => x.Groups[1].Value)
            .ToArray();

        return (matches.Length is 0)
            ? Array.Empty<object?>()
            : _classProperties
                .IntersectBy(matches, x => x.Name)
                .OrderBy(x => matches.IndexOf(x.Name))
                .Select(x => x.GetValue(this))
                .ToArray();
    }

    [GeneratedRegex(@"{(\w+)}", RegexOptions.IgnoreCase)]
    private static partial Regex GenerateArgumentRegex();
}