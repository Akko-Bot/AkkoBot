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
using Microsoft.Extensions.Logging;

namespace AkkoBot.Command.Modules.Basic
{
    [RequireBotPermissions(Permissions.SendMessages)]
    public class BasicCommands : AkkoCommandModule
    {
        private readonly DateTimeOffset _startup;
        private readonly AkkoUnitOfWork _db;
        //private readonly Credentials _creds;

        public BasicCommands(AkkoUnitOfWork db)
        {
            _startup = DateTimeOffset.Now;
            _db = db;
            //_creds = creds;

            /*
            Receive Repository object
            Repository looks up cache
            Repository returns result from request
             */
        }

        [Command("ping")]
        [Description("Ping Discord's servers and return the bot's latency.")]
        public async Task Ping(CommandContext context) =>
            await context.Message.RespondAsync(Formatter.BlockCode($"{context.Client.Ping} ms"));

        [Command("die"), Aliases("shutdown")]
        [Description("Shuts the bot down.")]
        public async Task Die(CommandContext context)
        {
            // There is probably a better way to do this
            await context.Message.RespondAsync("Shutting down.");
            context.Client.Logger.BeginScope(context);
            context.Client.Logger.LogInformation(
                new EventId(LoggerEvents.WebSocketReceive.Id, "Command"),
                context.Message.Content
            );

            await context.Client.DisconnectAsync();
            context.Client.Dispose();
            Environment.Exit(Environment.ExitCode);
        }

        [Command("uptime")]
        [Description("Shows the bot's uptime.")]
        public async Task Uptime(CommandContext context)
        {
            var elapsed = DateTimeOffset.Now.Subtract(_startup);

            await context.Message.RespondAsync(
                $"Uptime {Formatter.InlineCode($"[{_startup.LocalDateTime}]")}\n" +
                Formatter.BlockCode(
                    $"Days: {elapsed.Days}\n" +
                    $"Hours: {elapsed.Hours}\n" +
                    $"Minutes: {elapsed.Minutes}\n" +
                    $"Seconds: {elapsed.Seconds}"
                )
            );
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
                TypeId = context.User.Id,
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
                TypeId = context.User.Id
            });

            await _db.SaveChangesAsync();

            await context.RespondAsync("Done. Removed db entry.");
        }
    }
}