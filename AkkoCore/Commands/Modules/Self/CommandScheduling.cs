using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Self.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Self;

[BotOwner, RequireGuild]
[Group("autocommand"), Aliases("autocmd")]
[Description("cmd_autocommand")]
public sealed class CommandScheduling : AkkoCommandModule
{
    private readonly CommandScheduleService _service;

    public CommandScheduling(CommandScheduleService service)
        => _service = service;

    [Command("addscheduled")]
    [Description("cmd_autocommand_addscheduled")]
    public async Task AddScheduledCommandAsync(CommandContext context, [Description("arg_autocommand_time")] TimeSpan time, [RemainingText, Description("arg_command")] string command)
    {
        if (command.StartsWith(context.Prefix, StringComparison.InvariantCultureIgnoreCase))
            command = command[context.Prefix.Length..];

        var cmd = context.CommandsNext.FindCommand(command, out var args);
        var success = !(await cmd.RunChecksAsync(context, false)).Any()
            && await _service.AddAutoCommandAsync(context, time, AutoCommandType.Scheduled, cmd, args);

        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("addrepeated")]
    [Description("cmd_autocommand_addrepeated")]
    public async Task AddRepeatedCommandAsync(CommandContext context, [Description("arg_autocommand_time")] TimeSpan time, [RemainingText, Description("arg_command")] string command)
    {
        if (command.StartsWith(context.Prefix, StringComparison.InvariantCultureIgnoreCase))
            command = command[context.Prefix.Length..];

        var cmd = context.CommandsNext.FindCommand(command, out var args);
        var success = !(await cmd.RunChecksAsync(context, false)).Any()
            && await _service.AddAutoCommandAsync(context, time, AutoCommandType.Repeated, cmd, args);

        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("addstartup")]
    [Description("cmd_autocommand_startup")]
    public async Task AddStartupCommandAsync(CommandContext context, [RemainingText, Description("arg_command")] string command)
    {
        if (command.StartsWith(context.Prefix, StringComparison.InvariantCultureIgnoreCase))
            command = command[context.Prefix.Length..];

        var cmd = context.CommandsNext.FindCommand(command, out var args);
        var success = !(await cmd.RunChecksAsync(context, false)).Any()
            && await _service.AddStartupCommandAsync(context, cmd, args);

        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("remove"), Aliases("rm")]
    [Description("cmd_autocommand_remove")]
    public async Task RemoveAutoCommandAsync(CommandContext context, [Description("arg_autocommand_id")] int id)
    {
        var success = await _service.RemoveAutoCommandAsync(context.User, id);
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [GroupCommand, Command("list"), Aliases("show")]
    [Description("cmd_autocommand_list")]
    public async Task ListAutoCommandsAsync(CommandContext context)
    {
        var reminders = (await _service.GetAutoCommandsAsync(context.User))
            .OrderBy(x => _service.GetElapseTime(x));

        var embed = new SerializableDiscordEmbed();

        if (!reminders.Any())
        {
            embed.WithDescription("autocommand_empty");
            await context.RespondLocalizedAsync(embed, isError: true);
        }
        else
        {
            embed.WithTitle("autocommand_title");

            foreach (var group in reminders.Chunk(15))
            {
                // Have to use .Bold for the IDs because .InlineCode misaligns the embed fields
                embed.AddField("command", string.Join("\n", group.Select(x => Formatter.Bold($"{x.Id}.") + " " + context.Prefix + x.CommandString.Replace("\n", string.Empty).MaxLength(50, AkkoConstants.EllipsisTerminator))), true)
                    .AddField("channel", string.Join("\n", group.Select(x => $"<#{x.ChannelId}>")), true)
                    .AddField("triggers_in", string.Join("\n", group.Select(x => _service.GetElapseTime(x))), true);
            }

            await context.RespondPaginatedByFieldsAsync(embed, 3);
        }
    }
}