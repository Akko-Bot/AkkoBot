using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using DSharpPlus;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace AkkoCore.SlashCommands.Attributes
{
    /// <summary>
    /// Checks if both the user and the bot are allowed to execute a slash command and responds with an error message if one of them isn't.
    /// </summary>
    [AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
    public sealed class AkkoSlashRequirePermission : SlashCheckBaseAttribute
    {
        private readonly Permissions _permissions;

        public AkkoSlashRequirePermission(Permissions permissions)
            => _permissions = permissions;

        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            var isUserAllowed = ctx.Member is null || ctx.Member.PermissionsIn(ctx.Channel).HasPermission(_permissions);
            var isBotAllowed = ctx.Guild is null || ctx.Guild.CurrentMember.PermissionsIn(ctx.Channel).HasPermission(_permissions);

            if (isUserAllowed || isBotAllowed)
                return Task.FromResult(true);

            var embed = new SerializableDiscordEmbed()
                .WithDescription((isUserAllowed) ? "slash_user_cmd_error" : "slash_bot_cmd_error");

            _ = ctx.RespondLocalizedAsync(embed, true, true);

            return Task.FromResult(false);
        }
    }
}