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

    public int ShardId { get; }
    public string Username { get; }
    public ulong UserId { get; }
    public string GuildName { get; }
    public ulong? GuildId { get; }
    public string ChannelName { get; }
    public ulong ChannelId { get; }
    public string Command { get; }
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