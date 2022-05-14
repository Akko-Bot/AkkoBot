using AkkoCore.Commands.Attributes;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events;

/// <summary>
/// Handles log messages when a command is executed.
/// </summary>
[CommandService<ICommandLogHandler>(ServiceLifetime.Singleton)]
internal sealed class CommandLogHandler : ICommandLogHandler
{
    public Task LogCmdExecutionAsync(CommandsNextExtension cmdHandler, CommandExecutionEventArgs eventArgs)
    {
        cmdHandler.Client.Logger.LogCommand(LogLevel.Information, eventArgs.Context);
        return Task.CompletedTask;
    }

    public Task LogCmdErrorAsync(CommandsNextExtension cmdHandler, CommandErrorEventArgs eventArgs)
    {
        if (eventArgs.Exception
            is ArgumentException            // Ignore commands with invalid arguments and subcommands that do not exist
            or CommandNotFoundException     // Ignore commands that do not exist
            or InvalidOperationException)   // Ignore groups that are not commands themselves
            return Task.CompletedTask;

        eventArgs.Handled = true;

        if (eventArgs.Exception is ChecksFailedException ex)
        {
            if (ex.Context.Member is null || ex.Context.Member.PermissionsIn(ex.Context.Channel).HasFlag(Permissions.AddReactions))
                ex.Context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);

            return Task.CompletedTask;
        }

        //Log common errors
        cmdHandler.Client.Logger.LogCommand(
            LogLevel.Error,
            eventArgs.Context,
            string.Empty,
            eventArgs.Exception
        );

        return Task.CompletedTask;
    }

    public Task LogCmdCooldownAsync(CommandsNextExtension cmdHandler, CommandErrorEventArgs eventArgs)
    {
        if (eventArgs.Exception is not ChecksFailedException ex || !ex.FailedChecks.Any(x => x.GetType() == typeof(GlobalCooldownAttribute)))
            return Task.CompletedTask;

        // Log command cooldowns
        cmdHandler.Client.Logger.LogCommand(
            LogLevel.Warning,
            eventArgs.Context,
            "Command execution has been cancelled due to an active cooldown."
        );

        eventArgs.Handled = true;

        // React with a cooldown emoji
        return eventArgs.Context.Message.CreateReactionAsync(AkkoStatics.CooldownEmoji);
    }

    public Task LogSlashCmdExecutionAsync(SlashCommandsExtension slashHandler, SlashCommandExecutedEventArgs eventArgs)
    {
        slashHandler.Client.Logger.LogCommand(LogLevel.Information, eventArgs.Context);
        return Task.CompletedTask;
    }

    public Task LogSlashCmdErrorAsync(SlashCommandsExtension slashHandler, SlashCommandErrorEventArgs eventArgs)
    {
        if (eventArgs.Exception is not SlashExecutionChecksFailedException)
            slashHandler.Client.Logger.LogCommand(LogLevel.Error, eventArgs.Context, string.Empty, eventArgs.Exception);

        return Task.CompletedTask;
    }
}