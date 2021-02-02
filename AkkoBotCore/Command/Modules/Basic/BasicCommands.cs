using System;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using AkkoBot.Extensions;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Collections.Generic;

namespace AkkoBot.Command.Modules.Basic
{
    [RequireBotPermissions(Permissions.SendMessages)]
    public class BasicCommands : AkkoCommandModule
    {
        private readonly DateTimeOffset _startup = DateTimeOffset.Now;

        public BasicCommands()
        {
            //
        }

        [Command("ping")]
        [Description("Gets the websocket latency for this bot.")]
        public async Task Ping(CommandContext context)
        {
            var embed = new DiscordEmbedBuilder()
                .WithDescription($"{context.Client.Ping} ms");

            await context.RespondLocalizedAsync(embed);
        }

        [Command("die"), Aliases("shutdown")]
        [Description("Shuts the bot down.")]
        public async Task Die(CommandContext context)
        {
            // There is probably a better way to do this
            var embed = new DiscordEmbedBuilder()
                .WithDescription("shutdown");

            await context.RespondLocalizedAsync(embed);

            // Log to the console
            context.Client.Logger.BeginScope(context);
            context.Client.Logger.LogInformation(
                new EventId(LoggerEvents.ConnectionClose.Id, "Command"),
                context.Message.Content
            );

            // Clean-up
            await context.Client.DisconnectAsync();
            context.Client.Dispose();
            Environment.Exit(Environment.ExitCode);
        }

        [Command("uptime")]
        [Description("Shows the bot's uptime.")]
        public async Task Uptime(CommandContext context)
        {
            var elapsed = DateTimeOffset.Now.Subtract(_startup);

            var embed = new DiscordEmbedBuilder()
                .WithTitle("online_since")
                .WithDescription(Formatter.InlineCode($"[{_startup.LocalDateTime}]"))
                .AddField("Shards", $"#{context.Client.ShardId}/{context.Client.ShardCount}", inline: true)
                .AddField(
                    "uptime",
                    context.FormatLocalized("{0}: {1}\n", "days", elapsed.Days) +
                    context.FormatLocalized("{0}: {1}\n", "hours", elapsed.Hours) +
                    context.FormatLocalized("{0}: {1}\n", "minutes", elapsed.Minutes) +
                    context.FormatLocalized("{0}: {1}\n", "seconds", elapsed.Seconds),
                    inline: true
                );

            await context.RespondLocalizedAsync(embed, false);
        }
    }
}