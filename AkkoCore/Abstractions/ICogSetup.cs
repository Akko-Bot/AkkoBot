using DSharpPlus;
using Microsoft.Extensions.DependencyInjection;

namespace AkkoCore.Abstractions
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
        /// Subscribes or unsubscribes methods to certain Discord websocket events.
        /// </summary>
        /// <param name="shardedClient">The bot's sharded client.</param>
        void RegisterCallbacks(DiscordShardedClient shardedClient);

        /// <summary>
        /// Registers services from this cog to the bot's IoC container.
        /// </summary>
        /// <param name="ioc">The IoC container.</param>
        void RegisterServices(IServiceCollection ioc);
    }
}
