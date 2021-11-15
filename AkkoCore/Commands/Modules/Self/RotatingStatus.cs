using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Self.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Self;

[BotOwner]
[Group("playingstatus"), Aliases("pl")]
[Description("cmd_playingstatus")]
public sealed class RotatingStatus : AkkoCommandModule
{
    private readonly StatusService _statusService;
    private readonly BotConfigService _botService;

    public RotatingStatus(StatusService service, BotConfigService botService)
    {
        _statusService = service;
        _botService = botService;
    }

    [Command("set"), HiddenOverload]
    public async Task ResetStatusAsync(CommandContext context)
    {
        await context.Client.UpdateStatusAsync();
        var success = await _statusService.RemoveStatusesAsync(x => x.RotationTime == TimeSpan.Zero);
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("set")]
    [Description("cmd_setstatus")]
    public async Task SetStatusAsync(CommandContext context,
        [Description("arg_pstatus_type")] ActivityType type,
        [RemainingText, Description("arg_pstatus")] string message)
    {
        var activity = new DiscordActivity(message, type);

        await context.Client.UpdateStatusAsync(activity);
        await _statusService.CreateStatusAsync(activity, TimeSpan.Zero);
        await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
    }

    [Command("set")]
    [Description("cmd_setstatus")]
    public async Task SetStreamStatusAsync(CommandContext context,
        [Description("arg_stream_url")] string streamUrl,
        [RemainingText, Description("arg_pstatus")] string message)
    {
        var activity = new DiscordActivity(message, ActivityType.Streaming) { StreamUrl = streamUrl };
        var isValid = await _statusService.CreateStatusAsync(activity, TimeSpan.Zero);

        if (isValid)
            await context.Client.UpdateStatusAsync(activity);

        await context.Message.CreateReactionAsync((isValid) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("add")]
    [Description("cmd_addstatus")]
    public async Task AddStatusAsync(CommandContext context,
        [Description("arg_timed_pstatus")] TimeSpan time,
        [Description("arg_pstatus_type")] ActivityType type,
        [RemainingText, Description("arg_pstatus")] string message)
    {
        var activity = new DiscordActivity(message, type);
        var success = time != TimeSpan.Zero && await _statusService.CreateStatusAsync(activity, time);

        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("add")]
    public async Task AddStreamStatusAsync(CommandContext context,
        [Description("arg_timed_pstatus")] TimeSpan time,
        [Description("arg_stream_url")] string streamUrl,
        [RemainingText, Description("arg_pstatus")] string message)
    {
        var activity = new DiscordActivity(message, ActivityType.Streaming) { StreamUrl = streamUrl };
        var isValid = time != TimeSpan.Zero && await _statusService.CreateStatusAsync(activity, time);

        await context.Message.CreateReactionAsync((isValid) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("rotatestatus"), Aliases("rotate", "ropl")]
    [Description("cmd_rotatestatus")]
    public async Task RotateStatusAsync(CommandContext context)
    {
        var success = await _statusService.RotateStatusesAsync();
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("remove"), Aliases("rm")]
    [Description("cmd_removestatus")]
    public async Task RemoveStatusAsync(CommandContext context, [Description("arg_int")] int id)
    {
        var success = await _statusService.RemoveStatusesAsync(x => x.Id == id);

        if (_statusService.GetStatuses().Count is 0)
            await context.Client.UpdateStatusAsync();

        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("clear")]
    [Description("cmd_clearstatus")]
    public async Task ClearStatusAsync(CommandContext context)
    {
        var amount = await _statusService.ClearStatusesAsync();

        await context.Client.UpdateStatusAsync();
        await context.Message.CreateReactionAsync((amount is not 0) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [GroupCommand, Command("list"), Aliases("show")]
    [Description("cmd_liststatus")]
    public async Task ListStatusAsync(CommandContext context)
    {
        var statuses = _statusService.GetStatuses();

        if (statuses.Count is 0)
        {
            var error = new SerializableDiscordEmbed()
                .WithDescription("pstatus_error");

            await context.RespondLocalizedAsync(error, isError: true);
            return;
        }

        var embed = new SerializableDiscordEmbed()
            .WithTitle("pstatus_title")
            .AddField("id", string.Join('\n', statuses.Select(x => x.Id)), true)
            .AddField("message", string.Join('\n', statuses.Select(x => $"{x.Type} {x.Message}".MaxLength(40, AkkoConstants.EllipsisTerminator))), true)
            .AddField("pstatus_time", string.Join('\n', statuses.Select(x => x.RotationTime)), true)
            .WithFooter(context.FormatLocalized("pstatus_rotation", (_botService.GetConfig().RotateStatus) ? "enabled" : "disabled"));

        await context.RespondPaginatedByFieldsAsync(embed);
    }
}