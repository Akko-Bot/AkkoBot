using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration
{
    [RequireGuild]
    public sealed class WordFilterExtras : AkkoCommandModule
    {
        private readonly WordFilterService _service;

        public WordFilterExtras(WordFilterService service)
            => _service = service;

        [Command("filterinvite"), Aliases("fi")]
        [Description("cmd_filterinvite")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task ToggleInviteRemovalAsync(CommandContext context)
        {
            var success = await _service.SetWordFilterAsync(context.Guild.Id, x => x.Behavior = x.Behavior.ToggleFlag(WordFilterBehavior.FilterInvite));

            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized("fi_toggle", (success.HasFlag(WordFilterBehavior.FilterInvite)) ? "enabled" : "disabled"));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("filtersticker"), Aliases("fs")]
        [Description("cmd_filtersticker")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task ToggleStickerRemovalAsync(CommandContext context)
        {
            var success = await _service.SetWordFilterAsync(context.Guild.Id, x => x.Behavior = x.Behavior.ToggleFlag(WordFilterBehavior.FilterSticker));

            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized("fs_toggle", (success.HasFlag(WordFilterBehavior.FilterSticker)) ? "enabled" : "disabled"));

            await context.RespondLocalizedAsync(embed);
        }
    }
}