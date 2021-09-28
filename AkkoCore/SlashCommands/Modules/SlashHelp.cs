using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Modules.Help.Services;
using AkkoCore.Extensions;
using AkkoCore.SlashCommands.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace AkkoCore.SlashCommands.Modules
{
    public class SlashHelp : AkkoSlashCommandModule
    {
        private readonly HelpService _service;

        public SlashHelp(HelpService service)
            => _service = service;

        [SlashCommand("help", "Gets help on how to use a command.")]
        public async Task SlashHelpCommandAsync(InteractionContext context, [RemainingText, Option("command", "The command to get help for.")] string command = "")
        {
            var cmdHandler = context.Client.GetCommandsNext();
            var cmd = cmdHandler.FindCommand(command, out var args)
                ?? cmdHandler.FindCommand("help", out args);

            using var scope = context.Services.GetRequiredScopedService<IHelpFormatter>(out var helpBuilder);
            var settings = context.GetMessageSettings();

            var fakeContext = cmdHandler.CreateFakeContext(context.User, context.Channel, command, settings.Prefix, cmd, args);
            var message = helpBuilder.GenerateHelpMessage(fakeContext, string.IsNullOrWhiteSpace(command) ? Array.Empty<string>() : command.Split(' '));

            await context.RespondLocalizedAsync(message);
        }

        [SlashCommand("modules", "Lists all command modules.")]
        public async Task SlashModulesAsync(InteractionContext context)
        {
            var embed = _service.GetAllModules(context.GetMessageSettings(), context.Client.GetCommandsNext(), context.CommandName);
            await context.RespondLocalizedAsync(embed);
        }

        [SlashCommand("module", "Lists all commands under the specified module.")]
        public async Task SlashModulesAsync(InteractionContext context, [Option("command", "The module to list the commands from.")] string module)
        {
            var embed = await _service.GetAllModuleCommandsAsync(
                context.GetMessageSettings(),
                context.Client.GetCommandsNext(),
                context.User,
                context.Channel,
                module
            );

            await context.RespondLocalizedAsync(embed);
        }

        [SlashCommand("search", "Searches for a command with the specified keyword.")]
        public async Task SlashSearchCommandsAsync(InteractionContext context, [RemainingText, Option("keyword", "The term to search for.")] string keyword)
        {
            var searchResult = _service.SearchCommandByKeyword(context.GetMessageSettings(), context.Client.GetCommandsNext(), keyword);
            await context.RespondPaginatedByFieldsAsync(searchResult, 2);
        }
    }
}