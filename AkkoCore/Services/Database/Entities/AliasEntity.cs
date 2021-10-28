using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Services.Database.Abstractions;
using DSharpPlus.CommandsNext;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AkkoCore.Services.Database.Entities
{
    /// <summary>
    /// Stores command aliases.
    /// </summary>
    [Comment("Stores command aliases.")]
    public class AliasEntity : DbEntity
    {
        private readonly string _alias = null!;
        private string _command = null!;
        private string? _arguments;

        /// <summary>
        /// The settings of the Discord guild this tag is associated with.
        /// </summary>
        /// <remarks>This property is <see langword="null"/> for global aliases.</remarks>
        public GuildConfigEntity? GuildConfigRel { get; init; }

        /// <summary>
        /// The ID of the Discord guild associated with this alias.
        /// </summary>
        /// <remarks>This property is <see langword="null"/> if the alias is global.</remarks>
        public ulong? GuildIdFK { get; init; }

        /// <summary>
        /// Defines whether this alias accepts additional parameters.
        /// </summary>
        public bool IsDynamic { get; set; }

        /// <summary>
        /// The command alias.
        /// </summary>
        [Required]
        [MaxLength(AkkoConstants.MaxMessageLength)]
        public string Alias
        {
            get => _alias;
            init => _alias = value.MaxLength(AkkoConstants.MaxMessageLength);
        }

        /// <summary>
        /// The qualified command name this alias is mapped to.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Command
        {
            get => _command;
            set => _command = value.MaxLength(200);
        }

        /// <summary>
        /// The arguments of the command alias, if any.
        /// </summary>
        [MaxLength(AkkoConstants.MaxMessageLength)]
        public string? Arguments
        {
            get => _arguments;
            set => _arguments = value?.MaxLength(AkkoConstants.MaxMessageLength);
        }

        /// <summary>
        /// Gets the full command string mapped to this alias.
        /// </summary>
        /// <remarks>This property is not mapped.</remarks>
        [NotMapped]
        public string FullCommand
            => string.IsNullOrWhiteSpace(Arguments) ? Command : $"{Command} {Arguments}";

        /// <summary>
        /// Gets the command associated with this alias.
        /// </summary>
        /// <param name="cmdHandler">The command handler.</param>
        /// <param name="arguments">Arguments of the command. <see cref="string.Empty"/> if there are none, <see langword="null"/> if the command is not found.</param>
        /// <returns>The command mapped to this alias.</returns>
        public Command GetCommand(CommandsNextExtension cmdHandler, out string arguments)
            => cmdHandler.FindCommand(Command + " " + Arguments, out arguments);

        /* Overrides */

        public static bool operator ==(AliasEntity x, AliasEntity y)
            => x.GuildIdFK == y.GuildIdFK && x.IsDynamic == y.IsDynamic && x.Alias == y.Alias && x.Command == y.Command && x.Arguments == y.Arguments;

        public static bool operator !=(AliasEntity x, AliasEntity y)
            => !(x == y);

        public override bool Equals(object? obj)
            => ReferenceEquals(this, obj) || (obj is not null && obj is AliasEntity dbAlias && this == dbAlias);

        public override int GetHashCode()
            => base.GetHashCode();
    }
}