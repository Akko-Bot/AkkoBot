using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration
{
    [RequireGuild]
    public class WordFilterExtras : AkkoCommandModule
    {
        private readonly WordFilterService _service;

        public WordFilterExtras(WordFilterService service)
            => _service = service;

        [Command("filterinvite"), Aliases("fi")]
        [Description("cmd_filterinvite")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task ToggleInviteRemovalAsync(CommandContext context)
        {
            var success = await _service.SetWordFilterSettingsAsync(context.Guild.Id, x => x.FilterInvites = !x.FilterInvites);

            var embed = new SerializableDiscordMessage()
                .WithDescription(context.FormatLocalized("fi_toggle", (success) ? "enabled" : "disabled"));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("filtersticker"), Aliases("fs")]
        [Description("cmd_filtersticker")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task ToggleStickerRemovalAsync(CommandContext context)
        {
            var success = await _service.SetWordFilterSettingsAsync(context.Guild.Id, x => x.FilterStickers = !x.FilterStickers);

            var embed = new SerializableDiscordMessage()
                .WithDescription(context.FormatLocalized("fs_toggle", (success) ? "enabled" : "disabled"));

            await context.RespondLocalizedAsync(embed);
        }
    }
}