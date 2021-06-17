using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Common;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration
{
    [RequireGuild]
    [Group("filterword"), Aliases("fw")]
    [Description("cmd_fw")]
    public class WordFilter : AkkoCommandModule
    {
        private readonly WordFilterService _service;

        public WordFilter(WordFilterService service)
            => _service = service;

        [Command("add")]
        [Description("cmd_fw_add")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task AddWord(CommandContext context, [RemainingText, Description("arg_fw_word")] string word)
        {
            var success = await _service.AddFilteredWordsAsync(context.Guild.Id, word);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("addmany")]
        [Description("cmd_fw_addmany")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task AddManyWords(CommandContext context, [Description("arg_fw_words")] params string[] words)
        {
            var success = await _service.AddFilteredWordsAsync(context.Guild.Id, words);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("message")]
        [Description("cmd_fw_message")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task SetNotificationMessage(CommandContext context, [RemainingText, Description("arg_fw_message")] string message)
        {
            await _service.SetWordFilterSettingsAsync(context.Guild.Id, x => x.NotificationMessage = message);
            await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
        }

        [Command("notify")]
        [Description("cmd_fw_notify")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task ToggleNotification(CommandContext context)
        {
            var isEnabled = await _service.SetWordFilterSettingsAsync(context.Guild.Id, x => x.NotifyOnDelete = !x.NotifyOnDelete);
            var embed = new DiscordEmbedBuilder { Description = context.FormatLocalized("fw_notify", (isEnabled) ? "enabled" : "disabled") };

            await context.RespondLocalizedAsync(embed);
        }

        [Command("warn")]
        [Description("cmd_fw_warn")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task ToggleWarnOnDelete(CommandContext context)
        {
            var isEnabled = await _service.SetWordFilterSettingsAsync(context.Guild.Id, x => x.WarnOnDelete = !x.WarnOnDelete);
            var embed = new DiscordEmbedBuilder { Description = context.FormatLocalized("fw_warn", (isEnabled) ? "enabled" : "disabled") };

            await context.RespondLocalizedAsync(embed);
        }

        [Command("addignore")]
        [Description("cmd_fw_addignore")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task AddIgnoredId(CommandContext context, [Description("arg_fw_ids")] params ulong[] ids)
        {
            var success = await _service.AddIgnoredIdsAsync(context.Guild.Id, ids);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("removeignore"), Aliases("rmignore")]
        [Description("cmd_fw_removeignore")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task RemoveIgnoredId(CommandContext context, [Description("arg_fw_ids")] params ulong[] ids)
        {
            var result = await _service.SetWordFilterSettingsAsync(context.Guild.Id, x => x.IgnoredIds.RemoveAll(x => ids.Contains((ulong)x)));
            await context.Message.CreateReactionAsync((result is not 0) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("remove"), Aliases("rm")]
        [Description("cmd_fw_remove")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task RemoveWord(CommandContext context, [RemainingText, Description("arg_fw_word")] string word)
        {
            var success = await _service.RemoveFilteredWordsAsync(context.Guild.Id, word);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("removemany"), Aliases("rmmany")]
        [Description("cmd_fw_removemany")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task RemoveManyWords(CommandContext context, [Description("arg_fw_words")] params string[] words)
        {
            var success = await _service.RemoveFilteredWordsAsync(context.Guild.Id, words);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("clear")]
        [Description("cmd_fw_clear")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task ClearWords(CommandContext context)
        {
            var success = await _service.ClearFilteredWordsAsync(context.Guild.Id);
            await context.Message.CreateReactionAsync((success) ? AkkoEntities.SuccessEmoji : AkkoEntities.FailureEmoji);
        }

        [Command("enable")]
        [Description("cmd_fw_enable")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task Enable(CommandContext context)
        {
            var success = await _service.SetWordFilterSettingsAsync(context.Guild.Id, x => x.IsActive = !x.IsActive);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("fw_toggle", (success) ? "enabled" : "disabled"));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("invites"), Aliases("invite")]
        [Description("cmd_fw_invites")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task ToggleInviteRemoval(CommandContext context)
        {
            var success = await _service.SetWordFilterSettingsAsync(context.Guild.Id, x => x.FilterInvites = !x.FilterInvites);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("fw_invite_toggle", (success) ? "enabled" : "disabled"));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("stickers")]
        [Description("cmd_fw_stickers")]
        [RequireUserPermissions(Permissions.ManageGuild)]
        public async Task ToggleStickerRemoval(CommandContext context)
        {
            var success = await _service.SetWordFilterSettingsAsync(context.Guild.Id, x => x.FilterStickers = !x.FilterStickers);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("fw_sticker_toggle", (success) ? "enabled" : "disabled"));

            await context.RespondLocalizedAsync(embed);
        }

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_fw_list")]
        public async Task ListFilteredWords(CommandContext context)
        {
            var dbEntry = _service.GetFilteredWords(context.Guild.Id);
            var words = dbEntry?.Words
                .OrderBy(x => x.Trim('*'))
                .Select(x => Formatter.InlineCode(x))
                .ToArray();

            var isEmpty = dbEntry is null || words.Length == 0;

            var embed = new DiscordEmbedBuilder()
                .WithDescription((isEmpty) ? "fw_list_empty" : string.Join(", ", words).MaxLength(AkkoConstants.MaxEmbedDescriptionLength, "[...]"));

            if (!isEmpty)
            {
                embed.WithTitle("fw_list_title");

                var channels = context.Guild.Channels.Values
                    .Where(x => dbEntry.IgnoredIds.Contains((long)x.Id))
                    .OrderBy(x => x.Name)
                    .Select(x => '#' + x.Name)
                    .ToArray();

                var roles = context.Guild.Roles.Values
                    .Where(x => dbEntry.IgnoredIds.Contains((long)x.Id))
                    .OrderBy(x => x.Name)
                    .Select(x => x.Name)
                    .ToArray();

                var members = context.Guild.Members.Values
                    .Where(x => dbEntry.IgnoredIds.Contains((long)x.Id))
                    .OrderBy(x => x.Username)
                    .Select(x => x.GetFullname())
                    .ToArray();

                if (channels.Length != 0)
                    embed.AddField("fw_ignored_channels", string.Join(", ", channels).MaxLength(AkkoConstants.MaxEmbedFieldLength, "[...]"));

                if (roles.Length != 0)
                    embed.AddField("fw_ignored_roles", string.Join(", ", roles).MaxLength(AkkoConstants.MaxEmbedFieldLength, "[...]"));

                if (members.Length != 0)
                    embed.AddField("fw_ignored_users", string.Join(", ", members).MaxLength(AkkoConstants.MaxEmbedFieldLength, "[...]"));

                if (dbEntry.FilterInvites || dbEntry.FilterStickers)
                {
                    var extraFilters = dbEntry.GetSettings()
                        .Where(x => x.Key is "filter_invites" or "filter_stickers" && x.Value is "True")
                        .Select(x => context.FormatLocalized(x.Key));

                    embed.WithFooter($"{context.FormatLocalized("fw_extra_filters")}: {string.Join(", ", extraFilters)}");
                }
            }

            await context.RespondLocalizedAsync(embed, isEmpty, isEmpty);
        }
    }
}