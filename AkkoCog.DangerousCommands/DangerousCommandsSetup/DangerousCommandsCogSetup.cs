using AkkoCore.Commands.Abstractions;
using AkkoCore.Config.Abstractions;
using AkkoCore.Services;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace AkkoCog.DangerousCommandsSetup
{
    /// <summary>
    /// Initializes this cog's dependencies.
    /// </summary>
    public class DangerousCommandsCogSetup : ICogSetup
    {
        public string LocalizationDirectory { get; } = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location)?.FullName ?? string.Empty, "Localization");

        // This cog doesn't have slash commands
        public IDictionary<Type, ulong?> SlashCommandsScope
            => new Dictionary<Type, ulong?>(0);

        public void RegisterServices(IServiceCollection ioc)
        {
            foreach (var type in AkkoUtilities.GetConcreteTypesOf(typeof(ICommandService)))
                ioc.AddSingleton(type);
        }

        public void RegisterCallbacks(DiscordShardedClient shardedClient)
        {
            // This cog doesn't need events.
        }

        public void RegisterComponentResponses(IInteractionResponseManager responseManager)
        {
            // This cog doesn't have interactive messages.
        }
    }
}