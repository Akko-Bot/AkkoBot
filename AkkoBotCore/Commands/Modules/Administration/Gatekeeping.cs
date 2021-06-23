using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Common;
using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration
{
    [RequireGuild]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public class Gatekeeping : AkkoCommandModule
    {
        private readonly GatekeepingService _service;
        private readonly UtilitiesService _utilitiesService;

        public Gatekeeping(GatekeepingService service, UtilitiesService utilitiesService)
        {
            _service = service;
            _utilitiesService = utilitiesService;
        }

        [Command("sanitizenames"), Aliases("sanitizenicks")]
        [Description("cmd_sanitizenames")]
        [RequireUserPermissions(Permissions.ManageNicknames)]
        public async Task SanitizeNicknamesAsync(CommandContext context)
        {
            var result = await _service.SetPropertyAsync(context.Guild, x => x.SanitizeNames = !x.SanitizeNames);

            var embed = new DiscordEmbedBuilder()
            {
                Description = context.FormatLocalized("guild_sanitizenames", (result) ? "enabled" : "disabled")
            };

            await context.RespondLocalizedAsync(embed);
        }

        [Command("sanitizedname"), Aliases("sanitizednick")]
        [Description("cmd_sanitizedname")]
        [RequireUserPermissions(Permissions.ManageNicknames)]
        public async Task SetCustomSanitizedNicknameAsync(CommandContext context, [RemainingText, Description("arg_nickname")] string nickname = "")
        {
            var result = await _service.SetPropertyAsync(context.Guild, x => x.CustomSanitizedName = nickname.SanitizeUsername());

            var embed = new DiscordEmbedBuilder()
            {
                Description = (string.IsNullOrWhiteSpace(result))
                    ? "sanitizedname_reset"
                    : context.FormatLocalized("sanitizedname_set", Formatter.InlineCode(result))
            };

            await context.RespondLocalizedAsync(embed);
        }

        [Command("greetdm")]
        [Description("cmd_greetdm")]
        public async Task ToggleDmGreetingAsync(CommandContext context)
        {
            var result = await _service.SetPropertyAsync(context.Guild, x => x.GreetDm = !x.GreetDm);

            var embed = new DiscordEmbedBuilder()
                .WithDescription((result) ? "greetdm_enabled" : "greetdm_disabled");

            await context.RespondLocalizedAsync(embed);
        }

        [Command("greetchannel")]
        [Description("cmd_greetchannel")]
        public async Task SetGreetChannelAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
        {
            var isValid = channel?.PermissionsFor(context.Guild.CurrentMember).HasPermission(Permissions.AccessChannels | Permissions.SendMessages) is true;
            var embed = new DiscordEmbedBuilder()
            {
                Description = (channel is null)
                    ? "greet_channel_removed"
                    : context.FormatLocalized((isValid) ? "greet_channel_set" : "channel_invalid", channel.Mention)
            };

            if (isValid)
                await _service.SetPropertyAsync(context.Guild, x => x.GreetChannelId = channel?.Id);

            await context.RespondLocalizedAsync(embed, isError: !isValid);
        }

        [Command("farewellchannel")]
        [Description("cmd_farewellchannel")]
        public async Task SetFarewellChannelAsync(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel = null)
        {
            var isValid = channel?.PermissionsFor(context.Guild.CurrentMember).HasPermission(Permissions.AccessChannels | Permissions.SendMessages) is true;
            var embed = new DiscordEmbedBuilder()
            {
                Description = (channel is null)
                    ? "farewell_channel_removed"
                    : context.FormatLocalized((isValid) ? "farewell_channel_set" : "channel_invalid", channel.Mention)
            };

            if (isValid)
                await _service.SetPropertyAsync(context.Guild, x => x.FarewellChannelId = channel?.Id);

            await context.RespondLocalizedAsync(embed);
        }

        [Command("greetmessage"), Aliases("greetmsg", "greet")]
        [Description("cmd_greetmessage")]
        public async Task SetGreetChannelMessageAsync(CommandContext context, [RemainingText, Description("arg_say")] string message = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await SendSettingAsync(context, "greet_message", x => x.GreetMessage);
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithDescription("greet_message_set");

            await _service.SetPropertyAsync(context.Guild, x => x.GreetMessage = message);
            await context.RespondLocalizedAsync(embed);
        }

        [Command("farewellmessage"), Aliases("farewellmsg", "farewell", "bye")]
        [Description("cmd_farewellmessage")]
        public async Task SetFarewellChannelMessageAsync(CommandContext context, [RemainingText, Description("arg_say")] string message = null)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                await SendSettingAsync(context, "farewell_message", x => x.FarewellMessage);
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithDescription("farewell_message_set");

            await _service.SetPropertyAsync(context.Guild, x => x.FarewellMessage = message);
            await context.RespondLocalizedAsync(embed);
        }

        [Command("greetdelete"), Aliases("greetdel")]
        [Description("cmd_greetdel")]
        public async Task SetGreetDeleteTimeAsync(CommandContext context, [Description("arg_greetdel_time")] TimeSpan time = default)
        {
            if (time > TimeSpan.FromMinutes(2))
                time = TimeSpan.FromMinutes(2);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized((time == TimeSpan.Zero) ? "greetdel_disable" : "greetdel_enable", time.TotalSeconds));

            await _service.SetPropertyAsync(context.Guild, x => x.GreetDeleteTime = time);
            await context.RespondLocalizedAsync(embed);
        }

        [Command("farewelldelete"), Aliases("farewelldel", "byedel")]
        [Description("cmd_farewelldel")]
        public async Task SetFarewellDeleteTimeAsync(CommandContext context, [Description("arg_greetdel_time")] TimeSpan time = default)
        {
            if (time > TimeSpan.FromMinutes(2))
                time = TimeSpan.FromMinutes(2);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized((time == TimeSpan.Zero) ? "farewelldel_disable" : "farewelldel_enable", time.TotalSeconds));

            await _service.SetPropertyAsync(context.Guild, x => x.FarewellDeleteTime = time);
            await context.RespondLocalizedAsync(embed);
        }

        /// <summary>
        /// Sends a guild setting to the context Discord channel.
        /// </summary>
        /// <typeparam name="T">The type of the targeted property.</typeparam>
        /// <param name="context">The command context.</param>
        /// <param name="propName">Name/Type of the entity property.</param>
        /// <param name="selector">Method to select the desired property.</param>
        private async Task SendSettingAsync<T>(CommandContext context, string propName, Func<GatekeepEntity, T> selector)
        {
            var property = selector(_service.GetGatekeepSettings(context.Guild) ?? new())?.ToString();
            var parsedMessage = new SmartString(context, property);

            if (string.IsNullOrWhiteSpace(parsedMessage))
                await context.RespondLocalizedAsync(new DiscordEmbedBuilder() { Description = context.FormatLocalized("guild_prop_null", propName) }, isError: true);
            else if (_utilitiesService.DeserializeEmbed(parsedMessage, out var message))
                await context.Channel.SendMessageAsync(message);
            else
                await context.Channel.SendMessageAsync(parsedMessage);
        }
    }
}