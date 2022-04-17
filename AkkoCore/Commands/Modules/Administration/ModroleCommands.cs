using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Kotz.Extensions;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration;

[Group("modrole"), Aliases("modroles")]
[Description("cmd_modrole")]
[RequireGuild]
public sealed class ModroleCommands : AkkoCommandModule
{
    private readonly ModroleService _service;

    public ModroleCommands(ModroleService service)
        => _service = service;

    [Command("add")]
    [Description("cmd_modrole_add")]
    [RequirePermissions(Permissions.ManageRoles)]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task CreateModRoleAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole modrole, [Description("arg_discord_roles")] params DiscordRole[] targetRoles)
    {
        var added = await _service.SetModroleAsync(context.Guild.Id, modrole.Id, x =>
        {
            var amount = x.TargetRoleIds.Count;

            foreach (var targetRole in targetRoles)
            {
                if (!x.TargetRoleIds.Contains((long)targetRole.Id))
                    x.TargetRoleIds.Add((long)targetRole.Id);
            }

            return x.TargetRoleIds.Count - amount;
        });

        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized((added is 0) ? "modrole_add_fail" : "modrole_add_success", added, Formatter.InlineCode(modrole.Name)));

        await context.RespondLocalizedAsync(embed, isError: added is 0);
    }

    [Command("remove"), Aliases("rm")]
    [Description("cmd_modrole_remove")]
    [RequirePermissions(Permissions.ManageRoles)]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task RemoveModRoleAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole modrole)
        => await RemoveModRoleAsync(context, modrole.Id);

    [Command("remove"), HiddenOverload]
    public async Task RemoveModRoleAsync(CommandContext context, ulong modroleId)
    {
        var success = await _service.DeleteModroleAsync(context.Guild.Id, modroleId);
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("remove")]
    public async Task RemoveTargetRolesAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole modrole, [Description("arg_discord_roles")] params DiscordRole[] targetRoles)
    {
        var removed = await _service.SetModroleAsync(context.Guild.Id, modrole.Id, x =>
        {
            var amount = x.TargetRoleIds.Count;

            foreach (var targetRole in targetRoles)
                x.TargetRoleIds.Remove((long)targetRole.Id);

            return amount - x.TargetRoleIds.Count;
        });

        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized((removed is 0) ? "modrole_remove_fail" : "modrole_remove_success", modrole.Name, removed));

        await context.RespondLocalizedAsync(embed, isError: removed is 0);
    }

    [Command("behavior")]
    [Description("cmd_modrole_behavior")]
    [RequirePermissions(Permissions.ManageRoles)]
    [RequireUserPermissions(Permissions.ManageGuild)]
    public async Task SetHierarchyEnforcementAsync(CommandContext context, [Description("arg_discord_role")] DiscordRole modrole, [Description("arg_modrole_behavior")] ModroleBehavior behavior)
    {
        if (!_service.GetModroles(context.Guild.Id).Any(x => x.ModroleId == modrole.Id))
        {
            var errorEmbed = new SerializableDiscordEmbed()
                .WithDescription("modrole_not_valid");

            await context.RespondLocalizedAsync(errorEmbed, isError: true);
            return;
        }

        var result = await _service.SetModroleAsync(context.Guild.Id, modrole.Id, x => x.Behavior = x.Behavior.ToggleFlag(behavior));

        var embed = new SerializableDiscordEmbed()
            .WithDescription(
                context.FormatLocalized(
                    "modrole_behavior_toggle",
                    Formatter.InlineCode(behavior.ToString()),
                    (result.HasFlag(behavior)) ? "enabled" : "disabled",
                    Formatter.Bold(modrole.Name)
                )
            );

        await context.RespondLocalizedAsync(embed);
    }

    [Command("set")]
    [Description("cmd_modrole_set")]
    [RequireBotPermissions(Permissions.ManageRoles)]
    public async Task SetTargetRoleAsync(CommandContext context, [Description("arg_discord_user")] DiscordMember user, [Description("arg_discord_role")] DiscordRole role)
    {
        var success = await _service.AddTargetRoleAsync(context.Guild, context.Member!, user, role);
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [Command("unset")]
    [Description("cmd_modrole_unset")]
    [RequireBotPermissions(Permissions.ManageRoles)]
    public async Task UnsetTargetRoleAsync(CommandContext context, [Description("arg_discord_user")] DiscordMember user, [Description("arg_discord_role")] DiscordRole role)
    {
        var success = await _service.RemoveTargetRoleAsync(context.Guild, context.Member!, user, role);
        await context.Message.CreateReactionAsync((success) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
    }

    [GroupCommand, Command("list")]
    [Description("cmd_modrole_list")]
    public async Task ListModrolesAsync(CommandContext context)
    {
        var modroles = _service.GetModroles(context.Guild.Id);
        var embed = new SerializableDiscordEmbed();

        if (modroles.Count is not 0)
        {
            foreach (var modrole in modroles)
            {
                if (modrole.TargetRoleIds.Count is 0 || !context.Guild.Roles.TryGetValue(modrole.ModroleId, out var discordModrole))
                {
                    await _service.DeleteModroleAsync(context.Guild.Id, modrole.ModroleId);
                    continue;
                }

                embed.AddField(
                    GetModroleString(context, discordModrole.Id),
                    string.Join('\n', modrole.TargetRoleIds.Select(x => GetModroleString(context, (ulong)x))),
                    true
                );
            }
        }

        if (embed.Fields is not { Count: > 0 })
            embed.WithDescription("modroles_list_empty");
        else
            embed.WithTitle("modroles_list_title");

        await context.RespondLocalizedAsync(embed, isError: embed.Fields is not { Count: > 0 });
    }

    /// <summary>
    /// Gets the exibition string for a given modrole ID.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="modroleId">The role ID of the modrole.</param>
    /// <returns>The modrole exibition string.</returns>
    private string GetModroleString(CommandContext context, ulong modroleId)
    {
        _service.GetModrole(context.Guild.Id, modroleId, out var dbModrole);

        return (context.Guild.Roles.TryGetValue(modroleId, out var modrole))
            ? $"{modrole.Name} ({modrole.Id}) {GetModroleEmojis(dbModrole)}"
            : $"{context.FormatLocalized("deleted_role")} ({modroleId}) {GetModroleEmojis(dbModrole)}";
    }

    /// <summary>
    /// Gets the emoji for the behaviors of the specified modrole.
    /// </summary>
    /// <param name="dbModrole">The modrole.</param>
    /// <returns>The modrole's emojis or <see cref="string.Empty"/> if there is no custom behavior.</returns>
    private string GetModroleEmojis(ModroleEntity? dbModrole)
    {
        return dbModrole?.Behavior switch
        {
            ModroleBehavior.EnforceHierarchy => "⏫",
            ModroleBehavior.Exclusive => "📍",
            ModroleBehavior.All => "⏫📍",
            _ => string.Empty
        };
    }
}