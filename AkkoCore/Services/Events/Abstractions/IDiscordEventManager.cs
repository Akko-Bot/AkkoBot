using DSharpPlus;
using System;

namespace AkkoCore.Services.Events.Abstractions
{
    /// <summary>
    /// Represents an object responsible for registering methods that
    /// define the behavior the bot should have for specific user actions.
    /// </summary>
    public interface IDiscordEventManager
    {
        /// <summary>
        /// Registers events that trigger after the bot has connected to Discord.
        /// </summary>
        void RegisterDefaultEvents();

        /// <summary>
        /// Subscribes or unsubscribes the specified methods to Discord websocket events defined by <paramref name="setter"/>.
        /// </summary>
        /// <param name="setter">The method that registers methods to the <see cref="DiscordShardedClient"/>.</param>
        void ManageCallbacks(Action<DiscordShardedClient> setter);

        /// <summary>
        /// Registers events that trigger before the bot has connected to Discord.
        /// </summary>
        void RegisterStartupEvents();

        /// <summary>
        /// Unregisters events that trigger after the bot has connected to Discord.
        /// </summary>
        void UnregisterDefaultEvents();

        /// <summary>
        /// Unregisters events that trigger before the bot has connected to Discord.
        /// </summary>
        void UnregisterStartupEvents();
    }
}