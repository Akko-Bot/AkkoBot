using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Timers.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities
{
    [Group("repeat")]
    [Description("cmd_repeat")]
    [RequireUserPermissions(Permissions.ManageMessages)]
    public class Repeaters : AkkoCommandModule
    {
        private readonly IDbCache _dbCache;
        private readonly RepeaterService _service;

        public Repeaters(IDbCache dbCache, RepeaterService service)
        {
            _dbCache = dbCache;
            _service = service;
        }

        [Command("here")]
        [Description("cmd_repeat_here")]
        public async Task AddDailyRepeater(CommandContext context, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [RemainingText, Description("arg_repeat_message")] string message)
        {
            var success = await _service.AddRepeaterAsync(context, context.Channel, timeOfDay.Interval, message, timeOfDay);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("here")]
        public async Task AddRepeater(CommandContext context, [Description("arg_repeat_time")] TimeSpan time, [RemainingText, Description("arg_repeat_message")] string message)
        {
            var success = await _service.AddRepeaterAsync(context, context.Channel, time, message);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [GroupCommand, Command("channel")]
        public async Task AddDailyChannelRepeater(CommandContext context,
            [Description("arg_discord_channel")] DiscordChannel channel,
            [Description("arg_time_of_day")] TimeOfDay timeOfDay,
            [RemainingText, Description("arg_repeat_message")] string message)
        {
            var success = await _service.AddRepeaterAsync(context, channel, timeOfDay.Interval, message, timeOfDay);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [GroupCommand, Command("channel")]
        [Description("cmd_repeat_channel")]
        public async Task AddChannelRepeater(CommandContext context,
            [Description("arg_discord_channel")] DiscordChannel channel,
            [Description("arg_repeat_time")] TimeSpan time,
            [RemainingText, Description("arg_repeat_message")] string message)
        {
            var success = await _service.AddRepeaterAsync(context, channel, time, message);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("remove"), Aliases("rm")]
        [Description("cmd_repeat_remove")]
        public async Task RemoveRepeater(CommandContext context, [Description("arg_uint")] int id)
        {
            var success = await _service.RemoveRepeaterAsync(context.Guild, id);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("clear")]
        [Description("cmd_repeat_clear")]
        public async Task ClearRepeaters(CommandContext context)
        {
            var success = await _service.ClearRepeatersAsync(context.Guild);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("info")]
        [Description("cmd_repeat_info")]
        public async Task RepeaterInfo(CommandContext context, [Description("arg_uint")] int id)
        {
            _dbCache.Repeaters.TryGetValue(context.Guild.Id, out var repeaterCache);
            var embed = new DiscordEmbedBuilder();
            var repeater = repeaterCache?.FirstOrDefault(x => x.Id == id)
                ?? (await _service.GetRepeatersAsync(context.Guild, x => x.Id == id)).FirstOrDefault();

            if (repeater is null)
            {
                embed.WithDescription(context.FormatLocalized("repeater_not_found", id));
            }
            else
            {
                _dbCache.Timers.TryGetValue(repeater.TimerId, out var timer);
                var member = await context.Guild.GetMemberSafelyAsync(repeater.AuthorId);
                var (dbTimer, dbUser) = await _service.GetRepeaterExtraInfoAsync(timer, repeater, member);

                embed.WithTitle(context.FormatLocalized("repeater") + $" #{repeater.Id}")
                    .WithDescription(Formatter.BlockCode(repeater.Content, "yaml"))
                    .AddField("interval", repeater.Interval.ToString(@"%d\d\ %h\h\ %m\m\ %s\s"), true)
                    .AddField("triggers_in", (timer?.ElapseIn ?? dbTimer.ElapseIn).ToString(@"%d\d\ %h\h\ %m\m\ %s\s"), true)
                    .AddField("triggers_at", (timer?.ElapseAt ?? dbTimer.ElapseAt).ToOffset(context.GetTimeZone().BaseUtcOffset).ToString(), true)
                    .AddField("author", member?.GetFullname() ?? dbUser.FullName, true)
                    .AddField("channel", $"<#{repeater.ChannelId}>", true);
            }

            await context.RespondLocalizedAsync(embed, repeater is null, repeater is null);
        }

        [Command("list"), Aliases("show")]
        [Description("cmd_repeat_list")]
        public async Task ListRepeaters(CommandContext context)
        {
            var repeaters = await _service.GetRepeatersAsync(context.Guild);
            var embed = new DiscordEmbedBuilder();

            if (repeaters.Count is 0)
            {
                embed.WithDescription("repeat_list_empty");
                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                var timers = new List<IAkkoTimer>(repeaters.Count);

                foreach (var repeater in repeaters)
                    timers.Add((_dbCache.Timers.TryGetValue(repeater.TimerId, out var timer)) ? timer : null);

                embed.WithTitle("repeater_list_title")
                    .AddField("message", string.Join("\n", repeaters.Select(x => (Formatter.Bold($"{x.Id}. ") + x.Content).MaxLength(50, "[...]"))), true)
                    .AddField("channel", string.Join("\n", repeaters.Select(x => $"<#{x.ChannelId}>")), true)
                    .AddField("triggers_in", string.Join("\n", timers.Select(x => x?.ElapseIn.ToString(@"%d\d\ %h\h\ %m\m\ %s\s") ?? context.FormatLocalized("repeat_over_24h"))), true);

                await context.RespondLocalizedAsync(embed);
            }
        }
    }
}