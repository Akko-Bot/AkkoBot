using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Config.Models;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Events.Abstractions;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AkkoCore.Commands.Formatters
{
    /// <summary>
    /// Generates help messages for a specific command.
    /// </summary>
    public class HelpFormatter : IHelpFormatter
    {
        private readonly SerializableDiscordEmbed _helpMessage = new();
        private readonly IDbCache _dbCache;
        private readonly ILocalizer _localizer;
        private readonly ICommandHandler _commandHandler;
        private readonly BotConfig _botConfig;

        public bool IsErroed { get; private set; }

        public HelpFormatter(IDbCache dbCache, ILocalizer localizer, ICommandHandler commandHandler, BotConfig botConfig)
        {
            _dbCache = dbCache;
            _localizer = localizer;
            _commandHandler = commandHandler;
            _botConfig = botConfig;
        }

        public SerializableDiscordMessage GenerateHelpMessage(CommandContext context)
            => GenerateHelpMessage(context, context.RawArguments as List<string>);

        public SerializableDiscordMessage GenerateHelpMessage(CommandContext context, IList<string> inputCommand)
        {
            inputCommand ??= context.RawArguments as List<string>;

            // If no parameter, send the default help message
            if (inputCommand.Count == 0)
            {
                // Default help message (no command)
                this.WithSubcommands(
                    context,
                    context.CommandsNext.RegisteredCommands.Values
                        .Where(cmd => !cmd.IsHidden)
                        .Distinct()
                );
            }
            else
            {
                // Remove prefix from the command, if user typed it in
                inputCommand[0] = inputCommand[0].Replace(context.Prefix, string.Empty);

                var cmd = context.CommandsNext.FindCommand(string.Join(" ", inputCommand), out _);

                if (cmd is null)
                    this.WithCmdNotFound(context);
                else if (cmd is CommandGroup group)
                    this.WithCommand(context, cmd).WithSubcommands(context, group.Children);
                else
                    this.WithCommand(context, cmd);
            }

            return this.Build(context);
        }

        /// <summary>
        /// Adds a command to the help message.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="cmd">Command to provide help for.</param>
        /// <returns>This HelpFormatter.</returns>
        private HelpFormatter WithCommand(CommandContext context, Command cmd)
        {
            // Set title and description
            _helpMessage
                .WithTitle(GetHelpHeader(cmd))
                .WithDescription(cmd.Description);

            // Add requirements
            _commandHandler.GetActiveOverride(context.Guild?.Id, cmd, out var permOverride);
            var stringBuilder = new StringBuilder();
            var requirements = cmd.GetRequirements();

            foreach (var att in requirements.OrderBy(x => x.AttributeType.Name))
            {
                if (att.AttributeType == typeof(BotOwnerAttribute))
                    stringBuilder.AppendLine(context.FormatLocalized("perm_bot_owner"));
                else if (att.AttributeType == typeof(RequireDirectMessageAttribute))
                    stringBuilder.AppendLine(context.FormatLocalized("perm_require_dm"));
                else if (permOverride is null || !permOverride.IsActive)
                    stringBuilder.AppendLine(GetLocalizedAttributePermissions(context, att.ConstructorArguments));
            }

            if (permOverride is not null && permOverride.IsActive)
                stringBuilder = GetLocalizedPermissionOverrides(context, stringBuilder, permOverride);

            if (stringBuilder.Length is not 0)
            {
                _helpMessage.AddField("requires", stringBuilder.ToString());
                stringBuilder.Clear();
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

                // If command takes no argument
                if (overload.Arguments.Count == 0)
                {
                    stringBuilder.AppendLine(Formatter.InlineCode(context.Prefix + cmd.QualifiedName) + "\n");
                    continue;
                }

                // Format full command name + <arguments>
                stringBuilder.Append('`' + context.Prefix + cmd.QualifiedName);

                foreach (var argument in overload.Arguments)
                    stringBuilder.Append($" <{argument.Name}>");

                stringBuilder.AppendLine("`");

                // Format argument descriptions
                foreach (var argument in overload.Arguments)
                {
                    var optional = (argument.IsOptional)
                        ? $"({context.FormatLocalized("help_optional")})"
                        : string.Empty;

                    stringBuilder.AppendLine(
                        $"{Formatter.InlineCode(argument.Name)}: {optional} " +
                        context.FormatLocalized(argument.Description ?? string.Empty)
                    );
                }

                stringBuilder.AppendLine();
            }

            _helpMessage.AddField("usage", stringBuilder.ToString());
            stringBuilder.Clear();

            return this;
        }

        /// <summary>
        /// Adds subcommands associated with a specified command
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="subcommands">The collection of subcommands.</param>
        /// <remarks>Only use this on a command that is a <see cref="CommandGroup"/>.</remarks>
        /// <returns>This HelpFormatter.</returns>
        private HelpFormatter WithSubcommands(CommandContext context, IEnumerable<Command> subcommands)
        {
            _helpMessage
                .AddField(
                    "commands",
                    string.Join(
                        ", ",
                        subcommands.OrderBy(command => command.Name)
                            .Select(command =>
                                (command.CustomAttributes.Any(x => x.GetType() == typeof(GroupCommandAttribute)))
                                    ? Formatter.Underline(Formatter.InlineCode(command.Name))
                                    : Formatter.InlineCode(command.Name)
                            )
                    )
                )
                .WithFooter(
                    context.FormatLocalized(
                        "help_footer",
                        context.Prefix + context.Command.QualifiedName + " " + context.RawArgumentString +
                        " <" + context.FormatLocalized("name").ToLowerInvariant() + ">"
                    )
                    .Replace("  ", " ")
                );

            return this;
        }

        /// <summary>
        /// Returns an error message.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <returns>This HelpFormatter.</returns>
        private HelpFormatter WithCmdNotFound(CommandContext context)
        {
            IsErroed = true;
            _helpMessage.WithDescription(
                context.FormatLocalized(
                    "help_cmd_not_found",
                    Formatter.InlineCode(context.Prefix + "module"),
                    Formatter.InlineCode(context.Prefix + "module" + " <" + context.FormatLocalized("name").ToLowerInvariant() + ">")
                )
            );

            return this;
        }

        /// <summary>
        /// Builds the help message.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <remarks>The string will be <see langword="null"/> if the embed is not <see langword="null"/> and vice-versa.</remarks>
        /// <returns>The resulting help message.</returns>
        private SerializableDiscordMessage Build(CommandContext context)
        {
            var useEmbed = (_dbCache.Guilds.TryGetValue(context.Guild?.Id ?? 0, out var dbGuild))
                ? dbGuild.UseEmbed
                : _botConfig.UseEmbed;

            if (useEmbed)
                return new(_helpMessage);

            _helpMessage.WithLocalization(_localizer, context.GetLocaleKey());

            return new SerializableDiscordMessage(
                ((_helpMessage.Body?.Title?.Text is not null) ? Formatter.Bold(_helpMessage.Body.Title.Text) + "\n" : string.Empty) +
                ((_helpMessage.Body?.Description is not null) ? _helpMessage.Body.Description + "\n\n" : string.Empty) +
                ((_helpMessage.Fields?.Count is not 0 and not null) ? string.Join("\n", _helpMessage.Fields.Select(x => $"{Formatter.Bold(x.Title)}\n{x.Text}")) + "\n\n" : string.Empty) +
                ((_helpMessage.Footer?.Text is not null) ? _helpMessage.Footer.Text : string.Empty)
            );
        }

        /// <summary>
        /// Gets the localized permissions of a permission attribute.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="permissions">Collection of attributes to have their permissions taken from.</param>
        /// <returns>A string with all localized attributes separated by a newline.</returns>
        /// <exception cref="InvalidCastException">Occurs when the attribute argument is not of type <see cref="Permissions"/>.</exception>
        private string GetLocalizedAttributePermissions(CommandContext context, IEnumerable<CustomAttributeTypedArgument> permissions)
            => string.Join("\n", permissions.Where(x => x.ArgumentType == typeof(Permissions)).SelectMany(x => ((Permissions)x.Value).ToLocalizedStrings(context)));

        /// <summary>
        /// Adds the localized overriden permissions to the suplied <paramref name="stringBuilder"/>.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="stringBuilder">The supplied string builder.</param>
        /// <param name="permOverride">The permission override.</param>
        /// <returns>The supplied <paramref name="stringBuilder"/> with all overriden permissions.</returns>
        private StringBuilder GetLocalizedPermissionOverrides(CommandContext context, StringBuilder stringBuilder, PermissionOverrideEntity permOverride)
        {
            if (permOverride.AllowedUserIds.Count is not 0)
            {
                stringBuilder.AppendLine(
                    context.FormatLocalized(
                        "**{0}**: {1}",
                        "users",
                        string.Join(", ", _dbCache.Users.Values.Where(x => permOverride.AllowedUserIds.Contains((long)x.UserId)).Select(x => x.FullName))
                    )
                );
            }

            if (permOverride.AllowedRoleIds.Count is not 0)
            {
                stringBuilder.AppendLine(
                    context.FormatLocalized(
                        "**{0}**: {1}",
                        "roles",
                        string.Join(", ", context.Guild.Roles.Values.Where(x => permOverride.AllowedRoleIds.Contains((long)x.Id)).Select(x => x.Name))
                    )
                );
            }

            if (permOverride.AllowedChannelIds.Count is not 0)
            {
                stringBuilder.AppendLine(
                    context.FormatLocalized(
                        "**{0}**: {1}",
                        "channels",
                        string.Join(", ", context.Guild.Channels.Values.Where(x => permOverride.AllowedChannelIds.Contains((long)x.Id)).Select(x => x.Mention))
                    )
                );
            }

            if (permOverride.Permissions is not Permissions.None)
                stringBuilder.AppendLine(string.Join('\n', permOverride.Permissions.ToLocalizedStrings(context)));

            return stringBuilder;
        }

        /// <summary>
        /// Gets the title of the help message.
        /// </summary>
        /// <param name="cmd">A command.</param>
        /// <returns>A string with command and aliases separated by a slash.</returns>
        private string GetHelpHeader(Command cmd)
        {
            return string.Join(
                " / ",
                cmd.Aliases
                    .Select(alias => Formatter.InlineCode(alias))
                    .Prepend(Formatter.InlineCode(cmd.Name))
            );
        }

        /// <summary>
        /// Gets all reflected parameters of a given command.
        /// </summary>
        /// <param name="cmd">Command to extract the parameters from.</param>
        /// <remarks>It filters out overloads marked as <see cref="HiddenAttribute"/> or <see cref="HiddenOverloadAttribute"/>.</remarks>
        /// <returns>A collection of parameters where each array represents a valid command overload.</returns>
        private IEnumerable<ParameterInfo[]> GetOverloads(Command cmd)
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
                        if (ovArg.Name.Equals(param.Name, StringComparison.Ordinal)
                            && (param.ParameterType.BaseType == typeof(Array) || ovArg.Type == param.ParameterType) // This is needed for arrays of variable length
                            && ++matches == parameters.Length)
                            return true; // Overload is valid
                    }
                }
            }

            // Overload didn't match all reflected parameters
            return false;
        }

        public void Dispose()
        {
            _helpMessage.Clear();
            GC.SuppressFinalize(this);
        }
    }
}