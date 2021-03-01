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

namespace AkkoBot.Command.Modules.Self
{
    [BotOwner]
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
            var success = await _statusService.RemoveStaticStatus();
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("setstatus"), Aliases("setgame")]
        public async Task SetStatus(CommandContext context, ActivityType type, [RemainingText] string status)
        {
            var activity = new DiscordActivity(status, type);

            await context.Client.UpdateStatusAsync(activity);
            await _statusService.CreateStatus(activity, TimeSpan.Zero);
            await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
        }

        [Command("liststatus")]
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
            var types = string.Join('\n', statuses.Select(x => x.Type).ToArray());
            var messages = string.Join('\n', statuses.Select(x => x.Message.MaxLength(40, "[...]")).ToArray());

            var embed = new DiscordEmbedBuilder()
                .WithTitle("pstatus_title")
                .AddField("id", ids, true)
                .AddField("type", types, true)
                .AddField("message", messages, true)
                .WithFooter(context.FormatLocalized("pstatus_rotation", enabledDisabled));

            await context.RespondPaginatedByFieldsAsync(embed);
        }
    }
}