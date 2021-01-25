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
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace AkkoBot.Command.Formatters
{
    public class HelpFormatter : BaseHelpFormatter
    {
        private string _helpTitle;
        private string _helpDescription;
        private readonly StringBuilder _helpRequiresField = new();
        private StringBuilder _helpExamplesField;
        private StringBuilder _helpSubcommandsField;
        private readonly CommandContext _cmdContext;

        public HelpFormatter(CommandContext context) : base(context)
        {
            _cmdContext = context;
        }

        // This is called first
        public override BaseHelpFormatter WithCommand(DSharpPlus.CommandsNext.Command cmd)
        {
            using var scope = _cmdContext.Services.CreateScope();
            var prefix = scope.ServiceProvider
                .GetService<IUnitOfWork>().GuildConfigs
                .GetSync(_cmdContext.Guild.Id).Prefix;

            // Set title
            _helpTitle = GetHelpHeader(cmd);

            // Add description
            _helpDescription = _cmdContext.FormatLocalized(cmd.Description);

            // Add requirements
            foreach (var att in cmd.Module.ModuleType.CustomAttributes)
            {
                if (att.AttributeType == typeof(BotOwner))
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

            _helpExamplesField = new();

            // Format usage
            foreach (var overload in cmd.Overloads)
            {
                // If command takes no argument
                if (overload.Arguments.Count == 0)
                {
                    _helpExamplesField.Append(Formatter.InlineCode(prefix + cmd.QualifiedName));
                    continue;
                }

                // Format full command name + <arguments>
                _helpExamplesField.Append($"`{prefix}{cmd.QualifiedName}");

                foreach (var argument in overload.Arguments)
                {
                    _helpExamplesField.Append($" <{argument.Name}>");
                }

                _helpExamplesField.AppendLine("`");

                // Format argument descriptions
                foreach (var argument in overload.Arguments)
                {
                    _helpExamplesField.AppendLine(
                        $"{Formatter.InlineCode($"{argument.Name}")}: " +
                        _cmdContext.FormatLocalized(argument.Description ?? string.Empty)
                    );
                }

                _helpExamplesField.AppendLine();
            }

            return this;
        }

        // This is called second, it sets the current group's subcommands. If no group is being
        // processed or current command is not a group, it won't be called
        public override BaseHelpFormatter WithSubcommands(IEnumerable<DSharpPlus.CommandsNext.Command> subcommands)
        {
            _helpSubcommandsField = new();

            foreach (var command in subcommands)
            {
                _helpSubcommandsField.Append(Formatter.InlineCode(command.Name) + ", ");
            }

            _helpSubcommandsField.Remove(_helpSubcommandsField.Length - 2, 2);

            return this;
        }

        // This is called last.
        // It should produce the final message and return it
        public override CommandHelpMessage Build()
        {
            using var scope = _cmdContext.Services.CreateScope();
            var guildSettings = scope.ServiceProvider
                .GetService<IUnitOfWork>().GuildConfigs
                .GetSync(_cmdContext.Guild.Id);

            if (guildSettings.UseEmbed)
            {
                var msg = new DiscordEmbedBuilder()
                    .WithTitle(_helpTitle)
                    .WithDescription(_helpDescription)
                    .WithColor(new DiscordColor(guildSettings.OkColor));

                if (_helpRequiresField.Length != 0)
                    msg.AddField(_cmdContext.FormatLocalized("requires"), _helpRequiresField.ToString());

                if (_helpSubcommandsField is not null)
                    msg.AddField(_cmdContext.FormatLocalized("subcommands"), _helpSubcommandsField.ToString());

                if (_helpExamplesField is not null)
                    msg.AddField(_cmdContext.FormatLocalized("usage"), _helpExamplesField.ToString());

                return new CommandHelpMessage(null, msg);
            }
            else
            {
                return new CommandHelpMessage(
                    ((_helpTitle is not null) ? Formatter.Bold(_helpTitle) + "\n" : string.Empty) +
                    ((_helpDescription is not null) ? _helpDescription + "\n\n" : string.Empty) +
                    ((_helpRequiresField.Length != 0) ? Formatter.Bold(_cmdContext.FormatLocalized("requires")) + "\n" + _helpRequiresField.ToString() + "\n" : string.Empty) +
                    ((_helpSubcommandsField is not null) ? Formatter.Bold(_cmdContext.FormatLocalized("subcommands")) + "\n" + _helpSubcommandsField.ToString() + "\n" : string.Empty) +
                    ((_helpExamplesField is not null) ? Formatter.Bold(_cmdContext.FormatLocalized("usage")) + "\n" + _helpExamplesField.ToString() + "\n" : string.Empty)
                );
            }
        }

        private string GetHelpHeader(DSharpPlus.CommandsNext.Command cmd)
        {
            return string.Join(
                " / ",
                cmd.Aliases
                    .Select(x => Formatter.InlineCode(x))
                    .Prepend(Formatter.InlineCode(cmd.Name))
                    .ToArray()
            );
        }
    }
}