using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Commands.Modules.Self.Services;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services;
using AkkoCore.SlashCommands.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.SlashCommands;
using System.Threading.Tasks;

namespace AkkoCore.SlashCommands.Modules
{
    [SlashModuleLifespan(SlashModuleLifespan.Singleton)]
    public sealed class SlashAdministration : AkkoSlashCommandModule
    {
        private readonly BotConfigService _botService;
        private readonly GuildConfigService _guildService;

        public SlashAdministration(BotConfigService botService, GuildConfigService guildService)
        {
            _botService = botService;
            _guildService = guildService;
        }

        [SlashCommand("prefix", "Shows the prefix the bot responds to.")]
        public async Task SlashPrefixAsync(InteractionContext context, [RemainingText, Option("newPrefix", "The new prefix the bot should respond to.")] string? prefix = default)
        {
            if (prefix is not null
                && ((AkkoUtilities.IsOwner(context, context.User.Id) && context.Guild is null)
                || (context.Guild is not null && context.Member.PermissionsIn(context.Channel).HasPermission(Permissions.ManageGuild))))
                await SetPrefixAsync(context, prefix);
            else
                await CheckPrefixAsync(context);
        }

        private async Task SetPrefixAsync(InteractionContext context, string prefix)
        {
            var isAllowed = context.Guild is not null || AkkoUtilities.IsOwner(context, context.User.Id);
            var embed = new SerializableDiscordEmbed()
                .WithDescription((isAllowed) ? context.FormatLocalized("guild_prefix_change", Formatter.InlineCode(prefix)) : "bot_owner_error");

            if (isAllowed && context.Guild is null)
                _botService.GetOrSetProperty(x => x.Prefix = prefix);
            else if (isAllowed)
                await _guildService.SetPropertyAsync(context.Guild!, x => x.Prefix = prefix);

            await context.RespondLocalizedAsync(embed, isError: !isAllowed);
        }

        private async Task CheckPrefixAsync(InteractionContext context)
        {
            var response = (context.Guild is null) ? "bot_prefix_check" : "guild_prefix_check";
            var prefix = context.GetMessageSettings().Prefix;

            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized(response, Formatter.InlineCode(prefix)));

            await context.RespondLocalizedAsync(embed);
        }
    }
}