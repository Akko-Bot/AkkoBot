using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Models.Serializable.EmbedParts;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration
{
    [Group("filtercontent"), Aliases("fc")]
    [Description("cmd_fc")]
    [RequireGuild, RequirePermissions(Permissions.ManageGuild)]
    public sealed class ContentFilterCommands : AkkoCommandModule
    {
        private readonly ContentFilterService _service;

        public ContentFilterCommands(ContentFilterService service)
            => _service = service;

        [Command("url"), Aliases("link")]
        [Description("cmd_fc_url")]
        public async Task UrlToggleAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel? channel = default)
            => await SetPropertyAsync(context, channel, "fc_url_toggle", ContentFilter.Url);

        [Command("attachment"), Aliases("att")]
        [Description("cmd_fc_att")]
        public async Task AttachmentToggleAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel? channel = default)
            => await SetPropertyAsync(context, channel, "fc_att_toggle", ContentFilter.Attachment);

        [Command("image"), Aliases("img")]
        [Description("cmd_fc_img")]
        public async Task ImageToggleAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel? channel = default)
            => await SetPropertyAsync(context, channel, "fc_img_toggle", ContentFilter.Image);

        [Command("invite")]
        [Description("cmd_fc_invite")]
        public async Task InviteToggleAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel? channel = default)
            => await SetPropertyAsync(context, channel, "fc_invite_toggle", ContentFilter.Invite);

        [Command("sticker")]
        [Description("cmd_fc_sticker")]
        public async Task StickerToggleAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel? channel = default)
            => await SetPropertyAsync(context, channel, "fc_sticker_toggle", ContentFilter.Sticker);

        [Command("command"), Aliases("cmd")]
        [Description("cmd_fc_command")]
        public async Task CommandToggleAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel? channel = default)
            => await SetPropertyAsync(context, channel, "fc_command_toggle", ContentFilter.Command);

        [Command("remove"), Aliases("rm")]
        [Description("cmd_fc_remove")]
        public async Task RemoveFilterAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel? channel = default)
        {
            channel ??= context.Channel;

            var filter = _service.GetContentFilters(context.Guild)
                .FirstOrDefault(x => x.ChannelId == channel.Id);

            var success = filter is not null && await _service.RemoveContentFilterAsync(context.Guild, filter.Id);

            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("remove")]
        public async Task RemoveFilterAsync(CommandContext context, [Description("arg_fc_id")] int id)
        {
            var success = await _service.RemoveContentFilterAsync(context.Guild, id);
            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("clear")]
        [Description("cmd_fc_clear")]
        public async Task ClearAllAsync(CommandContext context)
        {
            var success = await _service.ClearContentFiltersAsync(context.Guild);
            await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_fc_list")]
        public async Task ListFiltersAsync(CommandContext context)
        {
            var embed = new SerializableDiscordEmbed();
            var filters = _service.GetContentFilters(context.Guild)
                .Where(x => x.IsActive);

            if (!filters.Any())
            {
                embed.WithDescription("fc_list_empty");
                await context.RespondLocalizedAsync(embed, isError: true);

                return;
            }

            var fields = new List<SerializableEmbedField>();

            foreach (var filterGroup in filters.Chunk(AkkoConstants.LinesPerPage))
            {
                fields.Add(new("id", string.Join("\n", filterGroup.Select(x => x.Id)), true));
                fields.Add(new("channel", string.Join("\n", filterGroup.Select(x => $"<#{x.ChannelId}>")), true));
                fields.Add(new("fc_allow", string.Join("\n", filterGroup.Select(x => string.Join(", ", x.ContentType.ToStrings().Select(y => context.FormatLocalized(y.ToSnakeCase()))))), true));
            }

            await context.RespondPaginatedByFieldsAsync(embed, fields, 3);
        }

        /// <summary>
        /// Sets the property of a content filter for the specified guild channel and sends a confirmation message.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="channel">The Discord channel.</param>
        /// <param name="responseKey">The response key to be sent in the message.</param>
        /// <param name="selector">A method to define which property is going to be updated.</param>
        /// <returns>The message sent to Discord.</returns>
        private async Task<DiscordMessage> SetPropertyAsync(CommandContext context, DiscordChannel? channel, string responseKey, ContentFilter filter)
        {
            channel ??= context.Channel;
            var result = await _service.SetContentFilterAsync(context.Guild, channel, x => x.ContentType = (x.ContentType.HasFlag(filter)) ? x.ContentType & ~filter : x.ContentType | filter);
            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized(responseKey, (result.HasFlag(filter)) ? "enabled" : "disabled", channel.Mention));

            return await context.RespondLocalizedAsync(embed);
        }
    }
}