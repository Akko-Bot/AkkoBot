using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Utilities
{
    [RequireGuild]
    [RequireBotPermissions(Permissions.SendMessages | Permissions.AddReactions)]
    public class ServerUtilities : AkkoCommandModule
    {
        private readonly UtilitiesService _service;

        public ServerUtilities(UtilitiesService service) 
            => _service = service;

        [Command("say"), HiddenOverload]
        [Priority(0)]
        public async Task Say(CommandContext context, [RemainingText] string message)
            => await Say(context, context.Channel, message);

        [Command("say")]
        [Description("cmd_say")]
        [RequirePermissions(Permissions.ManageMessages)]
        [Priority(1)]
        public async Task Say(CommandContext context, [Description("arg_discord_channel")] DiscordChannel channel, [RemainingText, Description("arg_say")] string message)
        {
            if (!context.Guild.CurrentMember.PermissionsIn(channel).HasFlag(Permissions.SendMessages))
                await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
            else if (string.IsNullOrWhiteSpace(message))
                await context.RespondAsync(channel.Mention);
            else if (_service.DeserializeEmbed(message, out var parsedMessage))
                await channel.SendMessageAsync(parsedMessage);
            else
                await channel.SendMessageAsync(message);
        }
    }
}
