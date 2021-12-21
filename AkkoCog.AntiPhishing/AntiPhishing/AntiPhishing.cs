using AkkoCog.AntiPhishing.AntiPhishing.Services;
using AkkoCore.Commands.Abstractions;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
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
}
