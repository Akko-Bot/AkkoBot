using System;
using AkkoBot.Command.Abstractions;
using AkkoBot.Command.Attributes;
using AkkoBot.Command.Modules.Self.Services;
using AkkoBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using System.Linq;
using AkkoBot.Extensions;
using DSharpPlus;

namespace AkkoBot.Command.Modules.Self
{
    [BotOwner]
    [RequireBotPermissions(Permissions.SendMessages | Permissions.AddReactions)]
    public class RotatingStatus : AkkoCommandModule
    {
        private readonly StatusService _statusService;
        private readonly BotConfigService _botService;

        public RotatingStatus(StatusService service, BotConfigService botService)
        {
            _statusService = service;
            _botService = botService;
        }

        [Command("setstatus"), HiddenOverload]
        public async Task ResetStatus(CommandContext context)
        {
            await context.Client.UpdateStatusAsync();
            var success = await _statusService.RemoveStatusesAsync(x => x.RotationTime == TimeSpan.Zero);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("setstatus")]
        [Description("cmd_setstatus")]
        public async Task SetStatus(CommandContext context,
            [Description("arg_pstatus_type")] ActivityType type,
            [RemainingText, Description("arg_pstatus")] string status)
        {
            var activity = new DiscordActivity(status, type);

            await context.Client.UpdateStatusAsync(activity);
            await _statusService.CreateStatusAsync(activity, TimeSpan.Zero);
            await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
        }

        [Command("addstatus")]
        public async Task AddStatus(CommandContext context,
            [Description("arg_timed_pstatus")] TimeSpan time,
            [Description("arg_pstatus_type")] ActivityType type,
            [RemainingText, Description("arg_pstatus")] string status)
        {
            var activity = new DiscordActivity(status, type);

            await _statusService.CreateStatusAsync(activity, (time == TimeSpan.Zero) ? TimeSpan.FromSeconds(30) : time);
            await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
        }

        [Command("liststatus")]
        [Description("cmd_liststatus")]
        public async Task ListStatus(CommandContext context)
        {
            var statuses = _statusService.GetStatuses();

            if (statuses.Count == 0)
            {
                var error = new DiscordEmbedBuilder()
                    .WithDescription("pstatus_error");

                await context.RespondLocalizedAsync(error, isError: true);
                return;
            }

            var enabledDisabled = (_botService.GetConfig().RotateStatus)
                ? context.FormatLocalized("enabled")
                : context.FormatLocalized("disabled");

            var ids = string.Join('\n', statuses.Select(x => x.Id).ToArray());
            var messages = string.Join('\n', statuses.Select(x => $"{x.Type} {x.Message}".MaxLength(40, "[...]")).ToArray());
            var time = string.Join('\n', statuses.Select(x => x.RotationTime).ToArray());

            var embed = new DiscordEmbedBuilder()
                .WithTitle("pstatus_title")
                .AddField("id", ids, true)
                .AddField("message", messages, true)
                .AddField("pstatus_time", time, true)
                .WithFooter(context.FormatLocalized("pstatus_rotation", enabledDisabled));

            await context.RespondPaginatedByFieldsAsync(embed);
        }

        [Command("removestatus")]
        [Description("cmd_removestatus")]
        public async Task RemoveStatus(CommandContext context, [Description("arg_int")] int id)
        {
            var success = await _statusService.RemoveStatusesAsync(x => x.Id == id);

            if (_statusService.GetStatuses().Count == 0)
                await context.Client.UpdateStatusAsync();

            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("clearstatus"), Aliases("clearstatuses")]
        [Description("cmd_clearstatus")]
        public async Task ClearStatus(CommandContext context)
        {
            var amount = await _statusService.ClearStatusesAsync();

            await context.Client.UpdateStatusAsync();
            await context.Message.CreateReactionAsync((amount is not 0) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }
    }
}