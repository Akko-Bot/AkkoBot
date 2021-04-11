using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities
{
    [RequireGuild]
    [Group("emoji"), Aliases("emote", "emojis", "emotes")]
    [Description("cmd_emoji")]
    public class GuildEmojis : AkkoCommandModule
    {
        private const double _additionalDelay = 1.4;
        private readonly UtilitiesService _service;

        public GuildEmojis(UtilitiesService service)
            => _service = service;

        [GroupCommand, Command("list")]
        [Description("cmd_emoji_list")]
        public async Task CheckGuildEmoji(CommandContext context)
        {
            var fields = new List<SerializableEmbedField>();
            var embed = new DiscordEmbedBuilder()
                .WithTitle("emoji_list_title");

            var emojis = context.Guild.Emojis.Values
                .Where(emoji => emoji.IsAvailable)
                .OrderBy(x => x.Name)
                .OrderBy(x => x.IsAnimated) // ThenBy() doesn't do anything here, for some reason
                .SplitInto(15);

            foreach (var emojiGroup in emojis)
            {
                fields.Add(new("emojis", string.Join('\n', emojiGroup.Select(emoji => $"{emoji} {emoji.GetDiscordName()}").ToArray()), true));
                fields.Add(new("exclusive", string.Join('\n', emojiGroup.Select(x => (x.Roles.Count != 0) ? AkkoEntities.SuccessEmoji.Name : AkkoEntities.FailureEmoji.Name).ToArray()), true));
            }

            await context.RespondPaginatedByFieldsAsync(embed, fields, 2);
        }

        [Command("show"), Aliases("showemoji", "se")]
        [Description("cmd_emoji_showemoji")]
        public async Task ShowEmoji(CommandContext context, [Description("arg_emojis")] params DiscordEmoji[] emojis)
        {
            var result = new StringBuilder();

            foreach (var emoji in emojis.Where(emoji => emoji.Id is not 0))
                result.AppendLine($"{emoji} {Formatter.InlineCode(emoji.GetDiscordName())} {emoji.Url}");

            await context.RespondAsync(result.ToString());
        }

        [Command("add")]
        [Description("cmd_emoji_add")]
        [RequirePermissions(Permissions.ManageEmojis)]
        public async Task AddEmojis(CommandContext context, [Description("arg_emojis")] params DiscordEmoji[] emojis)
        {
            var success = false;
            await context.TriggerTypingAsync();

            foreach (var emoji in emojis)
            {
                success |= await _service.AddGuildEmojiAsync(context, emoji, emoji.Name);
                await Task.Delay(AkkoEntities.SafetyDelay.Add(TimeSpan.FromSeconds(_additionalDelay)));
            }

            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("add")]
        public async Task AddEmoji(
            CommandContext context,
            [Description("arg_emoji_url")] Uri url,
            [Description("arg_emoji_name")] string name = null)
        {
            var success = await _service.AddGuildEmojiAsync(context, url, name);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("add")]
        public async Task AddEmoji(
            CommandContext context,
            [Description("arg_emoji")] DiscordEmoji emoji,
            [Description("arg_emoji_name")] string name)
        {
            var success = await _service.AddGuildEmojiAsync(context, emoji, name);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("add")]
        public async Task AddEmojiAttachments(CommandContext context)
        {
            var success = await _service.AddGuildEmojisAsync(context, context.Message.Attachments);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("remove"), Aliases("rm")]
        [Description("cmd_emoji_remove")]
        [RequirePermissions(Permissions.ManageEmojis)]
        public async Task RemoveEmojis(CommandContext context, [Description("arg_emojis")] params DiscordEmoji[] emojis)
        {
            var success = false;

            if (emojis.Length == 1)
            {
                var gEmoji = await emojis[0].ToGuildEmojiAsync(context.Guild);
                await gEmoji?.DeleteAsync();

                success = gEmoji is not null;
            }
            else
            {
                await context.TriggerTypingAsync();

                foreach (var emoji in await emojis.ToGuildEmojisAsync(context.Guild))
                {
                    await Task.Delay(AkkoEntities.SafetyDelay.Add(TimeSpan.FromSeconds(_additionalDelay)));
                    await emoji.DeleteAsync();

                    success = true;
                }
            }

            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("rename"), Aliases("ren")]
        [Description("cmd_emoji_rename")]
        [RequirePermissions(Permissions.ManageEmojis)]
        public async Task RenameEmoji(
            CommandContext context,
            [Description("arg_emoji")] DiscordEmoji emoji,
            [Description("arg_emoji_newname")] string newName)
        {
            var guildEmoji = await emoji.ToGuildEmojiAsync(context.Guild);

            if (guildEmoji is not null)
                await context.Guild.ModifyEmojiAsync(guildEmoji, newName.SanitizeEmojiName());

            await context.Message.CreateReactionAsync((guildEmoji is not null) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("setrole"), Aliases("setroles")]
        [Description("cmd_emoji_setrole")]
        [RequirePermissions(Permissions.ManageEmojis | Permissions.ManageRoles)]
        public async Task AddRoles(
            CommandContext context,
            [Description("arg_emoji")] DiscordEmoji emoji,
            [Description("arg_discord_roles")] params DiscordRole[] roles)
        {
            var guildEmoji = await emoji.ToGuildEmojiAsync(context.Guild);

            if (guildEmoji is not null)
                await context.Guild.ModifyEmojiAsync(guildEmoji, guildEmoji.Name, roles);

            await context.Message.CreateReactionAsync((guildEmoji is not null) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("removerole"), Aliases("removeroles")]
        [Description("cmd_emoji_removerole")]
        [RequirePermissions(Permissions.ManageEmojis | Permissions.ManageRoles)]
        public async Task RemoveRoles(CommandContext context, [Description("arg_emoji")] DiscordEmoji emoji)
        {
            var guildEmoji = await emoji.ToGuildEmojiAsync(context.Guild);

            if (guildEmoji is not null)
                await context.Guild.ModifyEmojiAsync(guildEmoji, guildEmoji.Name);

            await context.Message.CreateReactionAsync((guildEmoji is not null) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }
    }
}