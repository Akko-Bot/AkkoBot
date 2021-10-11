using AkkoCore.Services.Events.Abstractions;
using AkkoCore.Services.Events.Controllers.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace AkkoCore.Config.Abstractions
{
    /// <summary>
    /// Represents an object that defines actions necessary for a cog to work.
    /// </summary>
    public interface ICogSetup
    {
        /// <summary>
        /// The full path to the directory where the localized response strings are stored.
        /// </summary>
        /// <remarks>The files must be in Yaml format.</remarks>
        string LocalizationDirectory { get; }

        /// <summary>
        /// The type of the slash command module and the context they should run.
        /// </summary>
        /// <remarks>The value is the ID of the guild the commands should be available to or <see langword="null"/> if the commands should be global.</remarks>
        IReadOnlyDictionary<Type, ulong?> SlashCommandsScope { get; }

        /// <summary>
        /// Subscribes or unsubscribes methods to certain Discord websocket events.
        /// </summary>
        /// <param name="shardedClient">The bot's sharded client.</param>
        void RegisterCallbacks(DiscordShardedClient shardedClient);

        /// <summary>
        /// Registers or unregisters argument converters into the command handler.
        /// </summary>
        /// <param name="cmdHandler">The command handler.</param>
        /// <remarks>
        /// Use <see cref="CommandsNextExtension.RegisterConverter{T}(IArgumentConverter{T})"/>
        /// to register converters and <see cref="CommandsNextExtension.UnregisterConverter{T}"/> to unregister them.
        /// </remarks>
        void RegisterArgumentConverters(CommandsNextExtension cmdHandler);

        /// <summary>
        /// Registers services from this cog to the bot's IoC container.
        /// </summary>
        /// <param name="ioc">The IoC container.</param>
        void RegisterServices(IServiceCollection ioc);

        /// <summary>
        /// Registers interactive controllers from this cog.
        /// </summary>
        /// <param name="responseGenerator">The generator to register the responses to.</param>
        /// <remarks>
        /// Use <see cref="IInteractionResponseManager.Add(ISlashController)"/> or
        /// <see cref="IInteractionResponseManager.AddRange(IEnumerable{ISlashController})"/>
        /// to add the interaction controllers defined in this cog.
        /// </remarks>
        /// <exception cref="ArgumentException">Occurs when a two responses get registered under the same ID.</exception>
        public void RegisterComponentResponses(IInteractionResponseManager responseGenerator);
    }
}