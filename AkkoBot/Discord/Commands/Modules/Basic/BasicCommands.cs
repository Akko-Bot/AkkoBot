using AkkoBot.Core.Services.Abstractions;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Commands.Trees.Attributes;

namespace AkkoBot.Discord.Commands.Modules.Basic;

/// <summary>
/// Demo commands.
/// </summary>
internal sealed class BasicCommands
{
    private readonly IBotLifetime _restartState;

    /// <summary>
    /// Demo commands
    /// </summary>
    /// <param name="restartState">The bot lifetime.</param>
    public BasicCommands(IBotLifetime restartState)
        => _restartState = restartState;

    [Command("ping")]
    public static async Task PingAsync(CommandContext context)
        => await context.RespondAsync($"{context.Client.Ping} ms");

    [Command("say")]
    public static async Task SayAsync(CommandContext context, string message)
        => await context.RespondAsync(message);

    [Command("error")]
    public static Task SayAsync(CommandContext context)
        => throw new InvalidOperationException("This is a command that failed.");

    [Command("die")]
    public async Task DieAsync(CommandContext context)
    {
        await context.Client.DisconnectAsync();
        _restartState.StopApplication();
    }

    [Command("restart")]
    public async Task RestartAsync(CommandContext context)
    {
        await context.Client.DisconnectAsync();
        _restartState.RestartApplication();
    }
}