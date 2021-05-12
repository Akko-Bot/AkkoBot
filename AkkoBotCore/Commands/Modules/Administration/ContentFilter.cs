using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Models;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration
{
    [Group("filtercontent"), Aliases("fc")]
    [Description("cmd_fc")]
    [RequireGuild, RequirePermissions(Permissions.ManageGuild)]
    public class ContentFilter : AkkoCommandModule
    {
        private readonly ContentFilterService _service;

        public ContentFilter(ContentFilterService service)
            => _service = service;

        [Command("url"), Aliases("link")]
        [Description("cmd_fc_url")]
        public async Task UrlToggle(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
            => await SetPropertyAsync(context, channel, "fc_url_toggle", x => x.IsUrlOnly = !x.IsUrlOnly);

        [Command("attachment"), Aliases("att")]
        [Description("cmd_fc_att")]
        public async Task AttachmentToggle(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
            => await SetPropertyAsync(context, channel, "fc_att_toggle", x => x.IsAttachmentOnly = !x.IsAttachmentOnly);

        [Command("image"), Aliases("img")]
        [Description("cmd_fc_img")]
        public async Task ImageToggle(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
            => await SetPropertyAsync(context, channel, "fc_img_toggle", x => x.IsImageOnly = !x.IsImageOnly);

        [Command("invite")]
        [Description("cmd_fc_invite")]
        public async Task InviteToggle(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
            => await SetPropertyAsync(context, channel, "fc_invite_toggle", x => x.IsInviteOnly = !x.IsInviteOnly);

        [Command("command"), Aliases("cmd")]
        [Description("cmd_fc_command")]
        public async Task CommandToggle(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
            => await SetPropertyAsync(context, channel, "fc_command_toggle", x => x.IsCommandOnly = !x.IsCommandOnly);

        [Command("remove"), Aliases("rm")]
        [Description("cmd_fc_remove")]
        public async Task RemoveFilter(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
        {
            channel ??= context.Channel;

            var filter = _service.GetContentFilters(context.Guild)
                .FirstOrDefault(x => x.ChannelId == channel.Id);

            var success = filter is not null && await _service.RemoveContentFilterAsync(context.Guild, filter.Id);

            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("remove")]
        public async Task RemoveFilter(CommandContext context, [Description("arg_fc_id")] int id)
        {
            var success = await _service.RemoveContentFilterAsync(context.Guild, id);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("clear")]
        [Description("cmd_fc_clear")]
        public async Task ClearAll(CommandContext context)
        {
            var success = await _service.ClearContentFiltersAsync(context.Guild);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_fc_list")]
        public async Task ListFilters(CommandContext context)
        {
            var embed = new DiscordEmbedBuilder();
            var filters = _service.GetContentFilters(context.Guild)
                .Where(x => x.IsActive);

            if (!filters.Any())
            {
                embed.WithDescription("fc_list_empty");
                await context.RespondLocalizedAsync(embed, isError: true);

                return;
            }

            var fields = new List<SerializableEmbedField>();

            foreach (var filterGroup in filters.SplitInto(AkkoConstants.LinesPerPage))
            {
                fields.Add(new("id", string.Join("\n", filterGroup.Select(x => x.Id)), true));
                fields.Add(new("channel", string.Join("\n", filterGroup.Select(x => $"<#{x.ChannelId}>")), true));
                fields.Add(new("fc_allow", string.Join("\n", filterGroup.Select(x => string.Join(", ", x.ActiveFilters.Select(y => context.FormatLocalized(y))))), true));
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
        private async Task<DiscordMessage> SetPropertyAsync(CommandContext context, DiscordChannel channel, string responseKey, Func<FilteredContentEntity, bool> selector)
        {
            channel ??= context.Channel;
            var result = await _service.SetContentFilterAsync(context.Guild, channel, selector);
            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized(responseKey, (result) ? "enabled" : "disabled", channel.Mention));

            return await context.RespondLocalizedAsync(embed);
        }
    }
}