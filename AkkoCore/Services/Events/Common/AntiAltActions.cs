using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Extensions;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Services.Events.Common;

/// <summary>
/// Handles punishment for alt accounts.
/// </summary>
[CommandService<IAntiAltActions>(ServiceLifetime.Singleton)]
internal sealed class AntiAltActions : IAntiAltActions
{
    private readonly RoleService _roleService;
    private readonly ChannelService _channelService;

    public AntiAltActions(RoleService roleService, ChannelService channelService)
    {
        _roleService = roleService;
        _channelService = channelService;
    }

    public async Task MuteAltAsync(CommandContext context, DiscordMember user, ulong muteRoleId)
    {
        if (!context.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasOneFlag(Permissions.Administrator | Permissions.ManageRoles | Permissions.MuteMembers)))
            return;

        var reason = context.FormatLocalized("auto_punish", context.Guild.CurrentMember.GetFullname(), "antialt_punish");

        if (!context.Guild.Roles.TryGetValue(muteRoleId, out var muteRole))
        {
            muteRole = await _roleService.FetchMuteRoleAsync(user.Guild);
            await _channelService.SetMuteOverwritesAsync(context.Guild, muteRole, reason, false);
        }

        if (_roleService.CheckHierarchy(context.Guild.CurrentMember, user)
            && context.Guild.CurrentMember.Hierarchy > muteRole.Position)
            await _roleService.MuteUserAsync(context, muteRole, user, TimeSpan.Zero, reason);
    }

    public async Task KickAltAsync(CommandContext context, DiscordMember user)
    {
        if (_roleService.CheckHierarchy(context.Guild.CurrentMember, user)
            && context.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasOneFlag(Permissions.Administrator | Permissions.KickMembers)))
            await user.RemoveAsync(context.FormatLocalized("auto_punish", context.Guild.CurrentMember.GetFullname(), "antialt_punish"));
    }

    public async Task BanAltAsync(CommandContext context, DiscordMember user)
    {
        if (_roleService.CheckHierarchy(context.Guild.CurrentMember, user)
            && context.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasOneFlag(Permissions.Administrator | Permissions.BanMembers)))
            await user.BanAsync(1, context.FormatLocalized("auto_punish", context.Guild.CurrentMember.GetFullname(), "antialt_punish"));
    }

    public async Task RoleAltAsync(CommandContext context, DiscordMember user, ulong roleId)
    {
        if (context.Guild.Roles.TryGetValue(roleId, out var role) && _roleService.CheckHierarchy(context.Guild.CurrentMember, user)
            && context.Guild.CurrentMember.Hierarchy > role.Position
            && context.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasOneFlag(Permissions.Administrator | Permissions.ManageRoles)))
            await user.GrantRoleAsync(role, context.FormatLocalized("auto_punish", context.Guild.CurrentMember.GetFullname(), "antialt_punish"));
    }
}