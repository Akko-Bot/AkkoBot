﻿using AkkoBot.Commands.Attributes;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Events.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Services.Events
{
    /// <summary>
    /// Handles log messages when a command is executed.
    /// </summary>
    internal class CommandLogHandler : ICommandLogHandler
    {
        public Task LogCmdExecutionAsync(CommandsNextExtension cmdHandler, CommandExecutionEventArgs eventArgs)
        {
            cmdHandler.Client.Logger.LogCommand(LogLevel.Information, eventArgs.Context);
            return Task.CompletedTask;
        }

        public Task LogCmdErrorAsync(CommandsNextExtension cmdHandler, CommandErrorEventArgs eventArgs)
        {
            if (eventArgs.Exception
                is not ArgumentException             // Ignore commands with invalid arguments and subcommands that do not exist
                and not ChecksFailedException        // Ignore command check fails
                and not CommandNotFoundException     // Ignore commands that do not exist
                and not InvalidOperationException)   // Ignore groups that are not commands themselves
            {
                //Log common errors
                cmdHandler.Client.Logger.LogCommand(
                    LogLevel.Error,
                    eventArgs.Context,
                    string.Empty,
                    eventArgs.Exception
                );
            }
            else if (eventArgs.Exception is ChecksFailedException ex && ex.FailedChecks[0].GetType() == typeof(GlobalCooldownAttribute))
            {
                // Log command cooldowns
                cmdHandler.Client.Logger.LogCommand(
                    LogLevel.Warning,
                    eventArgs.Context,
                    "Command execution has been cancelled due to an active cooldown."
                );

                return eventArgs.Context.Message.CreateReactionAsync(AkkoEntities.CooldownEmoji);
            }

            return Task.CompletedTask;
        }
    }
}