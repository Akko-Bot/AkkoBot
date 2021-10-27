using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace AkkoCore.SlashCommands.Attributes
{
    /// <summary>
    /// Checks if the slash command was executed in direct message and sends an error message if it nas not issued by a bot owner.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
    public sealed class AkkoSlashRequireUserPermissionAttribute : SlashCheckBaseAttribute
    {
        private readonly Permissions _permissions;

        public AkkoSlashRequireUserPermissionAttribute(Permissions permissions)
            => _permissions = permissions;

        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            if (ctx.Member is null || ctx.Member.PermissionsIn(ctx.Channel).HasPermission(_permissions))
                return Task.FromResult(true);

            var embed = new SerializableDiscordEmbed()
                .WithDescription("slash_user_cmd_error");

            _ = ctx.RespondLocalizedAsync(embed, true, true);

            return Task.FromResult(false);
        }
    }
}