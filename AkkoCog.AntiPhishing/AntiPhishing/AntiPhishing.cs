using AkkoCog.AntiPhishing.AntiPhishing.Services;
using AkkoCore.Commands.Abstractions;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kotz.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCog.AntiPhishing.AntiPhishing;

[Group("antiphishing")]
[Description("cmd_antiphishing")]
[RequireGuild]
public sealed class AntiPhishing : AkkoCommandModule
{
    private readonly AntiPhishingService _service;

    public AntiPhishing(AntiPhishingService service)
        => _service = service;

    [GroupCommand, Command("toggle")]
    [Description("cmd_antiphishing_toggle")]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task ToggleAntiPhishingAsync(CommandContext context)
    {
        var result = _service.ToggleAntiPhishing(context.Guild.Id);

        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("antiphishing_toggle_desc", (result) ? "enabled" : "disabled"));

        await context.RespondLocalizedAsync(embed);
    }

    [Command("punishment"), Aliases("punish")]
    [Description("cmd_antiphishing_punishment")]
    public async Task SetPhishingPunishmentAsync(CommandContext context, [Description("arg_warnp_type")] PunishmentType? punishmentType = default)
    {
        var success = _service.SetPunishment(context.Guild.Id, punishmentType);
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("ignore")]
    [Description("cmd_antiphishing_ignore")]
    public async Task ToggleIgnoredIdAsync(CommandContext context, [Description("arg_snowflakes")] params SnowflakeObject[] snowflakes)
    {
        var success = _service.ToggleIgnoredIds(context.Guild.Id, snowflakes.Select(x => x.Id));
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("list")]
    [Description("cmd_antiphishing_list")]
    public async Task ListSettingsAsync(CommandContext context)
    {
        _service.TryGetAntiPhishingConfig(context.Guild.Id, out var config);
        var embed = new SerializableDiscordEmbed()
            .WithTitle("antiphishing_list_title")
            .AddField("warnpl_punish", config?.PunishmentType?.ToString() ?? "none", true)
            .AddField("is_active", (config?.IsActive is true) ? "q_yes" : "q_no", true);

        if (config?.IgnoredIds is { Count: > 0 })
        {
            embed.WithDescription(
                context.FormatLocalized(
                    "{0}:\n{1}",
                    Formatter.Bold(context.FormatLocalized("ignored_ids")),
                    string.Join(", ", config.IgnoredIds.Select(x => context.Guild.GetMention(x)).OrderBy(x => x))
                        .MaxLength(AkkoConstants.MaxEmbedDescriptionLength, AkkoConstants.EllipsisTerminator)
                )
            );
        }

        var message = embed.BuildMessage()
            .WithAllowedMentions(Mentions.None);

        await context.RespondLocalizedAsync(message, false);
    }
}