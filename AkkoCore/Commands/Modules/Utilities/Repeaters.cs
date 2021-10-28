using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Modules.Utilities.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Timers.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Utilities
{
    [Group("repeat")]
    [Description("cmd_repeat")]
    [RequireUserPermissions(Permissions.ManageMessages)]
    public sealed class Repeaters : AkkoCommandModule
    {
        private readonly IAkkoCache _akkoCache;
        private readonly RepeaterService _service;

        public Repeaters(IAkkoCache akkoCache, RepeaterService service)
        {
            _akkoCache = akkoCache;
            _service = service;
        }

        [Command("here")]
        [Description("cmd_repeat_here")]
        public async Task AddDailyRepeaterAsync(CommandContext context, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [RemainingText, Description("arg_repeat_message")] string message)
        {
            var success = await _service.AddRepeaterAsync(context, context.Channel, timeOfDay.Interval, message, timeOfDay);
            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("here")]
        public async Task AddRepeaterAsync(CommandContext context, [Description("arg_repeat_time")] TimeSpan time, [RemainingText, Description("arg_repeat_message")] string message)
        {
            var success = await _service.AddRepeaterAsync(context, context.Channel, time, message);
            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [GroupCommand, Command("channel")]
        public async Task AddDailyChannelRepeaterAsync(CommandContext context,
            [Description("arg_discord_channel")] DiscordChannel channel,
            [Description("arg_time_of_day")] TimeOfDay timeOfDay,
            [RemainingText, Description("arg_repeat_message")] string message)
        {
            var success = await _service.AddRepeaterAsync(context, channel, timeOfDay.Interval, message, timeOfDay);
            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [GroupCommand, Command("channel")]
        [Description("cmd_repeat_channel")]
        public async Task AddChannelRepeaterAsync(CommandContext context,
            [Description("arg_discord_channel")] DiscordChannel channel,
            [Description("arg_repeat_time")] TimeSpan time,
            [RemainingText, Description("arg_repeat_message")] string message)
        {
            var success = await _service.AddRepeaterAsync(context, channel, time, message);
            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("remove"), Aliases("rm")]
        [Description("cmd_repeat_remove")]
        public async Task RemoveRepeaterAsync(CommandContext context, [Description("arg_uint")] int id)
        {
            var success = await _service.RemoveRepeaterAsync(context.Guild, id);
            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("clear")]
        [Description("cmd_repeat_clear")]
        public async Task ClearRepeatersAsync(CommandContext context)
        {
            var success = await _service.ClearRepeatersAsync(context.Guild);
            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("info")]
        [Description("cmd_repeat_info")]
        public async Task RepeaterInfoAsync(CommandContext context, [Description("arg_uint")] int id)
        {
            var embed = new SerializableDiscordEmbed();
            var repeaters = _service.GetRepeaters(context.Guild, x => x.Id == id);
            var isEmpty = repeaters is null or { Count: 0 };

            if (isEmpty)
                embed.WithDescription(context.FormatLocalized("repeater_not_found", id));
            else
            {
                var repeater = repeaters![0];
                _akkoCache.Timers.TryGetValue(repeater.TimerIdFK, out var timer);
                var member = await context.Guild.GetMemberSafelyAsync(repeater.AuthorId);
                var (dbTimer, dbUser) = await _service.GetRepeaterExtraInfoAsync(timer, repeater, member);

                embed.WithTitle(context.FormatLocalized("repeater") + $" #{repeater.Id}")
                    .WithDescription(Formatter.BlockCode(repeater.Content, "yaml"))
                    .AddField("interval", repeater.Interval.ToString(@"%d\d\ %h\h\ %m\m\ %s\s"), true)
                    .AddField("triggers_on", (timer?.ElapseAt ?? dbTimer!.ElapseAt).ToOffset(context.GetTimeZone().BaseUtcOffset).ToDiscordTimestamp(), true)
                    .AddField("triggers_in", DateTimeOffset.Now.Add(timer?.ElapseIn ?? dbTimer!.ElapseIn).ToDiscordTimestamp(TimestampFormat.RelativeTime), true)
                    .AddField("author", member?.GetFullname() ?? dbUser!.FullName, true)
                    .AddField("channel", $"<#{repeater.ChannelId}>", true);
            }

            await context.RespondLocalizedAsync(embed, isEmpty, isEmpty);
        }

        [Command("list"), Aliases("show")]
        [Description("cmd_repeat_list")]
        public async Task ListRepeatersAsync(CommandContext context)
        {
            var repeaters = _service.GetRepeaters(
                context.Guild,
                null,
                x => new RepeaterEntity()
                {
                    Id = x.Id,
                    TimerIdFK = x.TimerIdFK,
                    Content = x.Content,
                    ChannelId = x.ChannelId
                }
            );

            var embed = new SerializableDiscordEmbed();

            if (repeaters.Count is 0)
            {
                embed.WithDescription("repeat_list_empty");
                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                var timers = new List<IAkkoTimer>(repeaters.Count);

                foreach (var repeater in repeaters)
                {
                    if (_akkoCache.Timers.TryGetValue(repeater.TimerIdFK, out var timer))
                        timers.Add(timer);
                }

                embed.WithTitle("repeater_list_title")
                    .AddField("message", string.Join("\n", repeaters.Select(x => (Formatter.Bold($"{x.Id}. ") + x.Content).MaxLength(50, "[...]"))), true)
                    .AddField("channel", string.Join("\n", repeaters.Select(x => $"<#{x.ChannelId}>")), true)
                    .AddField("triggers_in", string.Join("\n", timers.Select(x => (x is null) ? context.FormatLocalized("repeat_over_24h") : DateTimeOffset.Now.Add(x.ElapseIn).ToDiscordTimestamp(TimestampFormat.RelativeTime))), true);

                await context.RespondLocalizedAsync(embed);
            }
        }
    }
}