using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Common;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Commands.Modules.Utilities.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Utilities
{
    [RequireGuild]
    public class GuildUtilities : AkkoCommandModule
    {
        private readonly Permissions _voicePerms = Permissions.UseVoice | Permissions.UseVoiceDetection | Permissions.Speak | Permissions.MuteMembers
            | Permissions.PrioritySpeaker | Permissions.Stream | Permissions.DeafenMembers | Permissions.MoveMembers;

        private readonly UtilitiesService _service;
        private readonly GuildLogService _logService;

        public GuildUtilities(UtilitiesService service, GuildLogService logService)
        {
            _service = service;
            _logService = logService;
        }

        [Command("say"), HiddenOverload]
        [Priority(0)]
        public async Task SayAsync(CommandContext context, [RemainingText] SmartString message)
            => await SayAsync(context, context.Channel, message);

        [Command("say")]
        [Description("cmd_say")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Priority(1)]
        public async Task SayAsync(CommandContext _, [Description("arg_discord_channel")] DiscordChannel channel, [RemainingText, Description("arg_say")] SmartString message)
        {
            if (string.IsNullOrWhiteSpace(message))    // If command only contains a channel name
                await channel.SendMessageAsync(channel.Name);
            else if (_service.DeserializeEmbed(message, out var parsedMessage)) // If command contains an embed in yaml format
                await channel.SendMessageAsync(parsedMessage);
            else    // If command is just plain text
                await channel.SendMessageAsync(message);
        }

        [Command("serverinfo"), Aliases("sinfo")]
        [Description("cmd_serverinfo")]
        public async Task ServerInfoAsync(CommandContext context)
            => await context.RespondLocalizedAsync(_service.GetServerInfo(context, context.Guild), false);

        [Command("serverinfo"), HiddenOverload]
        public async Task ServerInfoAsync(CommandContext context, DiscordGuild server)
        {
            if (!AkkoUtilities.IsOwner(context, context.Member.Id) || server.Channels.Count == 0)
                return;

            await context.RespondLocalizedAsync(_service.GetServerInfo(context, server), false);
        }

        [Command("channelinfo"), Aliases("cinfo")]
        [Description("cmd_channelinfo")]
        public async Task ChannelInfoAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
        {
            channel ??= context.Channel;

            var settings = context.GetMessageSettings();
            var embed = _service.GetChannelInfo(new SerializableDiscordEmbed(), channel)
                .WithFooter(context.FormatLocalized("{0}: {1}", "created_on", channel.CreationTimestamp.ToString("d", AkkoUtilities.GetCultureInfo(settings.Locale, true))));

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("userinfo"), Aliases("uinfo")]
        [Description("cmd_userinfo")]
        public async Task UserInfoAsync(CommandContext context, [Description("arg_discord_user")] DiscordMember user = null)
        {
            user ??= context.Member;
            var settings = context.GetMessageSettings();
            var isMod = user.Hierarchy is int.MaxValue || user.Roles.Any(role => role.Permissions.HasOneFlag(Permissions.Administrator | Permissions.KickMembers | Permissions.BanMembers));

            var embed = new SerializableDiscordEmbed()
                .WithThumbnail(user.AvatarUrl ?? user.DefaultAvatarUrl)
                .AddField("name", user.GetFullname(), true)
                .AddField("nickname", user.Nickname ?? "-", true)
                .AddField("id", user.Id.ToString(), true)
                .AddField("is_mod", (isMod) ? AkkoStatics.SuccessEmoji.Name : AkkoStatics.FailureEmoji.Name, true)
                .AddField("roles", user.Roles.Count().ToString(), true)
                .AddField("position", user.Hierarchy.ToString(), true)
                .AddField("created_on", user.CreationTimestamp.DateTime.ToString(AkkoUtilities.GetCultureInfo(settings.Locale, true)), true)
                .AddField("joined_on", user.JoinedAt.DateTime.ToString(AkkoUtilities.GetCultureInfo(settings.Locale, true)), true);

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("userinfo"), HiddenOverload]
        public async Task UserInfoAsync(CommandContext context, DiscordUser user)
        {
            var settings = context.GetMessageSettings();
            var embed = new SerializableDiscordEmbed()
                .WithThumbnail(user.AvatarUrl ?? user.DefaultAvatarUrl)
                .AddField("name", user.GetFullname(), true)
                .AddField("id", user.Id.ToString(), true)
                .AddField("created_on", user.CreationTimestamp.DateTime.ToString(AkkoUtilities.GetCultureInfo(settings.Locale, true)), false);

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("edit")]
        [Description("cmd_edit")]
        [RequireUserPermissions(Permissions.ManageMessages)]
        public async Task EditMessageAsync(CommandContext context, [Description("arg_discord_message")] DiscordMessage message, [RemainingText, Description("arg_edit_message")] SmartString newMessage)
        {
            if (message.Author.Id != context.Guild.CurrentMember.Id)
            {
                await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
                return;
            }
            _ = (_service.DeserializeEmbed(newMessage, out var dMsg))
                ? await message.ModifyAsync(dMsg)
                : await message.ModifyAsync(newMessage);

            await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
        }

        [Command("edit"), HiddenOverload]
        public async Task EditMessageAsync(CommandContext context, ulong messageId, [RemainingText] SmartString newMessage)
            => await GetMessageAndExecuteAsync(context, messageId, (message) => EditMessageAsync(context, message, newMessage));

        [Command("react")]
        [Description("cmd_react")]
        [RequireUserPermissions(Permissions.ManageMessages)]
        public async Task ReactAsync(CommandContext context, [Description("arg_discord_message")] DiscordMessage message, [Description("arg_emoji")] DiscordEmoji emoji)
        {
            var canReact = _service.CanUseEmoji(context.Guild, message.Channel, emoji);

            if (canReact)
            {
                await message.CreateReactionAsync(emoji);
                await Task.Delay(AkkoStatics.SafetyDelay);
            }

            await context.Message.CreateReactionAsync((canReact) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("react"), HiddenOverload]
        public async Task ReactAsync(CommandContext context, ulong messageId, DiscordEmoji emoji)
            => await GetMessageAndExecuteAsync(context, messageId, (message) => ReactAsync(context, message, emoji));

        [Command("reactremove"), Aliases("reactrm")]
        [Description("cmd_reactremove")]
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task DeleteReactionAsync(CommandContext context, [Description("arg_discord_message")] DiscordMessage message, [Description("arg_emoji")] DiscordEmoji emoji)
        {
            var hasReaction = message.Reactions.Any(x => x.Emoji.Equals(emoji));

            if (hasReaction)
            {
                await message.DeleteReactionsEmojiAsync(emoji);
                await Task.Delay(AkkoStatics.SafetyDelay);
            }

            await context.Message.CreateReactionAsync((hasReaction) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("reactremove"), HiddenOverload]
        public async Task DeleteReactionAsync(CommandContext context, ulong messageId, DiscordEmoji emoji)
            => await GetMessageAndExecuteAsync(context, messageId, (message) => DeleteReactionAsync(context, message, emoji));

        [Command("reactclear")]
        [Description("cmd_reactclear")]
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task ClearReactionsAsync(CommandContext context, [Description("arg_discord_message")] DiscordMessage message)
        {
            var hasReaction = message.Reactions.Count > 0;

            if (hasReaction)
            {
                await message.DeleteAllReactionsAsync();
                await Task.Delay(AkkoStatics.SafetyDelay);
            }

            await context.Message.CreateReactionAsync((hasReaction) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("reactclear"), HiddenOverload]
        public async Task ClearReactionsAsync(CommandContext context, ulong messageId)
            => await GetMessageAndExecuteAsync(context, messageId, (message) => ClearReactionsAsync(context, message));

        [Command("checkperms")]
        [Description("cmd_checkperms")]
        public async Task CheckUserPermsAsync(
            CommandContext context,
            [Description("arg_discord_channel")] DiscordChannel channel = null,
            [Description("arg_discord_user")] DiscordMember user = null)
        {
            channel ??= context.Channel;
            user ??= context.Member;

            var (allowedPermsCol, deniedPermsCol) = GetLocalizedPermissions(context, user.PermissionsIn(channel), channel.Type);
            var allowedPerms = string.Join("\n", allowedPermsCol);
            var deniedPerms = string.Join("\n", deniedPermsCol);

            var embed = new SerializableDiscordEmbed()
                .WithTitle(context.FormatLocalized("checkperms_title", user.GetFullname(), channel.Name));

            if (!string.IsNullOrWhiteSpace(allowedPerms))
                embed.AddField("allowed", allowedPerms.MaxLength(AkkoConstants.MaxEmbedFieldLength, "[...]"), true);

            if (!string.IsNullOrWhiteSpace(deniedPerms))
                embed.AddField("denied", deniedPerms.MaxLength(AkkoConstants.MaxEmbedFieldLength, "[...]"), true);

            await context.RespondLocalizedAsync(embed);
        }

        [Command("checkperms")]
        public async Task CheckUserPermsAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole role)
        {
            var (allowedPermsCol, deniedPermsCol) = GetLocalizedPermissions(context, role.Permissions);
            var allowedPerms = string.Join("\n", allowedPermsCol);
            var deniedPerms = string.Join("\n", deniedPermsCol);

            var embed = new SerializableDiscordEmbed()
                .WithTitle(context.FormatLocalized("checkperms_role_title", role.Name));

            if (!string.IsNullOrWhiteSpace(allowedPerms))
                embed.AddField("allowed", allowedPerms.MaxLength(AkkoConstants.MaxEmbedFieldLength, "[...]"), true);

            if (!string.IsNullOrWhiteSpace(deniedPerms))
                embed.AddField("denied", deniedPerms.MaxLength(AkkoConstants.MaxEmbedFieldLength, "[...]"), true);

            await context.RespondLocalizedAsync(embed);
        }

        [Command("checkperms"), HiddenOverload]
        public async Task CheckUserPermsAsync(CommandContext context, DiscordMember user = null)
            => await CheckUserPermsAsync(context, null, user);

        [Command("savechat"), HiddenOverload]
        public async Task SaveChatAsync(CommandContext context, int amount)
            => await SaveChatAsync(context, context.Channel, amount);

        [Command("savechat")]
        [Description("cmd_savechat")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task SaveChatAsync(
            CommandContext context,
            [Description("arg_discord_channel")] DiscordChannel channel = null,
            [Description("arg_int")] int amount = 100)
        {
            channel ??= context.Channel;

            if (amount is 0 || !context.Guild.CurrentMember.PermissionsIn(channel).HasPermission(Permissions.AccessChannels))
            {
                await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
                return;
            }

            amount = Math.Abs(amount);
            var requestLimit = (AkkoUtilities.IsOwner(context, context.User.Id))
                ? amount
                : AkkoUtilities.GetMaxMessageRequest(amount, 1);

            var messages = (await channel.GetMessagesAsync(requestLimit))
                .Where(x => !x.Author.IsBot)
                .Take(amount)
                .OrderBy(x => x.CreationTimestamp);

            var log = _logService.GenerateMessageLog(messages, channel, context.GetMessageSettings().Locale);

            if (log is null)
            {
                await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
                return;
            }

            var stream = new MemoryStream(Encoding.UTF8.GetBytes(log));
            var message = new DiscordMessageBuilder()
                .WithFile($"savechat_{channel.Name}_{DateTimeOffset.Now}.txt", stream);

            await context.Channel.SendMessageAsync(message);
        }

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
                await context.Message.CreateReactionAsync(AkkoStatics.FailureEmoji);
            }
        }

        /// <summary>
        /// Gets the localized strings of the allowed and denied permissions from the set of specified permissions.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="permissions">The set of permissions.</param>
        /// <param name="channelType">The type of the Discord channel.</param>
        /// <returns>A collection of localized allowed permissions and a collection of localized denied permissions.</returns>
        private (IEnumerable<string>, IEnumerable<string>) GetLocalizedPermissions(CommandContext context, Permissions permissions, ChannelType? channelType = null)
        {
            var allPerms = Enum.GetValues<Permissions>()
                .Where(x => x is not Permissions.All and not Permissions.None);

            if (channelType is not null)
            {
                allPerms = allPerms.Where(x => x is not Permissions.ViewAuditLog and not Permissions.Administrator)
                    .Where((channelType is ChannelType.Voice) ? x => x.HasOneFlag(_voicePerms) : x => !x.HasOneFlag(_voicePerms) || x is Permissions.MuteMembers);
            }

            var allowedPerms = allPerms
                .Where(x => permissions.HasFlag(x))
                .Select(x => context.FormatLocalized("perm_" + x.ToString().ToSnakeCase()))
                .OrderBy(x => x);

            var deniedPerms = allPerms
                .Where(x => !permissions.HasFlag(x))
                .Select(x => context.FormatLocalized("perm_" + x.ToString().ToSnakeCase()))
                .OrderBy(x => x);

            return (allowedPerms, deniedPerms);
        }
    }
}