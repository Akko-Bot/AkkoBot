using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Net.Http;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration
{
    [RequirePermissions(Permissions.ManageWebhooks)]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public class GuildLogging : AkkoCommandModule
    {
        private readonly GuildLogService _service;
        private readonly UtilitiesService _utilities;

        public GuildLogging(GuildLogService service, UtilitiesService utilities)
        {
            _service = service;
            _utilities = utilities;
        }

        [Command("log")]
        [Description("cmd_log")]
        public async Task StartLogAsync(
            CommandContext context,
            [Description("arg_guildlog_type")] GuildLog logType,
            [Description("arg_discord_channel")] DiscordChannel channel = null,
            [Description("arg_webhook_name")] string webhookName = null,
            [Description("arg_emoji_url")] string avatarUrl = null)
        {
            using var avatarStream = (string.IsNullOrWhiteSpace(avatarUrl))
                ? await _utilities.GetOnlineStreamAsync(context.Guild.CurrentMember.AvatarUrl ?? context.Guild.CurrentMember.DefaultAvatarUrl)
                : await _utilities.GetOnlineStreamAsync(avatarUrl);
            var result = await _service.AddLogAsync(context, channel ?? context.Channel, logType, webhookName?.MaxLength(AkkoConstants.MaxUsernameLength), avatarStream);
            await context.Message.CreateReactionAsync((result) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }
    }
}
