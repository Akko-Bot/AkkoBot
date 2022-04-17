using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Modules.Utilities.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kotz.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Utilities;

[Group("remind"), Aliases("reminder", "reminders")]
[Description("cmd_remind")]
public sealed class Reminders : AkkoCommandModule
{
    private readonly ReminderService _service;

    public Reminders(ReminderService service)
        => _service = service;

    [Command("me")]
    public async Task AddPrivateReminderAsync(CommandContext context, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [RemainingText, Description("arg_remind_message")] string message)
        => await AddPrivateReminderAsync(context, timeOfDay.Interval, message);

    [Command("here")]
    public async Task AddGuildReminderAsync(CommandContext context, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [RemainingText, Description("arg_remind_message")] string message)
        => await AddGuildReminderAsync(context, timeOfDay.Interval, message);

    [Command("channel")]
    public async Task AddChannelReminderAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [RemainingText, Description("arg_remind_message")] string message)
        => await AddChannelReminderAsync(context, channel, timeOfDay.Interval, message);

    [Command("me")]
    public async Task AddPrivateReminderAsync(CommandContext context, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [Description("arg_remind_time")] TimeSpan time, [RemainingText, Description("arg_remind_message")] string message)
        => await AddPrivateReminderAsync(context, timeOfDay.Interval.Add(time), message);

    [Command("here")]
    public async Task AddGuildReminderAsync(CommandContext context, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [Description("arg_remind_time")] TimeSpan time, [RemainingText, Description("arg_remind_message")] string message)
        => await AddGuildReminderAsync(context, timeOfDay.Interval.Add(time), message);

    [Command("channel")]
    public async Task AddPrivateReminderAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel, [Description("arg_time_of_day")] TimeOfDay timeOfDay, [Description("arg_remind_time")] TimeSpan time, [RemainingText, Description("arg_remind_message")] string message)
        => await AddChannelReminderAsync(context, channel, timeOfDay.Interval.Add(time), message);

    [Command("me")]
    [Description("cmd_remind_me")]
    public async Task AddPrivateReminderAsync(CommandContext context, [Description("arg_remind_time")] TimeSpan time, [RemainingText, Description("arg_remind_message")] string message)
    {
        var success = await _service.AddReminderAsync(context, context.Channel, time, true, message);
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("here")]
    [Description("cmd_remind_here")]
    public async Task AddGuildReminderAsync(CommandContext context, [Description("arg_remind_time")] TimeSpan time, [RemainingText, Description("arg_remind_message")] string message)
    {
        var success = await _service.AddReminderAsync(context, context.Channel, time, context.Guild is null, message);
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [GroupCommand, Command("channel")]
    [Description("cmd_remind_channel")]
    [RequireUserPermissions(Permissions.ManageMessages)] // Shows up on !help, but doesn't perform the check
    public async Task AddChannelReminderAsync(CommandContext context,
        [Description("arg_discord_channel")] DiscordChannel channel,
        [Description("arg_remind_time")] TimeSpan time,
        [RemainingText, Description("arg_remind_message")] string message)
    {
        var success = context.Guild is not null
            && context.Member!.Roles.Any(x => x.Permissions.HasPermission(Permissions.ManageMessages))
            && await _service.AddReminderAsync(context, channel, time, false, message);

        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("remove"), Aliases("rm")]
    [Description("cmd_remind_remove")]
    public async Task RemoveReminderAsync(CommandContext context, [Description("arg_remind_id")] int id)
    {
        var success = await _service.RemoveReminderAsync(context.User, id);
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("list"), Aliases("show")]
    [Description("cmd_remind_list")]
    public async Task ListRemindersAsync(CommandContext context)
    {
        var reminders = await _service.GetRemindersAsync(
            context.User,
            x => new ReminderEntity()
            {
                Id = x.Id,
                ChannelId = x.ChannelId,
                Content = x.Content,
                ElapseAt = x.ElapseAt
            }
        );

        var embed = new SerializableDiscordEmbed();

        if (reminders.Count == 0)
        {
            embed.WithDescription("reminder_list_empty");
            await context.RespondLocalizedAsync(embed, isError: true);
        }
        else
        {
            embed.WithTitle("reminder_list_title");

            foreach (var group in reminders.Chunk(15))
            {
                // Have to use .Bold for the IDs because .InlineCode misaligns the embed fields
                embed.AddField("message", string.Join("\n", group.Select(x => Formatter.Bold($"{x.Id}.") + " " + x.Content.Replace("\n", string.Empty).MaxLength(50, AkkoConstants.EllipsisTerminator))), true)
                    .AddField("channel", string.Join("\n", group.Select(x => (x.IsPrivate) ? "private" : $"<#{x.ChannelId}>")), true)
                    .AddField("triggers_in", string.Join("\n", group.Select(x => DateTimeOffset.Now.Add(x.ElapseIn).ToDiscordTimestamp(TimestampFormat.RelativeTime))), true);
            }

            await context.RespondPaginatedByFieldsAsync(embed, 3);
        }
    }
}