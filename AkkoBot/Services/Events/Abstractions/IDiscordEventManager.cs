namespace AkkoBot.Services.Events.Abstractions
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
        void RegisterEvents();

        /// <summary>
        /// Registers events that trigger before the bot has connected to Discord.
        /// </summary>
        void RegisterStartupEvents();

        /// <summary>
        /// Unregisters events that trigger after the bot has connected to Discord.
        /// </summary>
        void UnregisterEvents();

        /// <summary>
        /// Unregisters events that trigger before the bot has connected to Discord.
        /// </summary>
        void UnregisterStartupEvents();
    }
}