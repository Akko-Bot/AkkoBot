using AkkoCore.Commands.Common;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;

namespace AkkoCore.Services.Events.Abstractions
{
    /// <summary>
    /// Represents an object that stores <see cref="DiscordMember"/> objects and parses
    /// messages depending on the amount of stored users from a given context.
    /// </summary>
    public interface IMemberAggregator : IDisposable
    {
        /// <summary>
        /// Adds a Discord user to this aggregator.
        /// </summary>
        /// <param name="user">The Discord user.</param>
        /// <returns><see langword="true"/> if the user got added, <see langword="false"/> otherwise.</returns>
        bool Add(DiscordMember user);

        /// <summary>
        /// Checks whether a bulk message will be sent under the specified Discord guild ID and waiting time.
        /// </summary>
        /// <param name="sid">The ID of the Discord guild.</param>
        /// <param name="time">The waiting time.</param>
        /// <returns>
        /// <see langword="true"/> if a bulk message is to be generated,
        /// <see langword="false"/> if an individualized message is to be generated.
        /// </returns>
        bool SendsBulk(ulong sid, TimeSpan time);

        /// <summary>
        /// Parses a message according to the context and amount of users stored in this aggregator.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="greeting">A message with command placeholders.</param>
        /// <returns>The parsed message.</returns>
        SmartString ParseMessage(CommandContext context, string greeting);
    }
}