using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration;

[Group("autoslowmode"), RequireGuild]
[Description("cmd_autoslowmode")]
[RequireUserPermissions(Permissions.ManageMessages)]
[RequirePermissions(Permissions.ManageChannels)]
public sealed class AutoSlowmode : AkkoCommandModule
{
    private readonly AutoSlowmodeService _service;

    public AutoSlowmode(AutoSlowmodeService service)
        => _service = service;

    [GroupCommand, Command("set")]
    [Description("cmd_autoslowmode_set")]
    public async Task EnableAutoSlowmodeAsync(
        CommandContext context,
        [Description("arg_autoslowmode_messages")] int messages,
        [Description("arg_autoslowmode_time")] TimeSpan time,
        [Description("arg_autoslowmode_interval")] TimeSpan interval,
        [Description("arg_autoslowmode_duration")] TimeSpan duration)
    {
        if (messages < 2 || time <= TimeSpan.Zero || interval <= TimeSpan.Zero || duration <= interval)
        {
            await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
            return;
        }

        await _service.SetPropertyAsync(context.Guild, x =>
        {
            x.IsActive = true;
            x.MessageAmount = messages;
            x.SlowmodeTriggerTime = time;
            x.SlowmodeInterval = interval;
            x.SlowmodeDuration = duration;

            return x.IsActive;
        });

        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("autoslowmode_enabled", messages, (int)time.TotalSeconds, (int)interval.TotalSeconds, (int)duration.TotalSeconds));

        await context.RespondLocalizedAsync(embed);
    }

    [GroupCommand, Command("set"), HiddenOverload]
    public async Task DisableAutoSlowmodeAsync(CommandContext context)
    {
        var result = await _service.SetPropertyAsync(context.Guild, x => x.IsActive = false);

        var embed = new SerializableDiscordEmbed()
            .WithDescription("autoslowmode_disabled");

        await context.RespondLocalizedAsync(embed);
    }

    [Command("ignore")]
    public async Task SetIgnoredIdAsync(CommandContext context, [Description("arg_discord_user")] DiscordMember user)
        => await SetIgnoredIdAsync(context, user);

    [Command("ignore")]
    public async Task SetIgnoredIdAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel)
        => await SetIgnoredIdAsync(context, channel);

    [Command("ignore")]
    public async Task SetIgnoredIdAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role)
        => await SetIgnoredIdAsync(context, role);

    [Command("ignore")]
    [Description("cmd_autoslowmode_ignore")]
    public async Task SetIgnoredIdAsync(CommandContext context, [Description("arg_snowflakes")] params SnowflakeObject[] ids)
    {
        var result = await _service.SetPropertyAsync(context.Guild, x =>
        {
            var amount = x.IgnoredIds.Count;

            foreach (long id in ids.Select(x => x.Id))
            {
                if (x.IgnoredIds.Contains(id))
                    x.IgnoredIds.Remove(id);
                else
                    x.IgnoredIds.Add(id);
            }

            return amount < x.IgnoredIds.Count; // true = increased
        });

        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized((result) ? "ignored_ids_add" : "ignored_ids_remove", ids.Length));

        await context.RespondLocalizedAsync(embed);
    }

    [Command("ignoreclear")]
    [Description("cmd_autoslowmode_ignoreclear")]
    public async Task SetIgnoredIdAsync(CommandContext context)
    {
        var result = await _service.SetPropertyAsync(context.Guild, x =>
        {
            var count = x.IgnoredIds.Count;

            if (count is not 0)
                x.IgnoredIds.Clear();

            return count;
        });

        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized((result is not 0) ? "ignored_ids_remove" : "ignored_ids_empty", result));

        await context.RespondLocalizedAsync(embed);
    }

    [Command("list"), Aliases("show")]
    [Description("cmd_autoslowmode_list")]
    public async Task ListSlowmodeSettingsAsync(CommandContext context)
    {
        var slowmode = _service.GetAutoSlowmodeSettings(context.Guild) ?? new();

        var embed = new SerializableDiscordEmbed()
            .WithTitle("autoslowmode_title")
            .AddField("message", slowmode.MessageAmount.ToString(), true)
            .AddField("trigger_time", slowmode.SlowmodeTriggerTime.ToString(), true)
            .AddField("interval", slowmode.SlowmodeInterval.ToString(), true)
            .AddField("duration", slowmode.SlowmodeDuration.ToString(), true)
            .WithFooter((slowmode.IsActive) ? "is_active" : "is_not_active");

        if (slowmode.IgnoredIds.Count is not 0)
            embed.WithDescription($"{context.FormatLocalized("ignored_ids")}: {string.Join(", ", slowmode.IgnoredIds)}");

        await context.RespondLocalizedAsync(embed, false);
    }
}