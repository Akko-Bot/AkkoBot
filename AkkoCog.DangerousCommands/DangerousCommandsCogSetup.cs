using AkkoCore.Abstractions;
using AkkoCore.Commands.Abstractions;
using AkkoCore.Services;
using DSharpPlus;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using System.Reflection;

namespace AkkoCog.DangerousCommands
{
    /// <summary>
    /// Initializes this cog's dependencies.
    /// </summary>
    public class DangerousCommandsCogSetup : ICogSetup
    {
        public string LocalizationDirectory { get; } = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)?.FullName ?? string.Empty, "Localization");

        public void RegisterServices(IServiceCollection ioc)
        {
            foreach (var type in AkkoUtilities.GetConcreteTypesOf(typeof(ICommandService)))
                ioc.AddSingleton(type);
        }

        public void RegisterCallbacks(DiscordShardedClient shardedClient)
        {
            // This cog doesn't need events.
        }
    }
}