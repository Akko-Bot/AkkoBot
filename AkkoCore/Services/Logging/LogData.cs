using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System.Linq;

namespace AkkoCore.Services.Logging
{
    /// <summary>
    /// Wrapper for raw data that needs to be included on logs.
    /// </summary>
    public record LogData
    {
        /// <summary>
        /// An optional message to be displayed in the log.
        /// </summary>
        public string OptionalMessage { get; }

        /// <summary>
        /// The Discord client that processed the command.
        /// </summary>
        public DiscordClient Client { get; }

        /// <summary>
        /// The Discord guild where the command was executed,
        /// <see langword="null"/> if execution took place in direct message.
        /// </summary>
        public DiscordGuild Guild { get; }

        /// <summary>
        /// The Discord user that executed the command.
        /// </summary>
        public DiscordUser User { get; }

        /// <summary>
        /// The Discord channel where the command was executed.
        /// </summary>
        public DiscordChannel Channel { get; }

        /// <summary>
        /// The message that triggered the command.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Determines whether this <see cref="LogData"/> was instantiated with a command context or not.
        /// If this is <see langword="true"/>, all properties will be properly instantiated,
        /// otherwise they will all be set to <see langword="default"/>.
        /// </summary>
        public bool HasContext
            => Client is not null;

        /// <summary>
        /// Initializes a log data.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="optionalMessage">An optional message to be displayed in the log.</param>
        public LogData(CommandContext context, string optionalMessage)
        {
            OptionalMessage = optionalMessage;

            // Initialization
            Client = context.Client;
            Guild = context.Guild;
            User = context.User;
            Channel = context.Channel;
            Message = context.Message.Content;
        }

        /// <summary>
        /// Initializes a log data.
        /// </summary>
        /// <param name="context">The interaction context.</param>
        /// <param name="optionalMessage">An optional message to be displayed in the log.</param>
        public LogData(InteractionContext context, string optionalMessage)
        {
            OptionalMessage = optionalMessage;

            // Initialization
            Client = context.Client;
            Guild = context.Guild;
            User = context.User;
            Channel = context.Channel;
            Message = (context.Interaction.Data.Options is null)
                ? "/" + context.Interaction.Data.Name
                : $"/{context.Interaction.Data.Name} {string.Join(' ', context.Interaction.Data.Options.Select(x => x.Value))}";
        }
    }
}