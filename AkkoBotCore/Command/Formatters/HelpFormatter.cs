using System.Reflection.Metadata;
using System.Reflection;
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

        /// <summary>
        /// Adds a command to the help message.
        /// </summary>
        /// <param name="cmd">Command to provide help for.</param>
        /// <returns>This HelpFormatter.</returns>
        public HelpFormatter WithCommand(DSharpPlus.CommandsNext.Command cmd)
        {
            // Set title
            _helpTitle = GetHelpHeader(cmd);

            // Add description
            _helpDescription = _cmdContext.FormatLocalized(cmd.Description);

            // Add requirements
            var requirements = GetCmdRequirements(cmd);

            foreach (var att in requirements.OrderBy(x => x.AttributeType.Name))
            {
                if (att.AttributeType == typeof(BotOwnerAttribute))
                    _helpRequiresField.AppendLine(_cmdContext.FormatLocalized("help_bot_owner"));
                else
                    _helpRequiresField.AppendLine(GetLocalizedPermissions(att.ConstructorArguments));
            }

            // If this is a group, there are no arguments to be shown
            if (cmd is CommandGroup)
                return this;

            // Get the parameters of valid overloads
            var reflectedParameters = GetOverloads(cmd).ToArray();

            // Format usage
            foreach (var overload in cmd.Overloads.OrderBy(x => x.Arguments.Count))
            {
                if (!IsValidOverload(overload, reflectedParameters))
                    continue;

                // Initialize string builder
                _helpExamplesField ??= new();

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
                    var optional = (argument.IsOptional)
                        ? $"({_cmdContext.FormatLocalized("help_optional")})"
                        : string.Empty;

                    _helpExamplesField.AppendLine(
                        $"{Formatter.InlineCode(argument.Name)}: {optional} " +
                        _cmdContext.FormatLocalized(argument.Description ?? string.Empty)
                    );
                }

                _helpExamplesField.AppendLine();
            }

            return this;
        }

        /// <summary>
        /// Adds subcommands associated with a specified command
        /// </summary>
        /// <param name="subcommands">The collection of subcommands.</param>
        /// <remarks>Only use this on a command that is a <see cref="CommandGroup"/>.</remarks>
        /// <returns>This HelpFormatter.</returns>
        public HelpFormatter WithSubcommands(IEnumerable<DSharpPlus.CommandsNext.Command> subcommands)
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

        /// <summary>
        /// Returns an error message.
        /// </summary>
        /// <returns>This HelpFormatter.</returns>
        public HelpFormatter WithCmdNotFound()
        {
            IsErroed = true;
            _helpDescription = _cmdContext.FormatLocalized(
                "help_cmd_not_found",
                Formatter.InlineCode(_cmdContext.Prefix + "module"),
                Formatter.InlineCode(_cmdContext.Prefix + "module" + " <" + _cmdContext.FormatLocalized("name").ToLowerInvariant() + ">")
            );

            return this;
        }

        /// <summary>
        /// Builds the help message.
        /// </summary>
        /// <remarks>The string will be <see langword="null"/> if the embed is not <see langword="null"/> and vice-versa.</remarks>
        /// <returns>The result help message.</returns>
        public (string, DiscordEmbedBuilder) Build()
        {
            using var scope = _cmdContext.Services.GetScopedService<IUnitOfWork>(out var db);
            var guildSettings = db.GuildConfig.GetGuild(_cmdContext.Guild.Id);

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

        /// <summary>
        /// Gets the title of the help message.
        /// </summary>
        /// <param name="cmd">A command.</param>
        /// <returns>A string with command and aliases separated by a slash.</returns>
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

        /// <summary>
        /// Gets the requirements from a command.
        /// </summary>
        /// <param name="cmd">The command to get the requirements from.</param>
        /// <returns>A list of attributes with the requirements.</returns>
        private IEnumerable<CustomAttributeData> GetCmdRequirements(DSharpPlus.CommandsNext.Command cmd)
        {
            return cmd.Module.ModuleType.GetMethods()
                .Where(
                    method => method.CustomAttributes.Any(
                        attribute => (attribute.ConstructorArguments.FirstOrDefault().Value as string)
                            ?.Equals(cmd.Name, StringComparison.InvariantCultureIgnoreCase) ?? false
                    )
                )
                .SelectMany(method => method.CustomAttributes)
                .Concat(GetAttributeTree(cmd))
                .Where(
                    attribute => attribute.AttributeType == typeof(BotOwnerAttribute)
                        || attribute.AttributeType == typeof(RequireUserPermissionsAttribute)
                        || attribute.AttributeType == typeof(RequirePermissionsAttribute)
                )
                .DistinctBy(attribute => attribute.ConstructorArguments.FirstOrDefault().Value);
        }

        /// <summary>
        /// Gets all attributes from a command and the groups it belongs to.
        /// </summary>
        /// <param name="cmd">The command to get the attributes from.</param>
        /// <returns>A collection of attributes.</returns>
        private IEnumerable<CustomAttributeData> GetAttributeTree(DSharpPlus.CommandsNext.Command cmd)
        {
            return (cmd.Parent is null)
                ? cmd.Module.ModuleType.CustomAttributes
                : cmd.Parent.Module.ModuleType.CustomAttributes.Concat(GetAttributeTree(cmd.Parent));
        }

        /// <summary>
        /// Gets all reflected parameters of a given command.
        /// </summary>
        /// <param name="cmd">Command to extract the parameters from.</param>
        /// <remarks>It filters out overloads marked as <see cref="HiddenAttribute"/> or <see cref="HiddenOverloadAttribute"/>.</remarks>
        /// <returns>A collection of parameters where each array represents a valid command overload.</returns>
        private IEnumerable<ParameterInfo[]> GetOverloads(DSharpPlus.CommandsNext.Command cmd)
        {
            return cmd.Module.ModuleType.GetMethods()
                .Where(
                    method => method.CustomAttributes.Any(
                        attribute => (attribute.ConstructorArguments.FirstOrDefault().Value as string)
                            ?.Equals(cmd.Name, StringComparison.InvariantCultureIgnoreCase) ?? false
                    )
                    && !method.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(HiddenOverloadAttribute))
                )
                .Select(method =>
                {
                    var result = method.GetParameters();

                    return (result.Length == 1 && result.First().ParameterType == typeof(CommandContext))
                        ? Array.Empty<ParameterInfo>()
                        : result.Where(param => param.ParameterType != typeof(CommandContext)).ToArray();
                });
        }

        /// <summary>
        /// Checks whether a certain command overload is present in a collection of reflected parameters.
        /// </summary>
        /// <param name="overload">Command overload to check for.</param>
        /// <param name="methodParameters">The collection of reflected parameters.</param>
        /// <remarks>Each array in the collection represents a method.</remarks>
        /// <returns><see langword="true"/> if the overload is present in the collection, <see langword="false"/> otherwise.</returns>
        private bool IsValidOverload(CommandOverload overload, IEnumerable<ParameterInfo[]> methodParameters)
        {
            foreach (var parameters in methodParameters)
            {
                var matches = 0;

                // If command and reflected method have no parameters, return true
                if (parameters.Length == 0 && overload.Arguments.Count == 0)
                    return true;
                else if (parameters.Length != overload.Arguments.Count)
                    continue;

                foreach (var param in parameters)
                {
                    foreach (var ovArg in overload.Arguments)
                    {
                        if (ovArg.Name.Equals(param.Name)
                            && (param.ParameterType.BaseType == typeof(Array) || ovArg.Type == param.ParameterType) // This is needed for variable arrays
                            && ++matches == parameters.Length)
                            return true; // Overload is valid
                    }
                }
            }

            // Overload didn't match all reflected parameters
            return false;
        }

        /// <summary>
        /// Gets the localized permissions of a permission attribute.
        /// </summary>
        /// <param name="permissions">Collection of attributes to have their permissions taken from.</param>
        /// <returns>A string with all localized attributes separated by a newline.</returns>
        private string GetLocalizedPermissions(IEnumerable<CustomAttributeTypedArgument> permissions)
        {
            var localizedPerms = new List<string>();

            foreach (var perm in permissions)
            {
                Enum.TryParse(typeof(Permissions), perm.Value.ToString(), true, out var r);

                var results = r.ToString().Replace(",", string.Empty).Split(" ");

                foreach (var result in results)
                    localizedPerms.Add(_cmdContext.FormatLocalized("help_" + result.ToSnakeCase()));
            }

            return string.Join("\n", localizedPerms);
        }
    }
}