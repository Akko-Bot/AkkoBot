using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Help.Services;
using AkkoCore.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Help;

[HelpCommand]
public sealed class Help : AkkoCommandModule
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

        await context.RespondLocalizedAsync(message, helpBuilder.IsErroed, helpBuilder.IsErroed);
    }

    [Command("module"), Aliases("modules", "cmds")]
    [Description("cmd_modules")]
    [RequireBotPermissions(Permissions.SendMessages | Permissions.SendMessagesInThreads)]
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
    [RequireBotPermissions(Permissions.SendMessages | Permissions.SendMessagesInThreads | Permissions.AddReactions)]
    public async Task SearchAsync(CommandContext context, [RemainingText, Description("arg_keyword")] string keyword)
    {
        var embed = _service.SearchCommandByKeyword(context.GetMessageSettings(), context.CommandsNext, keyword);
        await context.RespondPaginatedByFieldsAsync(embed, 2);
    }
}