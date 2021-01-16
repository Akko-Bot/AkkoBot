using System;
using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Database.Repository;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.Entities;
using AkkoBot.Extensions;

namespace AkkoBot.Command.Modules.Basic
{
    [RequireBotPermissions(Permissions.SendMessages)]
    public class BasicCommands : AkkoCommandModule
    {
        private readonly IUnitOfWork _db;
        private readonly ILocalizer _localizer;
        private readonly DateTimeOffset _startup = DateTimeOffset.Now;

        public BasicCommands(IUnitOfWork db, ILocalizer localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        [Command("ping")]
        [Description("Ping Discord's servers and return the bot's latency.")]
        public async Task Ping(CommandContext context)
        {
            var embed = new DiscordEmbedBuilder()
                .WithDescription($"{context.Client.Ping} ms");

            await context.ReplyLocalizedAsync(embed);
        }

        [Command("die"), Aliases("shutdown")]
        [Description("Shuts the bot down.")]
        public async Task Die(CommandContext context)
        {
            var embed = new DiscordEmbedBuilder()
                .WithDescription("shutdown");
            // There is probably a better way to do this
            await context.ReplyLocalizedAsync(embed);

            /*
            context.Client.Logger.BeginScope(context);
            context.Client.Logger.LogInformation(
                new EventId(LoggerEvents.WebSocketReceive.Id, "Command"),
                context.Message.Content
            );
            */

            await context.Client.DisconnectAsync();
            context.Client.Dispose();
            Environment.Exit(Environment.ExitCode);
        }

        [Command("uptime")]
        [Description("Shows the bot's uptime.")]
        public async Task Uptime(CommandContext context)
        {
            var elapsed = DateTimeOffset.Now.Subtract(_startup);

            /* Test */
            /*
            await ReplyLocalizedAsync(
                context,
                "{0}" + $" {Formatter.InlineCode($"[{_startup.LocalDateTime}]")}\n" +
                Formatter.BlockCode(
                    "{1}" + $": {elapsed.Days}\n" +
                    "{2}" + $": {elapsed.Hours}\n" +
                    "{3}" + $": {elapsed.Minutes}\n" +
                    "{4}" + $": {elapsed.Seconds}"
                ),
                "uptime", "days", "hours", "minutes", "seconds"
            );
            */

            var embed = new DiscordEmbedBuilder()
                .WithTitle("uptime")
                .WithDescription(Formatter.InlineCode($"[{_startup.LocalDateTime}]"))
                .AddField("days", elapsed.Days.ToString(), true)
                .AddField("hours", elapsed.Hours.ToString(), true)
                .AddField("minutes", elapsed.Minutes.ToString(), true)
                .AddField("seconds", elapsed.Seconds.ToString(), true);

            await context.ReplyLocalizedAsync(embed, false);
        }

        [Command("dbread")]
        [Description("Reads an entry of myself in the db.")]
        public async Task DbRead(CommandContext context)
        {
            var result = await _db.DiscordUsers.GetAsync(x => x.UserId == context.User.Id);

            if (result is null)
                await context.RespondAsync("Nothing");
            else
                await context.RespondAsync(result.FirstOrDefault()?.Username);
        }

        [Command("dbwrite")]
        [Description("Writes an entry of myself in the db.")]
        public async Task DbWrite(CommandContext context)
        {
            await _db.DiscordUsers.CreateOrUpdateAsync(context.User);
            await _db.SaveChangesAsync();

            await context.RespondAsync("Done. Inserted db entry.");
        }

        [Command("addbl")]
        [Description("Add blacklist.")]
        public async Task DbBl(CommandContext context)
        {
            var me = new BlacklistEntity()
            {
                Type = BlacklistType.User,
                ContextId = context.User.Id,
                Name = context.User.Username
            };

            await _db.Blacklist.TryCreateAsync(me);
            await _db.SaveChangesAsync();

            await context.RespondAsync("Done. Inserted db entry.");
        }

        [Command("rembl")]
        [Description("Add blacklist.")]
        public async Task RemBl(CommandContext context)
        {
            _db.Blacklist.Delete(new BlacklistEntity()
            {
                Type = BlacklistType.User,
                ContextId = context.User.Id
            });

            await _db.SaveChangesAsync();

            await context.RespondAsync("Done. Removed db entry.");
        }
    }
}