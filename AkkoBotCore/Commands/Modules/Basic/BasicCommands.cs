using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Basic
{
    public class BasicCommands : AkkoCommandModule
    {
        private readonly DateTimeOffset _startup = DateTimeOffset.Now;

        [Command("ping")]
        [Description("Gets the websocket latency for this bot.")]
        public async Task PingAsync(CommandContext context)
        {
            var embed = new DiscordEmbedBuilder()
                .WithDescription($"{context.Client.Ping} ms");

            await context.RespondLocalizedAsync(embed);
        }

        [BotOwner]
        [Command("die"), Aliases("shutdown")]
        [Description("Shuts the bot down.")]
        public async Task DieAsync(CommandContext context)
        {
            var embed = new DiscordEmbedBuilder()
                .WithDescription("shutdown");

            await context.RespondLocalizedAsync(embed);

            // Clean-up
            foreach (var client in context.Services.GetService<DiscordShardedClient>().ShardClients.Values)
                await client.DisconnectAsync();

            Program.RestartBot = false;
            Program.ShutdownToken.Cancel();
        }

        [BotOwner]
        [Command("restart")]
        [Description("Restarts the bot.")]
        public async Task RestartAsync(CommandContext context)
        {
            var embed = new DiscordEmbedBuilder()
                .WithDescription("restart");

            await context.RespondLocalizedAsync(embed);

            // Clean-up
            foreach (var client in context.Services.GetService<DiscordShardedClient>().ShardClients.Values)
                await client.DisconnectAsync();

            Program.ShutdownToken.Cancel();
        }

        [Command("uptime")]
        [Description("Shows the bot's uptime.")]
        public async Task UptimeAsync(CommandContext context)
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
                    context.FormatLocalized("{0}: {1}", "seconds", elapsed.Seconds),
                    inline: true
                );

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("test")]
        public async Task TestAsync(CommandContext context)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            await context.Channel.SendMessageAsync("Time-consuming command executed.");
        }
    }
}