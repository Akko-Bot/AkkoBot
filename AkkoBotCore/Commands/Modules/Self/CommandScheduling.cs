﻿using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Commands.Modules.Self.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Self
{
    [BotOwner, RequireGuild]
    [Group("autocommand"), Aliases("autocmd")]
    [Description("cmd_autocommand")]
    public class CommandScheduling : AkkoCommandModule
    {
        private readonly CommandScheduleService _service;

        public CommandScheduling(CommandScheduleService service)
            => _service = service;

        [Command("addscheduled")]
        [Description("cmd_autocommand_addscheduled")]
        public async Task AddScheduledCommand(CommandContext context, [Description("arg_autocommand_time")] TimeSpan time, [RemainingText, Description("arg_command")] string command)
        {
            if (command.StartsWith(context.Prefix, StringComparison.InvariantCultureIgnoreCase))
                command = command[context.Prefix.Length..];

            var cmd = context.CommandsNext.FindCommand(command, out var args);
            var success = !(await cmd.RunChecksAsync(context, false)).Any()
                && await _service.AddAutoCommandAsync(context, time, CommandType.Scheduled, cmd, args);

            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("addrepeated")]
        [Description("cmd_autocommand_addrepeated")]
        public async Task AddRepeatedCommand(CommandContext context, [Description("arg_autocommand_time")] TimeSpan time, [RemainingText, Description("arg_command")] string command)
        {
            if (command.StartsWith(context.Prefix, StringComparison.InvariantCultureIgnoreCase))
                command = command[context.Prefix.Length..];

            var cmd = context.CommandsNext.FindCommand(command, out var args);
            var success = !(await cmd.RunChecksAsync(context, false)).Any()
                && await _service.AddAutoCommandAsync(context, time, CommandType.Repeated, cmd, args);

            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("addstartup")]
        [Description("cmd_autocommand_startup")]
        public async Task AddStartupCommand(CommandContext context, [RemainingText, Description("arg_command")] string command)
        {
            if (command.StartsWith(context.Prefix, StringComparison.InvariantCultureIgnoreCase))
                command = command[context.Prefix.Length..];

            var cmd = context.CommandsNext.FindCommand(command, out var args);
            var success = !(await cmd.RunChecksAsync(context, false)).Any()
                && await _service.AddStartupCommandAsync(context, cmd, args);

            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("remove"), Aliases("rm")]
        [Description("cmd_autocommand_remove")]
        public async Task RemoveAutoCommand(CommandContext context, [Description("arg_autocommand_id")] int id)
        {
            var success = await _service.RemoveAutoCommandAsync(context.User, id);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_autocommand_list")]
        public async Task ListAutoCommands(CommandContext context)
        {
            var reminders = await _service.GetAutoCommandsAsync(context.User);

            var embed = new DiscordEmbedBuilder();

            if (reminders.Count == 0)
            {
                embed.WithDescription("autocommand_empty");
                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                embed.WithTitle("autocommand_title");

                foreach (var group in reminders.SplitInto(15))
                {
                    // Have to use .Bold for the IDs because .InlineCode misaligns the embed fields
                    embed.AddField("command", string.Join("\n", group.Select(x => Formatter.Bold($"{x.Id}.") + " " + context.Prefix + x.CommandString.Replace("\n", string.Empty).MaxLength(50, "[...]")).ToArray()), true)
                        .AddField("channel", string.Join("\n", group.Select(x => $"<#{x.ChannelId}>").ToArray()), true)
                        .AddField("triggers_in", string.Join("\n", group.Select(x => _service.GetElapseTime(x)).ToArray()), true);
                }

                await context.RespondPaginatedByFieldsAsync(embed, 3);
            }
        }
    }
}