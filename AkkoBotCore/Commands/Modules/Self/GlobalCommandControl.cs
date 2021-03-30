using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Commands.Modules.Self.Services;
using AkkoBot.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Self
{
    [BotOwner]
    public class GlobalCommandControl : AkkoCommandModule
    {
        private readonly CommandControlService _service;

        public GlobalCommandControl(CommandControlService service)
            => _service = service;

        [Command("globalcommand"), Aliases("gcmd")]
        [Description("cmd_globalcmd")]
        public async Task GlobalCommandToggle(CommandContext context, [RemainingText, Description("arg_command")] string command)
        {
            if (command.StartsWith(context.Prefix))
                command = command[context.Prefix.Length..];

            var cmd = context.CommandsNext.FindCommand(command, out _);

            var success = (cmd is not null)
                ? await _service.DisableGlobalCommandAsync(cmd)
                : await _service.EnableGlobalCommandAsync(command);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(
                    context.FormatLocalized(
                        (cmd is not null || success) ? "gcmd_toggle" : "gcmd_error",
                        Formatter.Bold(command),
                        (cmd is not null && success) ? "disabled" : "enabled"
                    )
                );

            await context.RespondLocalizedAsync(embed, isError: !success);
        }

        [Command("globalmodule"), Aliases("gmod")]
        [Description("cmd_globalmodule")]
        public async Task GlobalModuleToggle(CommandContext context, [Description("arg_module")] string module)
        {
            var cmds = context.CommandsNext.RegisteredCommands.Values
                .Where(x => x.Module.ModuleType.FullName.Contains(module, StringComparison.InvariantCultureIgnoreCase))
                .ToArray();

            var success = (cmds.Length is not 0)
                ? await _service.DisableGlobalCommandsAsync(cmds)
                : await _service.EnableGlobalCommandsAsync(module);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(
                    context.FormatLocalized(
                        (cmds.Length is not 0 || success) ? "gmod_toggle" : "gmod_error",
                        Formatter.Bold(module),
                        (cmds.Length is not 0 && success) ? "disabled" : "enabled"
                    )
                );

            await context.RespondLocalizedAsync(embed, isError: !success);
        }

        [Command("listdisabledcommands"), Aliases("listdisabledcmds", "ldc")]
        [Description("cmd_ldc")]
        public async Task ListDisabledCommands(CommandContext context)
        {
            var cmds = _service.GetDisabledCommands()
                .Select(x => Formatter.InlineCode(x.Key))
                .ToArray();

            var embed = new DiscordEmbedBuilder()
                .WithTitle("ldc_title")
                .WithDescription((cmds.Length is 0) ? "ldc_empty" : string.Join(", ", cmds));

            await context.RespondLocalizedAsync(embed, false, cmds.Length is 0);
        }
    }
}
