using AkkoBot.Command.Abstractions;
using AkkoBot.Command.Attributes;
using AkkoBot.Command.Formatters;
using AkkoBot.Extensions;
using AkkoBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Command.Modules.Help
{
    public class Help : AkkoCommandModule
    {
        [HiddenOverload]
        [Command("help"), Aliases("h")]
        [Description("cmd_help")]
        public async Task HelpCommand(CommandContext context)
            => await HelpCommand(context, Array.Empty<string>());


        [Command("help")]
        public async Task HelpCommand(CommandContext context, [Description("arg_command")] params string[] command)
        {
            // Might consider removing this line if I ever decide to change the default help command
            var baseCmds = context.CommandsNext.RegisteredCommands.Values.Where(cmd => !cmd.IsHidden).Distinct();
            var helpBuilder = new HelpFormatter(context);

            // If no parameter, send the default help message
            if (command.Length == 0)
                helpBuilder.WithSubcommands(baseCmds);
            else
            {
                // Remove prefix from the command, if user typed it in
                command[0] = command[0].Replace(context.Prefix, string.Empty);

                var cmd = context.CommandsNext.FindCommand(string.Join(" ", command), out _);

                if (cmd is null)
                    helpBuilder.WithCmdNotFound();
                else if (cmd is CommandGroup group)
                    helpBuilder.WithCommand(cmd).WithSubcommands(group.Children);
                else
                    helpBuilder.WithCommand(cmd);
            }

            var (content, embed) = helpBuilder.Build();
            var botPerms = context.Guild.CurrentMember.PermissionsIn(context.Channel);

            // Try to send: channel message, channel reaction, direct message
            if (botPerms.HasPermission(Permissions.SendMessages))
                await context.RespondLocalizedAsync(content, embed, helpBuilder.IsErroed, helpBuilder.IsErroed);
            else if (botPerms.HasPermission(Permissions.AddReactions))
                await context.Message.CreateReactionAsync(AkkoEntities.WarningEmoji);
            else
            {
                // Might consider placing a global ratelimit on !help because of this
                await context.SendLocalizedDmAsync(
                    context.Member,
                    "⚠️" + Formatter.Bold(context.FormatLocalized("help_cant_dm")) + "\n\n" + content,
                    embed,
                    true
                );
            }
        }

        [Command("module"), Aliases("modules", "cmds")]
        [Description("cmd_modules")]
        [RequireBotPermissions(Permissions.SendMessages)]
        public async Task Modules(CommandContext context)
        {
            var namespaces = context.CommandsNext.RegisteredCommands.Values
                .Where(cmd => !cmd.IsHidden && !cmd.Module.ModuleType.FullName.Contains("DSharpPlus"))   // Remove library modules
                .Select(cmd =>                                                          // Section the namespaces
                {
                    var nspaces = cmd.Module.ModuleType.FullName.Split('.');
                    return nspaces[^Math.Min(2, nspaces.Length - 1)];
                })
                .Distinct()                                                             // Remove the repeated sections
                .OrderBy(x => x)
                .ToArray();

            var embed = new DiscordEmbedBuilder()
                .WithTitle("modules_title")
                .WithDescription(string.Join("\n", namespaces))
                .WithFooter(
                    context.FormatLocalized(
                        "modules_footer",
                        context.Prefix + context.Command.QualifiedName +
                        " <" + context.FormatLocalized("name").ToLowerInvariant() + ">"
                    )
                );

            await context.RespondLocalizedAsync(embed, false);
        }

        [Command("module")]
        public async Task Modules(CommandContext context, [Description("arg_module")] string moduleName)
        {
            var cmdGroup = await context.CommandsNext.RegisteredCommands.Values
                .Where(cmd => !cmd.IsHidden && cmd.Module.ModuleType.FullName.Contains(moduleName, StringComparison.InvariantCultureIgnoreCase))
                .Distinct()
                .OrderBy(x => x.Name)
                .Select(async cmd =>
                {
                    var emote = (await cmd.RunChecksAsync(context, false)).Any() ? "❌" : "✅";
                    return emote + context.Prefix + cmd.QualifiedName;
                })
                .ToListAsync();

            var embed = new DiscordEmbedBuilder();

            if (cmdGroup.Count == 0)
            {
                embed.WithDescription(context.FormatLocalized("module_not_exist", Formatter.InlineCode(context.Prefix + "modules")));
                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                embed.WithTitle(moduleName.Capitalize())
                    .WithDescription(Formatter.BlockCode(string.Join("\t", cmdGroup)))
                    .WithFooter(
                        context.FormatLocalized(
                            "command_modules_footer",
                            context.Prefix + "help" +
                            " <" + context.FormatLocalized("command").ToLowerInvariant() + ">"
                        )
                    );
                
                await context.RespondLocalizedAsync(embed, false);
            }
        }
    }
}