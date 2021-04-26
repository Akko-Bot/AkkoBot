﻿using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Commands.Common;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities
{
    [RequireGuild]
    public class GuildUtilities : AkkoCommandModule
    {
        private readonly UtilitiesService _service;

        public GuildUtilities(UtilitiesService service)
            => _service = service;

        [Command("say"), HiddenOverload]
        [Priority(0)]
        public async Task Say(CommandContext context, [RemainingText] SmartString message)
            => await Say(context, context.Channel, message);

        [Command("say")]
        [Description("cmd_say")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Priority(1)]
        public async Task Say(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel, [RemainingText, Description("arg_say")] SmartString message)
        {
            if (string.IsNullOrWhiteSpace(message?.Content))    // If command only contains a channel name
                await context.RespondAsync(channel.Name);
            else if (_service.DeserializeEmbed(message.Content, out var parsedMessage)) // If command contains an embed in yaml format
                await channel.SendMessageAsync(parsedMessage);
            else    // If command is just plain text
                await channel.SendMessageAsync(message.Content);
        }

        [Command("serverinfo"), Aliases("sinfo")]
        [Description("cmd_serverinfo")]
        public async Task ServerInfo(CommandContext context)
            => await context.RespondLocalizedAsync(_service.GetServerInfo(context, context.Guild), false);

        [Command("serverinfo"), HiddenOverload]
        public async Task ServerInfo(CommandContext context, DiscordGuild server)
        {
            if (!GeneralService.IsOwner(context, context.Member.Id) || server.Channels.Count == 0)
                return;

            await context.RespondLocalizedAsync(_service.GetServerInfo(context, server), false);
        }

        [Command("channelinfo"), Aliases("cinfo")]
        [Description("cmd_channelinfo")]
        public async Task ChannelInfo(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
        {
            channel ??= context.Channel;

            var embed = _service.GetChannelInfo(new DiscordEmbedBuilder(), channel)
                .WithFooter(context.FormatLocalized("{0}: {1}", "created_at", channel.CreationTimestamp.ToString("d", GeneralService.GetCultureInfo(context.GetLocaleKey(), true))));

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("userinfo"), Aliases("uinfo")]
        [Description("cmd_userinfo")]
        public async Task UserInfo(CommandContext context, [Description("arg_discord_user")] DiscordMember user = null)
        {
            user ??= context.Member;
            var isMod = user.Hierarchy is int.MaxValue || user.Roles.Any(role => role.Permissions.HasOneFlag(Permissions.Administrator | Permissions.KickMembers | Permissions.BanMembers));

            var embed = new DiscordEmbedBuilder()
                .WithThumbnail(user.AvatarUrl ?? user.DefaultAvatarUrl)
                .AddField("name", user.GetFullname(), true)
                .AddField("nickname", user.Nickname ?? "-", true)
                .AddField("id", user.Id.ToString(), true)
                .AddField("is_mod", (isMod) ? AkkoEntities.SuccessEmoji.Name : AkkoEntities.FailureEmoji.Name, true)
                .AddField("roles", user.Roles.Count().ToString(), true)
                .AddField("position", user.Hierarchy.ToString(), true)
                .AddField("created_at", user.CreationTimestamp.DateTime.ToString(GeneralService.GetCultureInfo(context.GetLocaleKey(), true)), true)
                .AddField("joined_at", user.JoinedAt.DateTime.ToString(GeneralService.GetCultureInfo(context.GetLocaleKey(), true)), true);

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("userinfo"), HiddenOverload]
        public async Task UserInfo(CommandContext context, DiscordUser user)
        {
            var embed = new DiscordEmbedBuilder()
                .WithThumbnail(user.AvatarUrl ?? user.DefaultAvatarUrl)
                .AddField("name", user.GetFullname(), true)
                .AddField("id", user.Id.ToString(), true)
                .AddField("created_at", user.CreationTimestamp.DateTime.ToString(GeneralService.GetCultureInfo(context.GetLocaleKey(), true)), false);

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("edit")]
        [Description("cmd_edit")]
        [RequireUserPermissions(Permissions.ManageMessages)]
        public async Task EditMessage(CommandContext context, [Description("arg_discord_message")] DiscordMessage message, [RemainingText, Description("arg_edit_message")] SmartString newMessage)
        {
            if (message.Author.Id != context.Guild.CurrentMember.Id)
            {
                await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
                return;
            }
            _ = (_service.DeserializeEmbed(newMessage.Content, out var dMsg))
                ? await message.ModifyAsync(dMsg)
                : await message.ModifyAsync(newMessage.Content, null);

            await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
        }

        [Command("edit"), HiddenOverload]
        public async Task EditMessage(CommandContext context, ulong messageId, [RemainingText] SmartString newMessage)
            => await GetMessageAndExecuteAsync(context, messageId, (message) => EditMessage(context, message, newMessage));

        [Command("react")]
        [Description("cmd_react")]
        [RequireUserPermissions(Permissions.ManageMessages)]
        public async Task React(CommandContext context, [Description("arg_discord_message")] DiscordMessage message, [Description("arg_emoji")] DiscordEmoji emoji)
        {
            var canReact = _service.CanUseEmoji(context.Guild, message.Channel, emoji);

            if (canReact)
            {
                await message.CreateReactionAsync(emoji);
                await Task.Delay(AkkoEntities.SafetyDelay);
            }

            await context.Message.CreateReactionAsync((canReact) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("react"), HiddenOverload]
        public async Task React(CommandContext context, ulong messageId, DiscordEmoji emoji)
            => await GetMessageAndExecuteAsync(context, messageId, (message) => React(context, message, emoji));

        [Command("reactremove"), Aliases("reactrm")]
        [Description("cmd_reactremove")]
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task DeleteReaction(CommandContext context, [Description("arg_discord_message")] DiscordMessage message, [Description("arg_emoji")] DiscordEmoji emoji)
        {
            var hasReaction = message.Reactions.Any(x => x.Emoji.Equals(emoji));

            if (hasReaction)
            {
                await message.DeleteReactionsEmojiAsync(emoji);
                await Task.Delay(AkkoEntities.SafetyDelay);
            }

            await context.Message.CreateReactionAsync((hasReaction) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("reactremove"), HiddenOverload]
        public async Task DeleteReaction(CommandContext context, ulong messageId, DiscordEmoji emoji)
            => await GetMessageAndExecuteAsync(context, messageId, (message) => DeleteReaction(context, message, emoji));

        [Command("reactclear")]
        [Description("cmd_reactclear")]
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task ClearReactions(CommandContext context, [Description("arg_discord_message")] DiscordMessage message)
        {
            var hasReaction = message.Reactions.Count > 0;

            if (hasReaction)
            {
                await message.DeleteAllReactionsAsync();
                await Task.Delay(AkkoEntities.SafetyDelay);
            }

            await context.Message.CreateReactionAsync((hasReaction) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("reactclear"), HiddenOverload]
        public async Task ClearReactions(CommandContext context, ulong messageId)
            => await GetMessageAndExecuteAsync(context, messageId, (message) => ClearReactions(context, message));

        /// <summary>
        /// Safely gets a Discord message with the specified ID in the context channel and executes a follow-up method with it.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="messageId">The message ID.</param>
        /// <param name="action">The method to be executed.</param>
        private async Task GetMessageAndExecuteAsync(CommandContext context, ulong messageId, Func<DiscordMessage, Task> action)
        {
            try
            {
                var message = await context.Channel.GetMessageAsync(messageId);
                await action(message);
            }
            catch
            {
                await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
            }
        }
    }
}