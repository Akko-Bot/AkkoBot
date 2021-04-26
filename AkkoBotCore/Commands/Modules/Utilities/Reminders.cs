﻿using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities
{
    [Group("remind"), Aliases("reminder", "reminders")]
    [Description("cmd_remind")]
    public class Reminders : AkkoCommandModule
    {
        private readonly ReminderService _service;

        public Reminders(ReminderService service)
            => _service = service;

        [Command("me")]
        public async Task AddPrivateReminder(CommandContext context, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [RemainingText, Description("arg_remind_message")] string message)
            => await AddPrivateReminder(context, timeOfDay.Interval, message);

        [Command("here")]
        public async Task AddGuildReminder(CommandContext context, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [RemainingText, Description("arg_remind_message")] string message)
            => await AddGuildReminder(context, timeOfDay.Interval, message);

        [Command("channel")]
        public async Task AddChannelReminder(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [RemainingText, Description("arg_remind_message")] string message)
            => await AddChannelReminder(context, channel, timeOfDay.Interval, message);

        [Command("me")]
        public async Task AddPrivateReminder(CommandContext context, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [Description("arg_remind_time")] TimeSpan time, [RemainingText, Description("arg_remind_message")] string message)
            => await AddPrivateReminder(context, timeOfDay.Interval.Add(time), message);

        [Command("here")]
        public async Task AddGuildReminder(CommandContext context, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [Description("arg_remind_time")] TimeSpan time, [RemainingText, Description("arg_remind_message")] string message)
            => await AddGuildReminder(context, timeOfDay.Interval.Add(time), message);

        [Command("channel")]
        public async Task AddPrivateReminder(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [Description("arg_remind_time")] TimeSpan time, [RemainingText, Description("arg_remind_message")] string message)
            => await AddChannelReminder(context, channel, timeOfDay.Interval.Add(time), message);

        [Command("me")]
        [Description("cmd_remind_me")]
        public async Task AddPrivateReminder(CommandContext context, [Description("arg_remind_time")] TimeSpan time, [RemainingText, Description("arg_remind_message")] string message)
        {
            var success = await _service.AddReminderAsync(context, context.Channel, time, true, message);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("here")]
        [Description("cmd_remind_here")]
        public async Task AddGuildReminder(CommandContext context, [Description("arg_remind_time")] TimeSpan time, [RemainingText, Description("arg_remind_message")] string message)
        {
            var success = await _service.AddReminderAsync(context, context.Channel, time, context.Guild is null, message);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [GroupCommand, Command("channel")]
        [Description("cmd_remind_channel")]
        [RequireUserPermissions(Permissions.ManageMessages)] // Shows up on !help, but doesn't perform the check
        public async Task AddChannelReminder(CommandContext context,
            [Description("arg_discord_channel")] DiscordChannel channel,
            [Description("arg_remind_time")] TimeSpan time,
            [RemainingText, Description("arg_remind_message")] string message)
        {
            var success = context.Guild is not null
                && (context.Member.Roles.Any(x => x.Permissions.HasPermission(Permissions.ManageMessages)))
                && await _service.AddReminderAsync(context, channel, time, false, message);

            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("remove"), Aliases("rm")]
        [Description("cmd_remind_remove")]
        public async Task RemoveReminder(CommandContext context, [Description("arg_remind_id")] int id)
        {
            var success = await _service.RemoveReminderAsync(context.User, id);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("list"), Aliases("show")]
        [Description("cmd_remind_list")]
        public async Task ListReminders(CommandContext context)
        {
            var reminders = await _service.GetRemindersAsync(context.User);

            var embed = new DiscordEmbedBuilder();

            if (reminders.Length == 0)
            {
                embed.WithDescription("reminder_list_empty");
                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                embed.WithTitle("reminder_list_title");

                foreach (var group in reminders.SplitInto(15))
                {
                    // Have to use .Bold for the IDs because .InlineCode misaligns the embed fields
                    embed.AddField("message", string.Join("\n", group.Select(x => Formatter.Bold($"{x.Id}.") + " " + x.Content.Replace("\n", string.Empty).MaxLength(50, "[...]")).ToArray()), true)
                        .AddField("channel", string.Join("\n", group.Select(x => (x.IsPrivate) ? "private" : $"<#{x.ChannelId}>").ToArray()), true)
                        .AddField("triggers_in", string.Join("\n", group.Select(x => x.ElapseAt.Subtract(DateTimeOffset.Now).ToString(@"%d\d\ %h\h\ %m\m")).ToArray()), true);
                }

                await context.RespondPaginatedByFieldsAsync(embed, 3);
            }
        }
    }
}