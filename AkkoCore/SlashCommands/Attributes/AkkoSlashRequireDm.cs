using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace AkkoCore.SlashCommands.Attributes
{
    /// <summary>
    /// Checks if the slash command was executed in direct message and responds with an error message if it wasn't.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
    public sealed class AkkoSlashRequireDm : SlashCheckBaseAttribute
    {
        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            if (ctx.Guild is null)
                return Task.FromResult(true);

            var embed = new SerializableDiscordEmbed()
                .WithDescription("slash_dm_cmd_error");

            _ = ctx.RespondLocalizedAsync(embed, true, true);

            return Task.FromResult(false);
        }
    }
}