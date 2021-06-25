using AkkoBot.Commands.Abstractions;
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
        private readonly Permissions _voicePerms = Permissions.UseVoice | Permissions.UseVoiceDetection | Permissions.Speak | Permissions.MuteMembers
            | Permissions.PrioritySpeaker | Permissions.Stream | Permissions.DeafenMembers | Permissions.MoveMembers;

        private readonly UtilitiesService _service;

        public GuildUtilities(UtilitiesService service)
            => _service = service;

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
            if (!GeneralService.IsOwner(context, context.Member.Id) || server.Channels.Count == 0)
                return;

            await context.RespondLocalizedAsync(_service.GetServerInfo(context, server), false);
        }

        [Command("channelinfo"), Aliases("cinfo")]
        [Description("cmd_channelinfo")]
        public async Task ChannelInfoAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
        {
            channel ??= context.Channel;

            var embed = _service.GetChannelInfo(new DiscordEmbedBuilder(), channel)
                .WithFooter(context.FormatLocalized("{0}: {1}", "created_at", channel.CreationTimestamp.ToString("d", GeneralService.GetCultureInfo(context.GetLocaleKey(), true))));

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("userinfo"), Aliases("uinfo")]
        [Description("cmd_userinfo")]
        public async Task UserInfoAsync(CommandContext context, [Description("arg_discord_user")] DiscordMember user = null)
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
        public async Task UserInfoAsync(CommandContext context, DiscordUser user)
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
        public async Task EditMessageAsync(CommandContext context, [Description("arg_discord_message")] DiscordMessage message, [RemainingText, Description("arg_edit_message")] SmartString newMessage)
        {
            if (message.Author.Id != context.Guild.CurrentMember.Id)
            {
                await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
                return;
            }
            _ = (_service.DeserializeEmbed(newMessage, out var dMsg))
                ? await message.ModifyAsync(dMsg)
                : await message.ModifyAsync(newMessage, null);

            await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
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
                await Task.Delay(AkkoEntities.SafetyDelay);
            }

            await context.Message.CreateReactionAsync((canReact) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
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
                await Task.Delay(AkkoEntities.SafetyDelay);
            }

            await context.Message.CreateReactionAsync((hasReaction) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
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
                await Task.Delay(AkkoEntities.SafetyDelay);
            }

            await context.Message.CreateReactionAsync((hasReaction) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
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

            var allPerms = Enum.GetValues<Permissions>()
                .Where(x => x is not Permissions.All and not Permissions.None and not Permissions.ViewAuditLog and not Permissions.Administrator)
                .Where((channel.Type is ChannelType.Voice) ? x => x.HasOneFlag(_voicePerms) : x => !x.HasOneFlag(_voicePerms) || x is Permissions.MuteMembers);

            var allowedPerms = string.Join(
                "\n",
                allPerms
                    .Where(x => user.PermissionsIn(channel).HasFlag(x))
                    .Select(x => context.FormatLocalized("perm_" + x.ToString().ToSnakeCase()))
                    .OrderBy(x => x)
            );

            var deniedPerms = string.Join(
                "\n",
                allPerms
                    .Where(x => !user.PermissionsIn(channel).HasFlag(x))
                    .Select(x => context.FormatLocalized("perm_" + x.ToString().ToSnakeCase()))
                    .OrderBy(x => x)
            );

            if (string.IsNullOrWhiteSpace(allowedPerms))
                allowedPerms = AkkoConstants.ValidWhitespace;

            if (string.IsNullOrWhiteSpace(deniedPerms))
                deniedPerms = AkkoConstants.ValidWhitespace;


            var embed = new DiscordEmbedBuilder()
                .WithTitle(context.FormatLocalized("checkperms_title", user.GetFullname(), channel.Name))
                .AddField("allowed", allowedPerms.MaxLength(AkkoConstants.MaxEmbedFieldLength, "[...]"), true)
                .AddField("denied", deniedPerms.MaxLength(AkkoConstants.MaxEmbedFieldLength, "[...]"), true);

            await context.RespondLocalizedAsync(embed);
        }

        [Command("checkperms"), HiddenOverload]
        public async Task CheckUserPermsAsync(CommandContext context, DiscordMember user = null)
            => await CheckUserPermsAsync(context, null, user);

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