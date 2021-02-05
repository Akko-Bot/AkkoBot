using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using AkkoBot.Command.Attributes;
using AkkoBot.Command.Modules.Self.Services;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace AkkoBot.Command.Modules.Self
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
        public async Task BlacklistAdd(CommandContext context, DiscordChannel channel, [RemainingText] string reason = null)
            => await BlacklistAdd(context, BlacklistType.Channel, channel.Id, reason);

        [Command("add")]
        public async Task BlacklistAdd(CommandContext context, [Description("arg_mention")] DiscordUser entity, [RemainingText, Description("arg_punishment_reason")] string reason = null)
            => await BlacklistAdd(context, BlacklistType.User, entity.Id, reason);

        [Command("remove"), HiddenOverload]
        public async Task BlacklistRemove(CommandContext context, DiscordChannel channel)
            => await BlacklistRemove(context, channel.Id);

        [Command("remove")]
        public async Task BlacklistRemove(CommandContext context, [Description("arg_mention")] DiscordUser entity)
            => await BlacklistRemove(context, entity.Id);

        [Command("add")]
        [Description("cmd_blacklist_add")]
        public async Task BlacklistAdd(
            CommandContext context,
            [Description("arg_bltype")] BlacklistType type,
            [Description("arg_ulong_id")] ulong id,
            [RemainingText] string reason = null)
        {
            var (entry, success) = await _service.TryAddAsync(context, type, id, reason);

            var entryName = (string.IsNullOrEmpty(entry.Name))
                ? context.FormatLocalized("unknown")
                : entry.Name;

            // bl_added: Successfully added {0} {1} {2} to the blacklist
            // bl_exists: '{0} {1} {2} is blacklisted already.'
            var embed = new DiscordEmbedBuilder()
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

        [Command("add")]
        public async Task MassBlacklist(CommandContext context, [Description("arg_ulong_id_col")] params ulong[] ids)
        {
            var amount = _service.AddRange(context, ids);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("bl_added_range", amount));

            await context.RespondLocalizedAsync(embed);
        }

        [HiddenOverload]
        [Command("remove"), Aliases("rem")]
        [Description("cmd_blacklist_rem")]
        public async Task BlacklistRemove(CommandContext context, [Description("arg_ulong_id")] ulong id)
        {
            var (entry, success) = await _service.TryRemoveAsync(context, id);

            var entryName = (string.IsNullOrEmpty(entry?.Name))
                ? context.FormatLocalized("unknown")
                : entry.Name;

            // bl_removed: Successfully removed {0} {1} {2} from the blacklist.
            // bl_not_exist: '{0} {1} {2} is not blacklisted.'
            var embed = new DiscordEmbedBuilder()
                .WithDescription(
                    context.FormatLocalized(
                        (success) ? "bl_removed" : "bl_not_exist",  // <- Key | Args ↓ 
                        entry?.Type.ToString().ToSnakeCase(),        // User, Channel, Server or Unspecified
                        Formatter.Bold(entryName),                  // Name or Unknown
                        Formatter.InlineCode(id.ToString())    // ID
                    )
                );

            await context.RespondLocalizedAsync(embed);
        }

        [Command("remove")]
        public async Task MassRemove(CommandContext context, [Description("arg_ulong_id_col")] params ulong[] ids)
        {
            var amount = _service.RemoveRange(context, ids);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("bl_removed_range", amount));

            await context.RespondLocalizedAsync(embed);
        }

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_blacklist_list")]
        public async Task BlacklistList(CommandContext context, [Description("arg_bltype")] BlacklistType? type = null)
        {
            // Get the blacklist. Returns an empty collection if there is nothing there.
            var blacklist = (type is null)
                ? await _service.GetAllAsync(context)
                : await _service.GetAsync(context, b => b.Type == type.Value);

            // Prepare localized response
            StringBuilder responseIds = new(), responseTypes = new(), responseNames = new();

            foreach (var entity in blacklist)
            {
                responseIds.AppendLine(entity.ContextId.ToString());
                responseTypes.AppendLine(context.FormatLocalized(entity.Type.ToString().ToSnakeCase()));
                responseNames.AppendLine((string.IsNullOrEmpty(entity.Name)) ? context.FormatLocalized("unknown") : entity.Name);
            }

            // Send response
            var embed = new DiscordEmbedBuilder()
                .WithTitle("bl_title");

            if (responseIds.Length == 0)
                embed.WithDescription("bl_empty");
            else
            {
                embed.AddField("id", responseIds.ToString(), true)
                .AddField("type", responseTypes.ToString(), true)
                .AddField("name", responseNames.ToString(), true);
            }

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("clear")]
        [Description("cmd_blacklist_clear")]
        public async Task BlacklistClear(CommandContext context)
        {
            // If blacklist is empty, return error
            if (!(await _service.GetAllAsync(context)).Any())
            {
                var embed = new DiscordEmbedBuilder()
                    .WithDescription("bl_empty");

                await context.RespondLocalizedAsync(embed, isError: true);
                return;
            }

            // Build the confirmation message
            var question = new DiscordEmbedBuilder()
                .WithDescription(
                    context.FormatLocalized(
                        "q_are_you_sure",               // Key
                        "q_blclear", "q_yes", "q_no"    // Values
                    )
                );

            // Send the interactive message and perform the action if user confirms it
            await context.RespondInteractiveAsync(question, "q_yes", async () =>
            {
                var rows = await _service.ClearAsync(context);

                var embed = new DiscordEmbedBuilder()
                    .WithDescription(context.FormatLocalized("bl_clear", rows));

                await context.RespondLocalizedAsync(embed);
            });
        }

        [Command("check")]
        [Description("cmd_bl_check")]
        public async Task BlacklistCheck(CommandContext context, [Description("arg_ulong_id")] ulong id)
        {
            var entity = (await _service.GetAsync(context, x => x.ContextId == id)).FirstOrDefault();

            if (entity is null)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithDescription("bl_not_found");

                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                var embed = new DiscordEmbedBuilder()
                    .AddField("name", entity.Name.ToString(), true)
                    .AddField("type", entity.Type.ToString(), true)
                    .AddField("id", entity.ContextId.ToString(), true)
                    .AddField("reason", string.IsNullOrWhiteSpace(entity.Reason) ? "-" : entity.Reason, false);

                await context.RespondLocalizedAsync(embed);
            }
        }
    }
}