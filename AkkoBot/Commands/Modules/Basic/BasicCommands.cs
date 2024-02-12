using AkkoBot.Core.Services.Abstractions;
using DSharpPlus.Commands.Trees;
using DSharpPlus.Commands.Trees.Attributes;

namespace AkkoBot.Commands.Modules.Basic;

/// <summary>
/// Demo commands.
/// </summary>
public sealed class BasicCommands
{
    private readonly IBotLifetime _restartState;

    public BasicCommands(IBotLifetime restartState)
        => _restartState = restartState;

    [Command("ping")]
    public static async Task PingAsync(CommandContext context)
        => await context.RespondAsync($"{context.Client.Ping} ms");

    [Command("say")]
    public static async Task SayAsync(CommandContext context, string message)
        => await context.RespondAsync(message);

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