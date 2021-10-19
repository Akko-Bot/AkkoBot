using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace AkkoCore.SlashCommands.Attributes
{
    /// <summary>
    /// Checks if the bot is allowed to execute a slash command and responds with an error message if it isn't.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
    public sealed class AkkoSlashRequireBotPermission : SlashCheckBaseAttribute
    {
        private readonly Permissions _permissions;

        public AkkoSlashRequireBotPermission(Permissions permissions)
            => _permissions = permissions;

        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            if (ctx.Guild is null || ctx.Guild.CurrentMember.PermissionsIn(ctx.Channel).HasPermission(_permissions))
                return Task.FromResult(true);

            var embed = new SerializableDiscordEmbed()
                .WithDescription("slash_bot_cmd_error");

            _ = ctx.RespondLocalizedAsync(embed, true, true);

            return Task.FromResult(false);
        }
    }
}