using AkkoCore.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Abstractions
{
    /// <summary>
    /// Defines an object that manages command execution.
    /// </summary>
    public interface ICommandHandler
    {
        /// <summary>
        /// Checks the permissions for a given command context and executes it if the permissions allow it.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <returns>A <see cref="Task"/> object if the command was run, <see langword="null"/> otherwise.</returns>
        Task CheckAndExecuteAsync(CommandContext context);

        /// <summary>
        /// Gets the active permission override for the specified command.
        /// </summary>
        /// <param name="sid">ID of the Discord server, or <see langword="null"/> if the permission override is global.</param>
        /// <param name="cmd">The command to check the permission for.</param>
        /// <param name="permOverride">The resulting permission override or <see langword="null"/> if not found.</param>
        /// <returns><see langword="true"/> if the permission override for <paramref name="cmd"/> has been found, <see langword="false"/> otherwise.</returns>
        bool GetActiveOverride(ulong? sid, Command cmd, out PermissionOverrideEntity permOverride);

        /// <summary>
        /// Checks if the current context is allowed to run the command for the overriden permissions.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="permOverride">The command permission overrides.</param>
        /// <returns><see langword="true"/> if the command can run in the current context, <see langword="false"/> otherwise.</returns>
        bool IsAllowedOverridenContext(CommandContext context, PermissionOverrideEntity permOverride);

        /// <summary>
        /// Executes commands that are mapped to aliases.
        /// </summary>
        Task HandleCommandAliasAsync(DiscordClient client, MessageCreateEventArgs eventArgs);

        /// <summary>
        /// Handles command execution.
        /// </summary>
        Task HandleCommandAsync(DiscordClient client, MessageCreateEventArgs eventArgs);
    }
}