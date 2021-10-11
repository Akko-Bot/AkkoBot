using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Commands.Modules.Utilities.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration
{
    [Group("log")]
    [Description("cmd_log")]
    [RequireGuild]
    [RequirePermissions(Permissions.ManageWebhooks)]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public sealed class GuildLogging : AkkoCommandModule
    {
        private readonly GuildConfigService _guilldService;
        private readonly GuildLogService _service;
        private readonly UtilitiesService _utilities;
        private readonly DiscordWebhookClient _webhookClient;

        public GuildLogging(GuildConfigService guilldService, GuildLogService service, UtilitiesService utilities, DiscordWebhookClient webhookClient)
        {
            _guilldService = guilldService;
            _service = service;
            _utilities = utilities;
            _webhookClient = webhookClient;
        }

        [GroupCommand, Command("start")]
        [Description("cmd_log_start")]
        public async Task StartLogAsync(
            CommandContext context,
            [Description("arg_guildlog_type")] GuildLogType logType,
            [Description("arg_discord_channel")] DiscordChannel channel = null,
            [Description("arg_webhook_name")] string webhookName = null,
            [Description("arg_emoji_url")] string avatarUrl = null)
        {
            channel ??= context.Channel;

            using var avatarStream = (string.IsNullOrWhiteSpace(avatarUrl))
                ? await _utilities.GetOnlineStreamAsync(context.Guild.CurrentMember.AvatarUrl ?? context.Guild.CurrentMember.DefaultAvatarUrl)
                : await _utilities.GetOnlineStreamAsync(avatarUrl);

            await _service.StartLogAsync(context, channel, logType, webhookName?.MaxLength(AkkoConstants.MaxUsernameLength), avatarStream);
            var message = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized("log_started", Formatter.InlineCode(logType.ToString()), channel.Mention));

            await context.RespondLocalizedAsync(message);
        }

        [Command("stop")]
        [Description("cmd_log_stop")]
        public async Task StopLogAsync(CommandContext context, [Description("arg_guildlog_type")] GuildLogType logType)
        {
            var result = await _service.StopLogAsync(context, logType);
            var message = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized(((result) ? "log_stopped_success" : "log_stopped_failure"), Formatter.InlineCode(logType.ToString())));

            await context.RespondLocalizedAsync(message, isError: !result);
        }

        [Command("edit")]
        [Description("cmd_log_edit")]
        public async Task EditLogAsync(
            CommandContext context,
            [Description("arg_discord_channel")] DiscordChannel channel,
            [Description("arg_webhook_name")] string newName,
            [Description("arg_emoji_url")] string avatarUrl = null)
        {
            var guildLog = _service.GetGuildLogs(context.Guild)
                .FirstOrDefault(x => x.ChannelId == channel.Id);

            if (guildLog is null)
            {
                await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
                return;
            }

            await _webhookClient.TryAddAsync(guildLog.WebhookId, context.Client);
            var webhook = _webhookClient.GetRegisteredWebhook(guildLog.WebhookId);

            if (webhook is null)
            {
                await _service.StopLogAsync(context, guildLog.Type, true);
                await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);

                return;
            }

            using var avatarStream = (string.IsNullOrWhiteSpace(avatarUrl))
                ? await _utilities.GetOnlineStreamAsync(webhook.AvatarUrl ?? context.Guild.CurrentMember.AvatarUrl ?? context.Guild.CurrentMember.DefaultAvatarUrl)
                : await _utilities.GetOnlineStreamAsync(avatarUrl);

            await webhook.ModifyAsync(newName, avatarStream);
            await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
        }

        [Command("ignore")]
        [Description("cmd_log_ignore")]
        public async Task IgnoreChannelAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel)
        {
            var result = await _guilldService.SetPropertyAsync(context.Guild, x => ToggleElement(x.GuildLogBlacklist, (long)channel.Id));

            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized((result) ? "log_ignore_add" : "log_ignore_remove", channel.Mention));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("ignoreclear")]
        [Description("cmd_log_ignoreclear")]
        public async Task IgnoreChannelClearAsync(CommandContext context)
        {
            var result = await _guilldService.SetPropertyAsync(context.Guild, x =>
            {
                var amount = x.GuildLogBlacklist.Count;
                x.GuildLogBlacklist.Clear();

                return amount is not 0;
            });

            await context.Message.CreateReactionAsync((result) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("list"), Aliases("show")]
        [Description("cmd_log_list")]
        public async Task ListGuildLogsAsync(CommandContext context)
        {
            var guildLogs = _service.GetGuildLogs(context.Guild);

            var message = new SerializableDiscordEmbed()
                .WithTitle("log_list_title")
                .WithDescription(
                    string.Join(
                        "\n",
                        Enum.GetValues<GuildLogType>()
                            .Where(x => x is not GuildLogType.None and not GuildLogType.All and not GuildLogType.Unknown and not GuildLogType.UserPresence)
                            .Select(x => $"{x}\t{GetChannelMention(context.Guild, guildLogs.FirstOrDefault(y => y.Type == x))}")
                    )
                );

            await context.RespondLocalizedAsync(message, false);
        }

        /// <summary>
        /// Gets the channel mention of the specified channel.
        /// </summary>
        /// <param name="server">The guild the channel is in.</param>
        /// <param name="guildLog">The guild log.</param>
        /// <returns>The channel mention or <see langword="null"/> if <paramref name="guildLog"/> is <see langword="null"/> or not active.</returns>
        private string GetChannelMention(DiscordGuild server, GuildLogEntity guildLog)
        {
            return (guildLog is null || !guildLog.IsActive || !server.Channels.TryGetValue(guildLog.ChannelId, out var channel))
                ? null
                : channel.Mention;
        }

        /// <summary>
        /// Adds or removes an element from the specified collection.
        /// </summary>
        /// <typeparam name="T">The type of the data.</typeparam>
        /// <param name="collection">The collection.</param>
        /// <param name="element">The element to be added or removed.</param>
        /// <returns><see langword="true"/> if the element got added, <see langword="false"/> otherwise.</returns>
        private bool ToggleElement<T>(IList<T> collection, T element)
        {
            var result = collection.Contains(element);

            if (result)
                collection.Remove(element);
            else
                collection.Add(element);

            return !result;
        }
    }
}