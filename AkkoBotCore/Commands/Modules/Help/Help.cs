using AkkoBot.Commands.Attributes;
using AkkoBot.Commands.Formatters;
using AkkoBot.Common;
using AkkoBot.Extensions;
using AkkoBot.Models.Serializable;
using AkkoBot.Models.Serializable.EmbedParts;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Help
{
    [HelpCommand, IsNotBlacklisted, GlobalCooldown]
    public class Help : BaseCommandModule
    {
        [HiddenOverload]
        [Command("help"), Aliases("h")]
        [Description("cmd_help")]
        public async Task HelpCommandAsync(CommandContext context)
            => await HelpCommandAsync(context, Array.Empty<string>());

        [Command("help")]
        public async Task HelpCommandAsync(CommandContext context, [Description("arg_command")] params string[] command)
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

            var message = helpBuilder.Build();
            var botPerms = context.Guild?.CurrentMember.PermissionsIn(context.Channel) ?? Permissions.SendMessages;

            // Try to send: channel message, channel reaction, direct message
            if (botPerms.HasPermission(Permissions.SendMessages))
                await context.RespondLocalizedAsync(message, helpBuilder.IsErroed, helpBuilder.IsErroed);
            else if (botPerms.HasPermission(Permissions.AddReactions))
                await context.Message.CreateReactionAsync(AkkoEntities.WarningEmoji);
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

            var embed = new SerializableDiscordMessage()
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
        public async Task ModulesAsync(CommandContext context, [Description("arg_module")] string moduleName)
        {
            var cmdGroup = await context.CommandsNext.RegisteredCommands.Values
                .Where(cmd => !cmd.IsHidden && cmd.Module.ModuleType.FullName.Contains(moduleName, StringComparison.InvariantCultureIgnoreCase))
                .Distinct()
                .OrderBy(x => x.Name)
                .Select(async cmd =>
                {
                    var emote = ((await cmd.RunChecksAsync(context, false)).Any())
                        ? AkkoEntities.FailureEmoji.Name
                        : AkkoEntities.SuccessEmoji.Name;

                    return emote + context.Prefix + cmd.QualifiedName;
                })
                .ToListAsync();

            var embed = new SerializableDiscordMessage();

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

        [Command("search")]
        [Description("cmd_search")]
        [RequireBotPermissions(Permissions.SendMessages | Permissions.AddReactions)]
        public async Task SearchAsync(CommandContext context, [RemainingText, Description("arg_keyword")] string keyword)
        {
            if (keyword.StartsWith(context.Prefix))
                keyword = keyword[context.Prefix.Length..];

            var embed = new SerializableDiscordMessage();
            var cmds = context.CommandsNext.RegisteredCommands.Values
                .Concat(
                    context.CommandsNext.RegisteredCommands.Values
                        .Where(x => x is CommandGroup)
                        .SelectMany(x => (x as CommandGroup).Children)
                )
                .Where(
                    x => x.QualifiedName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                        || x.Aliases.Any(x => x.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                )
                .DistinctBy(x => x.QualifiedName)
                .OrderBy(x => x.QualifiedName);

            if (!cmds.Any())
            {
                embed.WithDescription(context.FormatLocalized("search_result_empty", Formatter.InlineCode(keyword)));
                await context.RespondLocalizedAsync(embed, isError: true);

                return;
            }

            var fields = new List<SerializableEmbedField>();
            embed.WithTitle(context.FormatLocalized("search_result_description", Formatter.InlineCode(keyword)));

            foreach (var cmd in cmds.SplitInto(AkkoConstants.LinesPerPage))
            {
                fields.Add(new("command", string.Join("\n", cmd.Select(x => context.Prefix + x.QualifiedName)), true));
                fields.Add(new("description", string.Join("\n", cmd.Select(x => context.FormatLocalized(x.Description).MaxLength(50, "[...]"))), true));
            }

            await context.RespondPaginatedByFieldsAsync(embed, fields, 2);
        }
    }
}