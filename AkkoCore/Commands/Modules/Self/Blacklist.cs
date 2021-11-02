using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Self.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Models.Serializable.EmbedParts;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Self
{
    [BotOwner]
    [Group("blacklist"), Aliases("bl")]
    [Description("cmd_blacklist")]
    public sealed class Blacklist : AkkoCommandModule
    {
        private readonly BlacklistService _service;

        public Blacklist(BlacklistService service)
            => _service = service;

        [Command("add"), HiddenOverload]
        public async Task BlacklistAddAsync(CommandContext context, DiscordChannel channel, [RemainingText] string? reason = default)
            => await BlacklistAddAsync(context, BlacklistType.Channel, channel.Id, reason);

        [Command("add")]
        public async Task BlacklistAddAsync(CommandContext context, [Description("arg_mention")] DiscordUser entity, [RemainingText, Description("arg_punishment_reason")] string? reason = default)
            => await BlacklistAddAsync(context, BlacklistType.User, entity.Id, reason);

        [Command("remove"), HiddenOverload]
        public async Task BlacklistRemoveAsync(CommandContext context, DiscordChannel channel)
            => await BlacklistRemoveAsync(context, channel.Id);

        [Command("remove")]
        public async Task BlacklistRemoveAsync(CommandContext context, [Description("arg_mention")] DiscordUser entity)
            => await BlacklistRemoveAsync(context, entity.Id);

        [Command("add")]
        [Description("cmd_blacklist_add")]
        public async Task BlacklistAddAsync(
            CommandContext context,
            [Description("arg_bltype")] BlacklistType type,
            [Description("arg_ulong_id")] ulong id,
            [RemainingText] string? reason = default)
        {
            var (entry, success) = await _service.AddBlacklistAsync(context, type, id, reason);

            var entryName = (string.IsNullOrEmpty(entry.Name))
                ? context.FormatLocalized("unknown")
                : entry.Name;

            // bl_added: Successfully added {0} {1} {2} to the blacklist
            // bl_exists: '{0} {1} {2} is blacklisted already.'
            var embed = new SerializableDiscordEmbed()
                .WithDescription(
                    context.FormatLocalized(
                        (success) ? "bl_added" : "bl_exist",                // <- Key | Args ↓
                        entry.Type.ToString().ToSnakeCase(),                // User, Channel, Server or Unspecified
                        Formatter.Bold(entryName),                          // Name or Unknown
                        Formatter.InlineCode(entry.ContextId.ToString())    // ID
                    )
                );

            await context.RespondLocalizedAsync(embed);
        }

        [Command("addmany")]
        [Description("cmd_bl_addmany")]
        public async Task MassBlacklistAsync(CommandContext context, [Description("arg_ulong_id_col")] params ulong[] ids)
        {
            var amount = await _service.AddBlacklistsAsync(ids);

            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized("bl_added_range", amount));

            await context.RespondLocalizedAsync(embed);
        }

        [HiddenOverload]
        [Command("remove"), Aliases("rm")]
        [Description("cmd_blacklist_rem")]
        public async Task BlacklistRemoveAsync(CommandContext context, [Description("arg_ulong_id")] ulong id)
        {
            var entry = await _service.RemoveBlacklistAsync(id);

            var entryName = (string.IsNullOrEmpty(entry?.Name))
                ? context.FormatLocalized("unknown")
                : entry.Name;

            // bl_removed: Successfully removed {0} {1} {2} from the blacklist.
            // bl_not_exist: '{0} {1} {2} is not blacklisted.'
            var embed = new SerializableDiscordEmbed()
                .WithDescription(
                    context.FormatLocalized(
                        (entry is not null) ? "bl_removed" : "bl_not_exist",  // <- Key | Args ↓
                        entry?.Type.ToString().ToSnakeCase(),       // User, Channel, Server or Unspecified
                        Formatter.Bold(entryName),                  // Name or Unknown
                        Formatter.InlineCode(id.ToString())         // ID
                    )
                );

            await context.RespondLocalizedAsync(embed);
        }

        [Command("removemany")]
        [Description("cmd_bl_removemany")]
        public async Task MassRemoveAsync(CommandContext context, [Description("arg_ulong_id_col")] params ulong[] ids)
        {
            var amount = await _service.RemoveBlacklistsAsync(ids);

            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized("bl_removed_range", amount));

            await context.RespondLocalizedAsync(embed);
        }

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_blacklist_list")]
        public async Task BlacklistListAsync(CommandContext context, [Description("arg_bltype")] BlacklistType? type = null)
        {
            // Get the blacklist. Returns an empty collection if there is nothing there.
            var blacklist = await _service.GetBlacklistAsync(
                (type is null) ? null : x => x.Type == type.Value,
                y => new BlacklistEntity() { ContextId = y.ContextId, Type = y.Type, Name = y.Name }
            );

            var unknown = context.FormatLocalized("unknown");
            var fields = new List<SerializableEmbedField>();

            // Send response
            var embed = new SerializableDiscordEmbed()
                .WithTitle("bl_title");

            if (blacklist.Count == 0)
                embed.WithDescription("bl_empty");
            else
            {
                foreach (var blGroup in blacklist.Chunk(AkkoConstants.LinesPerPage))
                {
                    fields.Add(new("id", string.Join("\n", blGroup.Select(x => x.ContextId)), true));
                    fields.Add(new("type", string.Join("\n", blGroup.Select(x => x.Type)), true));
                    fields.Add(new("name", string.Join("\n", blGroup.Select(x => x.Name ?? unknown)), true));
                }
            }

            await context.RespondPaginatedByFieldsAsync(embed, fields, 3);
        }

        [Command("clear")]
        [Description("cmd_blacklist_clear")]
        public async Task BlacklistClearAsync(CommandContext context)
        {
            // If blacklist is empty, return error
            if (!_service.HasBlacklists)
            {
                var embed = new SerializableDiscordEmbed()
                    .WithDescription("bl_empty");

                await context.RespondLocalizedAsync(embed, isError: true);
                return;
            }

            // Build the confirmation message
            var question = new SerializableDiscordEmbed()
                .WithDescription(
                    context.FormatLocalized(
                        "q_are_you_sure",               // Key
                        "q_blclear", "q_yes", "q_no"    // Values
                    )
                );

            // Send the interactive message and perform the action if user confirms it
            await context.RespondInteractiveAsync(question, "q_yes", async () =>
            {
                var rows = await _service.ClearBlacklistsAsync();

                var embed = new SerializableDiscordEmbed()
                    .WithDescription(context.FormatLocalized("bl_clear", rows));

                await context.RespondLocalizedAsync(embed);
            });
        }

        [Command("check")]
        [Description("cmd_bl_check")]
        public async Task BlacklistCheckAsync(CommandContext context, [Description("arg_ulong_id")] ulong id)
        {
            var entity = (await _service.GetBlacklistAsync(x => x.ContextId == id))
                .FirstOrDefault();

            if (entity is null)
            {
                var embed = new SerializableDiscordEmbed()
                    .WithDescription("bl_not_found");

                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                var embed = new SerializableDiscordEmbed()
                    .AddField("name", entity.Name?.ToString() ?? context.FormatLocalized("unknown"), true)
                    .AddField("type", entity.Type.ToString(), true)
                    .AddField("id", entity.ContextId.ToString(), true)
                    .AddField("reason", string.IsNullOrWhiteSpace(entity.Reason) ? "-" : entity.Reason, false);

                await context.RespondLocalizedAsync(embed);
            }
        }
    }
}