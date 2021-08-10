using AkkoBot.Commands.Attributes;
using AkkoBot.Extensions;
using AkkoBot.Services.Localization.Abstractions;
using AkkoEntities.Extensions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace AkkoBot.Models.Serializable
{
    /// <summary>
    /// Represents a serializable model of a <see cref="Command"/> object.
    /// </summary>
    public record SerializableCommand
    {
        /// <summary>
        /// The qualified name of the command.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The description of the command.
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// The aliases of the command, if there are any.
        /// </summary>
        public IReadOnlyList<string> Aliases { get; }

        /// <summary>
        /// The (user) permissions required for the command to run.
        /// </summary>
        public IReadOnlyList<string> Permissions { get; }

        /// <summary>
        /// The subcommands associated with this command.
        /// </summary>
        /// <remarks>Will always be empty if the command is not a <see cref="CommandGroup"/>.</remarks>
        public IReadOnlyList<SerializableCommand> Subcommands { get; }

        /// <summary>
        /// The arguments of all overloads of this command.
        /// </summary>
        /// <remarks>Overloads that contain arguments with no description will be ignored.</remarks>
        public IReadOnlyList<IReadOnlyDictionary<string, string>> Arguments { get; }

        public SerializableCommand(ILocalizer localizer, Command command, string locale)
        {
            Name = command.QualifiedName;
            Description = localizer.GetResponseString(locale, command.Description);
            Aliases = command.Aliases.ToList();
            Permissions = GetRequirements(localizer, command, locale);
            Arguments = GetArguments(localizer, command, locale);
            Subcommands = (command is CommandGroup group)
                ? GetSubcommands(localizer, group, locale)
                : new List<SerializableCommand>(0);
        }

        /// <summary>
        /// Gets the serializable subcommands of the specified command group.
        /// </summary>
        /// <param name="localizer">The string localizer.</param>
        /// <param name="command">The command to get the subcommands from.</param>
        /// <param name="locale">The locale to get the strings for.</param>
        /// <returns>The list of serializable commands.</returns>
        private IReadOnlyList<SerializableCommand> GetSubcommands(ILocalizer localizer, CommandGroup command, string locale)
            => command.Children.Select(x => new SerializableCommand(localizer, x, locale)).ToList();

        /// <summary>
        /// Gets the required permissions of the command.
        /// </summary>
        /// <param name="localizer">The string localizer.</param>
        /// <param name="command">The command to get the permissions from.</param>
        /// <param name="locale">The locale to get the strings for.</param>
        /// <returns>The list of command requirements.</returns>
        private IReadOnlyList<string> GetRequirements(ILocalizer localizer, Command command, string locale)
        {
            var result = new List<string>();
            var requirements = command.GetRequirements();

            foreach (var att in requirements.OrderBy(x => x.AttributeType.Name))
            {
                if (att.AttributeType == typeof(BotOwnerAttribute))
                    result.Add(localizer.GetResponseString(locale, "perm_bot_owner"));
                else if (att.AttributeType == typeof(RequireDirectMessageAttribute))
                    result.Add(localizer.GetResponseString(locale, "perm_require_dm"));
                else
                {
                    foreach (var arg in att.ConstructorArguments.SelectMany(x => ((Permissions)x.Value).ToStrings().Skip(1)))
                        result.Add(localizer.GetResponseString(locale, "perm_" + arg.ToSnakeCase()));
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the arguments of all overloads of a given command.
        /// </summary>
        /// <param name="localizer">The string localizer.</param>
        /// <param name="command">The command to get the arguments from.</param>
        /// <param name="locale">The locale to get the strings for.</param>
        /// <remarks>This method assumes that the description of valid arguments are not null nor empty.</remarks>
        /// <returns>A collection with the arguments of every valid overload.</returns>
        private IReadOnlyList<IReadOnlyDictionary<string, string>> GetArguments(ILocalizer localizer, Command command, string locale)
        {
            var overloads = new List<Dictionary<string, string>>();

            foreach (var overload in command.Overloads.Where(x => x.Arguments.Any(y => !string.IsNullOrWhiteSpace(y.Description))).OrderBy(x => x.Arguments.Count))
            {
                var newOverloadArgs = new Dictionary<string, string>();
                overloads.Add(newOverloadArgs);

                foreach (var argument in overload.Arguments)
                {
                    var optional = (argument.IsOptional)
                        ? $"({localizer.GetResponseString(locale, "help_optional")}) "
                        : string.Empty;

                    newOverloadArgs.TryAdd(argument.Name, optional + localizer.GetResponseString(locale, argument.Description));
                }
            }

            return overloads;
        }
    }
}