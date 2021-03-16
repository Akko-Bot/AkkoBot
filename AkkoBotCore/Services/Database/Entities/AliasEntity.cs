using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.CommandsNext;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoBot.Services.Database.Entities
{
    [Comment("Stores command aliases.")]
    public class AliasEntity : DbEntity
    {
        public ulong? GuildId { get; init; }

        /// <summary>
        /// Defines whether this alias accepts additional parameters.
        /// </summary>
        public bool IsDynamic { get; set; }

        /// <summary>
        /// The command alias.
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Alias { get; init; }

        /// <summary>
        /// The actual command this alias is mapped to.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Command { get; set; }

        /// <summary>
        /// The arguments of the command alias, if any.
        /// </summary>
        [MaxLength(2000)]
        public string Arguments { get; set; }

        /// <summary>
        /// Gets the full command string mapped to this alias.
        /// </summary>
        [NotMapped]
        public string FullCommand => (string.IsNullOrWhiteSpace(Arguments))? Command : $"{Command} {Arguments}";

        /// <summary>
        /// Gets the command associated with this alias.
        /// </summary>
        /// <param name="cmdHandler">The command handler.</param>
        /// <param name="arguments">Arguments of the command. <see cref="string.Empty"/> if there are none, <see langword="null"/> if the command is not found.</param>
        /// <returns>The command mapped to this alias.</returns>
        public Command GetCommand(CommandsNextExtension cmdHandler, out string arguments)
            => cmdHandler.FindCommand(Command + " " + Arguments, out arguments);

        /// <summary>
        /// Parses a command alias and its arguments into an actual command string.
        /// </summary>
        /// <param name="prefix">The context prefix.</param>
        /// <param name="aliasInput">The raw command alias with arguments, if any.</param>
        /// <returns>The command string with the alias' arguments.</returns>
        public string ParseAliasInput(string prefix, string aliasInput)
        {
            var args = aliasInput.Replace(Alias.Replace("{p}", prefix), string.Empty).Trim();
            return (string.IsNullOrWhiteSpace(args)) ? FullCommand : $"{Command} {args}";
        }
    }
}
