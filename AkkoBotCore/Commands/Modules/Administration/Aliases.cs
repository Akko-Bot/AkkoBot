using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Extensions;
using AkkoBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration
{
    [Group("alias"), Aliases("aliases")]
    [Description("cmd_alias")]
    public class Aliases : AkkoCommandModule
    {
        private readonly AliasService _service;

        public Aliases(AliasService service) 
            => _service = service;

        [Command("add")]
        [Description("cmd_alias_add")]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task AddAlias(CommandContext context, 
            [Description("arg_alias_add_alias")] string alias, 
            [RemainingText, Description("arg_alias_add_command")] string command)
        {
            if (await _service.SaveAliasAsync(context, alias, command))
                await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
            else
                await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
        }

        [Command("remove"), Aliases("rm")]
        [Description("cmd_alias_remove")]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task RemoveAlias(CommandContext context, [RemainingText, Description("arg_alias_remove")] string alias)
        {
            if (await _service.RemoveAliasAsync(context, alias))
                await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
            else
                await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
        }

        [Command("clear")]
        [Description("cmd_alias_clear")]
        [RequirePermissions(Permissions.ManageGuild)]
        public async Task ClearAliases(CommandContext context)
        {
            if (await _service.ClearAliasesAsync(context))
                await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
            else
                await context.Message.CreateReactionAsync(AkkoEntities.FailureEmoji);
        }

        [GroupCommand, Command("list"), Aliases("show")]
        [Description("cmd_alias_list")]
        public async Task ListAliases(CommandContext context)
        {
            var dbAliases = _service.GetAliases(context.Guild?.Id);
            var isEmpty = dbAliases.Count is 0;
            var embed = new DiscordEmbedBuilder();

            if (isEmpty)
                embed.WithDescription("alias_error");
            else
            {
                embed.WithTitle((context.Guild is null) ? "alias_list_title_global" : "alias_list_title_server")
                    .AddField("alias", string.Join('\n', dbAliases.Select(x => x.Alias.Replace("{p}", context.Prefix)).ToArray()), true)
                    .AddField("command", string.Join('\n', dbAliases.Select(x => (context.Prefix + x.FullCommand).MaxLength(50, "[...]")).ToArray()), true);
            }

            await context.RespondLocalizedAsync(embed, isEmpty, isEmpty);
        }
    }
}
