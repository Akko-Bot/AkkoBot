using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace AkkoCore.SlashCommands.Attributes
{
    /// <summary>
    /// Checks if the slash command was executed in a Discord guild and responds with an error message if it wasn't.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
    public sealed class AkkoSlashRequireGuild : SlashCheckBaseAttribute
    {
        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            if (ctx.Guild is not null)
                return Task.FromResult(true);

            var embed = new SerializableDiscordEmbed()
                .WithDescription("slash_guild_cmd_error");

            _ = ctx.RespondLocalizedAsync(embed, true, true);

            return Task.FromResult(false);
        }
    }
}