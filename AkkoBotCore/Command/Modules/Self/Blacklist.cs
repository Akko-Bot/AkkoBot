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
    [Description("Groups commands related to the bot's blacklist.")]
    public sealed class Blacklist : AkkoCommandModule
    {
        private readonly BlacklistService _service;

        public Blacklist(BlacklistService service)
            => _service = service;

        [Command("add")]
        public async Task BlacklistAdd(CommandContext context, DiscordChannel channel)
            => await BlacklistAdd(context, BlacklistType.Channel, channel.Id);

        [Command("add")]
        public async Task BlacklistAdd(CommandContext context, DiscordUser user)
            => await BlacklistAdd(context, BlacklistType.User, user.Id);

        [Command("remove")]
        public async Task BlacklistRemove(CommandContext context, DiscordChannel channel)
            => await BlacklistRemove(context, channel.Id);

        [Command("remove")]
        public async Task BlacklistRemove(CommandContext context, DiscordUser user)
            => await BlacklistRemove(context, user.Id);

        [Command("add")]
        [Description("Adds an entry to the blacklist.")]
        public async Task BlacklistAdd(CommandContext context, BlacklistType type, ulong id)
        {
            var (entry, success) = await _service.TryAddAsync(context, type, id);

            var entryName = (string.IsNullOrEmpty(entry.Name))
                ? await context.FormatLocalizedAsync("unknown")
                : entry.Name;

            // bl_added: Successfully added {0} {1} {2} to the blacklist
            // bl_exists: '{0} {1} {2} is blacklisted already.'
            var embed = new DiscordEmbedBuilder()
                .WithDescription(
                    await context.FormatLocalizedAsync(
                        (success) ? "bl_added" : "bl_exist",    // <- Key | Args ↓ 
                        entry.Type.ToString().ToSnakeCase(),    // User, Channel, Server or Unspecified
                        Formatter.Bold(entryName),              // Name or Unknown
                        Formatter.InlineCode(entry.ContextId.ToString())    // ID
                    )
                );

            await context.RespondLocalizedAsync(embed);
        }

        [Command("remove"), Aliases("rem")]
        [Description("Remove from blacklist.")]
        public async Task BlacklistRemove(CommandContext context, ulong id)
        {
            var (entry, success) = await _service.TryRemoveAsync(context, id);

            var entryName = (string.IsNullOrEmpty(entry?.Name))
                ? await context.FormatLocalizedAsync("unknown")
                : entry.Name;

            // bl_removed: Successfully removed {0} {1} {2} from the blacklist.
            // bl_not_exist: '{0} {1} {2} is not blacklisted.'
            var embed = new DiscordEmbedBuilder()
                .WithDescription(
                    await context.FormatLocalizedAsync(
                        (success) ? "bl_removed" : "bl_not_exist",  // <- Key | Args ↓ 
                        entry?.Type.ToString().ToSnakeCase(),        // User, Channel, Server or Unspecified
                        Formatter.Bold(entryName),                  // Name or Unknown
                        Formatter.InlineCode(id.ToString())    // ID
                    )
                );

            await context.RespondLocalizedAsync(embed);
        }

        [Command("list"), Aliases("show")]
        [Description("Lists the blacklist.")]
        public async Task BlacklistList(CommandContext context, BlacklistType? type = null)
        {
            // Convert user input to the appropriate enum
            //var blType = _service.GetBlacklistType(type);

            // Get the blacklist. Returns an empty collection if there is nothing there.
            var blacklist = (type is null)
                ? await _service.GetAllAsync(context)
                : await _service.GetAsync(context, b => b.Type == type.Value);

            // Prepare localized response
            StringBuilder responseIds = new(), responseTypes = new(), responseNames = new();

            foreach (var entity in blacklist)
            {
                responseIds.AppendLine(entity.ContextId.ToString());
                responseTypes.AppendLine(await context.FormatLocalizedAsync(entity.Type.ToString().ToSnakeCase()));
                responseNames.AppendLine((entity.Name is null) ? await context.FormatLocalizedAsync("unknown") : entity.Name);
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

        [Command("clear")] // TODO: make this interactive
        [Description("Clears the blacklist.")]
        public async Task BlacklistClear(CommandContext context)
        {
            var rows = await _service.ClearAsync(context);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(await context.FormatLocalizedAsync("bl_clear", rows));

            await context.RespondLocalizedAsync(embed);
        }
    }
}