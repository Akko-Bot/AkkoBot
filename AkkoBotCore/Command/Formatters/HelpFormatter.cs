using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AkkoBot.Command.Attributes;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace AkkoBot.Command.Formatters
{
    public class HelpFormatter
    {
        private string _helpTitle;
        private string _helpDescription;
        private readonly StringBuilder _helpRequiresField = new();
        private StringBuilder _helpExamplesField;
        private StringBuilder _helpCommandsField;
        private readonly CommandContext _cmdContext;

        public bool IsErroed { get; private set; }

        public HelpFormatter(CommandContext context)
            => _cmdContext = context;

        // This is called first, except if command is !help with no parameters
        public HelpFormatter WithCommand(DSharpPlus.CommandsNext.Command cmd)
        {
            // Set title
            _helpTitle = GetHelpHeader(cmd);

            // Add description
            _helpDescription = _cmdContext.FormatLocalized(cmd.Description);

            // Add requirements
            foreach (var att in cmd.Module.ModuleType.CustomAttributes)
            {
                if (att.AttributeType == typeof(BotOwnerAttribute))
                {
                    _helpRequiresField.AppendLine(_cmdContext.FormatLocalized("help_bot_owner"));
                }
                else if (att.AttributeType == typeof(RequireBotPermissionsAttribute))
                {
                    Enum.TryParse(typeof(Permissions), att.ConstructorArguments.FirstOrDefault().Value.ToString(), true, out var result);
                    _helpRequiresField.AppendLine(_cmdContext.FormatLocalized("help_" + result.ToString().ToSnakeCase()));
                }
            }

            // If this is a group, there are no arguments to be shown
            if (cmd is CommandGroup)
                return this;

            // Initialize string builder
            _helpExamplesField = new();

            // Format usage
            foreach (var overload in cmd.Overloads)
            {
                // If command takes no argument
                if (overload.Arguments.Count == 0)
                {
                    _helpExamplesField.AppendLine(Formatter.InlineCode(_cmdContext.Prefix + cmd.QualifiedName) + "\n");
                    continue;
                }

                // Format full command name + <arguments>
                _helpExamplesField.Append($"`{_cmdContext.Prefix}{cmd.QualifiedName}");

                foreach (var argument in overload.Arguments)
                {
                    _helpExamplesField.Append($" <{argument.Name}>");
                }

                _helpExamplesField.AppendLine("`");

                // Format argument descriptions
                foreach (var argument in overload.Arguments)
                {
                    _helpExamplesField.AppendLine(
                        $"{Formatter.InlineCode(argument.Name)}: " +
                        _cmdContext.FormatLocalized(argument.Description ?? string.Empty)
                    );
                }

                _helpExamplesField.AppendLine();
            }

            return this;
        }

        // This is called second, it sets the current group's subcommands. If no group is being
        // processed or current command is not a group, it won't be called
        public HelpFormatter WithCommands(IEnumerable<DSharpPlus.CommandsNext.Command> subcommands)
        {
            // var isHelpCmd = _cmdContext.CommandsNext.RegisteredCommands.Values.ContainsSubcollection(subcommands);
            _helpCommandsField = new();

            // if (isHelpCmd)
            // {
            //     // Get all parent command groups
            //     var rootCmdGroups = _cmdContext.CommandsNext.RegisteredCommands.Values
            //         .Where(cmd => cmd is CommandGroup)
            //         ;//.DistinctBy(cmd => cmd.QualifiedName);

            //     // Add command groups
            //     foreach (var cmdGroup in rootCmdGroups)
            //         _helpCommandsField.Append(Formatter.InlineCode(cmdGroup.Name) + ", ");

            //     // Add regular commands
            //     foreach (var command in subcommands.Where(cmd => cmd is not CommandGroup))
            //         _helpCommandsField.Append(Formatter.InlineCode(command.Name) + ", ");
            // }
            // else
            // {
            //     // Add regular commands
            //     foreach (var command in subcommands)
            //         _helpCommandsField.Append(Formatter.InlineCode(command.Name) + ", ");
            // }

            foreach (var command in subcommands)
                    _helpCommandsField.Append(Formatter.InlineCode(command.Name) + ", ");

            _helpCommandsField.Remove(_helpCommandsField.Length - 2, 2);

            return this;
        }

        // This is called last.
        // It should produce the final message and return it
        public (string, DiscordEmbedBuilder) Build()
        {
            using var scope = _cmdContext.Services.GetScopedService<IUnitOfWork>(out var db);
            var guildSettings = db.GuildConfigs.GetGuild(_cmdContext.Guild.Id);

            if (guildSettings.UseEmbed)
            {
                var msg = new DiscordEmbedBuilder()
                    .WithTitle(_helpTitle)
                    .WithDescription(_helpDescription);

                if (_helpRequiresField.Length != 0)
                    msg.AddField(_cmdContext.FormatLocalized("requires"), _helpRequiresField.ToString());

                if (_helpCommandsField is not null)
                {
                    msg.AddField(_cmdContext.FormatLocalized("commands"), _helpCommandsField.ToString())
                        .WithFooter(
                            _cmdContext.FormatLocalized(
                            "help_footer",
                            _cmdContext.Prefix + _cmdContext.Command.QualifiedName + " " + _cmdContext.RawArgumentString +
                           " <" + _cmdContext.FormatLocalized("name").ToLowerInvariant() + ">"
                            )
                            .Replace("  ", " ")
                        );
                }
                    
                if (_helpExamplesField is not null)
                    msg.AddField(_cmdContext.FormatLocalized("usage"), _helpExamplesField.ToString());
                
                return (null, msg);
            }
            else
            {
                return (
                    ((_helpTitle is not null) ? Formatter.Bold(_helpTitle) + "\n" : string.Empty) +
                    ((_helpDescription is not null) ? _helpDescription + "\n\n" : string.Empty) +
                    ((_helpRequiresField.Length != 0) ? Formatter.Bold(_cmdContext.FormatLocalized("requires")) + "\n" + _helpRequiresField.ToString() + "\n" : string.Empty) +
                    ((_helpCommandsField is not null) ? Formatter.Bold(_cmdContext.FormatLocalized("commands")) + "\n" + _helpCommandsField.ToString() + "\n" : string.Empty) +
                    ((_helpExamplesField is not null) ? Formatter.Bold(_cmdContext.FormatLocalized("usage")) + "\n" + _helpExamplesField.ToString() + "\n" : string.Empty),
                    null
                );
            }
        }

        public HelpFormatter WithCmdNotFound()
        {
            IsErroed = true;
            _helpDescription = _cmdContext.FormatLocalized("help_cmd_not_found");
            return this;
        }

        private string GetHelpHeader(DSharpPlus.CommandsNext.Command cmd)
        {
            return string.Join(
                " / ",
                cmd.Aliases
                    .Select(alias => Formatter.InlineCode(alias))
                    .Prepend(Formatter.InlineCode(cmd.Name))
                    .ToArray()
            );
        }
    }
}