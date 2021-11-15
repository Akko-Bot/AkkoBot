using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services;
using AkkoCore.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration;

public sealed class Administration : AkkoCommandModule
{
    private readonly ICommandHandler _commandHandler;
    private readonly GuildConfigService _guildService;

    /// <summary>
    /// Represents the (allowed) permissions of the roles that should be exempt from channel lockdowns.
    /// </summary>
    private const Permissions _lockChannelModPerms = Permissions.Administrator | Permissions.KickMembers | Permissions.BanMembers | Permissions.ManageChannels;

    /// <summary>
    /// Represents the permissions that should be denied for lockdown overwrites.
    /// </summary>
    private const Permissions _lockPerms = Permissions.SendMessages | Permissions.AddReactions;

    public Administration(ICommandHandler commandHandler, GuildConfigService guildService)
    {
        _commandHandler = commandHandler;
        _guildService = guildService;
    }

    [Command("sudo")]
    [Description("cmd_sudo")]
    [BotOwner, RequireGuild, Hidden]
    public async Task SudoAsync(
        CommandContext context,
        [Description("arg_discord_user")] DiscordUser user,
        [RemainingText, Description("arg_command")] string command)
    {
        var cmd = context.CommandsNext.FindCommand(command, out var args);

        if (cmd is null)
        {
            var embed = new SerializableDiscordEmbed().WithDescription("command_not_found");
            await context.RespondLocalizedAsync(embed, isError: true);

            return;
        }

        var fakeContext = context.CommandsNext.CreateFakeContext(user, context.Channel, context.Prefix + command, context.Prefix, cmd, args);

        if (_commandHandler.CheckAndExecuteAsync(fakeContext) is null)
        {
            var embed = new SerializableDiscordEmbed().WithDescription("command_check_failed");
            await context.RespondLocalizedAsync(embed, isError: true);
        }
    }

    [Command("prefix"), HiddenOverload] // Account for dumb users
    public async Task ChangeDumbPrefixAsync(CommandContext context, string dumb, [RemainingText] string intendedPrefix)
        => await ChangePrefixAsync(context, (dumb.Equals("set", StringComparison.InvariantCultureIgnoreCase) ? intendedPrefix : context.RawArgumentString));

    [Command("prefix")]
    [Description("cmd_guild_prefix")]
    public async Task ChangePrefixAsync(CommandContext context, [RemainingText, Description("arg_prefix")] string? newPrefix = default)
    {
        if (context.Guild is null || string.IsNullOrWhiteSpace(newPrefix) || !context.Member.PermissionsIn(context.Channel).HasPermission(Permissions.ManageGuild))
        {
            await CheckPrefixAsync(context);
            return;
        }

        await _guildService.SetPropertyAsync(context.Guild, x => x.Prefix = newPrefix);

        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized("guild_prefix_change", Formatter.InlineCode(newPrefix)));

        await context.RespondLocalizedAsync(embed);
    }

    [Command("prune"), HiddenOverload]
    public async Task PruneAsync(CommandContext context, int amount = 50, string options = "")
        => await PruneAsync(context, null, amount, options);

    [Command("prune"), Aliases("clear")]
    [Description("cmd_prune")]
    [RequireGuild, RequirePermissions(Permissions.ManageMessages)]
    public async Task PruneAsync(CommandContext context,
        [Description("arg_discord_user")] DiscordUser? user = default,
        [Description("arg_int")] int amount = 50,
        [Description("arg_prune_options")] string options = "")
    {
        bool UserCheck(DiscordMessage msg)
            => user is null || msg.Author.Equals(user);

        bool OptionsCheck(DiscordMessage msg)
            => (!options.Equals(StringComparison.InvariantCultureIgnoreCase, "-s", "--safe")) || !msg.Pinned;

        amount = Math.Abs(amount);
        var requestLimit = AkkoUtilities.GetMaxMessageRequest(amount, 4);  // Limit it to 4 requests at most
        var messages = (await context.Channel.GetMessagesBeforeAsync(context.Message.Id, requestLimit))
            .Where(msg => DateTimeOffset.Now.Subtract(msg.CreationTimestamp) < AkkoStatics.MaxMessageDeletionAge && UserCheck(msg) && OptionsCheck(msg))
            .Take(amount);

        if (!messages.Any())
        {
            var embed = new SerializableDiscordEmbed()
                .WithDescription("prune_error");

            await context.RespondLocalizedAsync(embed, isError: true);
        }

        await context.Channel.DeleteMessagesAsync(messages.Prepend(context.Message));
    }

    [Command("lockchannel"), Aliases("lockdown", "lock")]
    [Description("cmd_lockchannel")]
    [RequireGuild, RequirePermissions(Permissions.ManageChannels)]
    public async Task LockChannelAsync(CommandContext context)
    {
        // Get the roles from the server mods
        var modRoles = context.Guild.Roles.Values
            .Where(role => role.Permissions.HasOneFlag(_lockChannelModPerms));

        // Get overwrites from non-mod roles
        var toLock = context.Channel.PermissionOverwrites.Where(overwrite =>
            !overwrite.Denied.HasFlag(Permissions.SendMessages) // Exclude the mute role
            && context.Guild.Roles.Values
                .Except(modRoles)
                .Select(role => role.Id)
                    .Contains(overwrite.Id)
        )
        .ToArray();

        // Lock the channel down to regular users
        for (var index = 0; index < toLock.Length; index++)
            await toLock[index].UpdateAsync(null, _lockPerms, context.FormatLocalized("lockchannel_reason"));

        // Unlock the channel to mods
        foreach (var role in modRoles)
            await context.Channel.AddOverwriteAsync(role, _lockPerms, Permissions.None, context.FormatLocalized("lockchannel_reason"));

        await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
    }

    [Command("unlockchannel"), Aliases("release", "unlock")]
    [Description("cmd_unlockchannel")]
    [RequireGuild, RequirePermissions(Permissions.ManageChannels)]
    public async Task UnlockChannelAsync(CommandContext context)
    {
        // Get the roles from the server mods
        var modRoles = context.Guild.Roles.Values
            .Where(role => role.Permissions.HasOneFlag(_lockChannelModPerms));

        // Get overwrites from non-mod roles
        var toUnlock = context.Channel.PermissionOverwrites.Where(overwrite =>
            !overwrite.Denied.HasFlag(Permissions.SendMessages) // Exclude the mute role
            && context.Guild.Roles.Values
                .Except(modRoles)
                .Select(role => role.Id)
                    .Contains(overwrite.Id)
        )
        .ToArray();

        // Unlock the channel for regular users
        for (var index = 0; index < toUnlock.Length; index++)
            await toUnlock[index].UpdateAsync(null, Permissions.None, context.FormatLocalized("unlockchannel_reason"));

        // Remove the overwrites for the mod roles
        foreach (var role in modRoles)
        {
            var modOverwrite = context.Channel.PermissionOverwrites
                .FirstOrDefault(overwrite => overwrite.Id == role.Id && overwrite.Allowed.HasFlag(_lockPerms));

            if (modOverwrite is not null)
                await modOverwrite.DeleteAsync();
        }

        await context.Message.CreateReactionAsync(AkkoStatics.SuccessEmoji);
    }

    /// <summary>
    /// Sends a message with the guild prefix to the context that triggered it.
    /// </summary>
    /// <param name="context">The command context.</param>
    private async Task CheckPrefixAsync(CommandContext context)
    {
        var prefix = context.GetMessageSettings().Prefix;

        var response = (context.Guild is null) ? "bot_prefix_check" : "guild_prefix_check";
        var embed = new SerializableDiscordEmbed()
            .WithDescription(context.FormatLocalized(response, Formatter.InlineCode(prefix)));

        await context.RespondLocalizedAsync(embed);
    }
}