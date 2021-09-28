using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Help.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Help
{
    [HelpCommand, IsNotBlacklisted, GlobalCooldown]
    public class Help : BaseCommandModule
    {
        private readonly HelpService _service;

        public Help(HelpService service)
            => _service = service;

        [Command("help"), HiddenOverload]
        public async Task HelpCommandAsync(CommandContext context)
            => await HelpCommandAsync(context, Array.Empty<string>());

        [Command("help"), Aliases("h")]
        [Description("cmd_help")]
        public async Task HelpCommandAsync(CommandContext context, [Description("arg_command")] params string[] command)
        {
            using var scope = context.Services.GetRequiredScopedService<IHelpFormatter>(out var helpBuilder);
            var message = helpBuilder.GenerateHelpMessage(context, command);
            var botPerms = context.Guild?.CurrentMember.PermissionsIn(context.Channel) ?? Permissions.SendMessages;

            // Try to send: channel message, channel reaction, direct message
            if (botPerms.HasPermission(Permissions.SendMessages))
                await context.RespondLocalizedAsync(message, helpBuilder.IsErroed, helpBuilder.IsErroed);
            else if (botPerms.HasPermission(Permissions.AddReactions))
                await context.Message.CreateReactionAsync(AkkoStatics.WarningEmoji);
            else
            {
                message.Content = "⚠️ " + Formatter.Bold(context.FormatLocalized("help_cant_dm", context.Guild.Name)) + "\n\n" + message.Content;

                // Might consider placing a global ratelimit on !help because of this
                await context.SendLocalizedDmAsync(context.Member, message, true);
            }
        }

        [Command("module"), Aliases("modules", "cmds")]
        [Description("cmd_modules")]
        [RequireBotPermissions(Permissions.SendMessages)]
        public async Task ModulesAsync(CommandContext context)
        {
            var embed = _service.GetAllModules(context.GetMessageSettings(), context.CommandsNext, context.Command.QualifiedName);
            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("module")]
        public async Task ModulesAsync(CommandContext context, [Description("arg_module")] string moduleName)
        {
            var embed = await _service.GetAllModuleCommandsAsync(context.GetMessageSettings(), context.CommandsNext, context.User, context.Channel, moduleName);
            await context.RespondLocalizedAsync(embed, embed.Color is not null);
        }

        [Command("search")]
        [Description("cmd_search")]
        [RequireBotPermissions(Permissions.SendMessages | Permissions.AddReactions)]
        public async Task SearchAsync(CommandContext context, [RemainingText, Description("arg_keyword")] string keyword)
        {
            var embed = _service.SearchCommandByKeyword(context.GetMessageSettings(), context.CommandsNext, keyword);
            await context.RespondPaginatedByFieldsAsync(embed, 2);
        }
    }
}