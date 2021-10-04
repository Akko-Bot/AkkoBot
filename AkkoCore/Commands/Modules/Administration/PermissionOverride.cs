using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services;
using AkkoCore.Services.Database.Entities;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration
{
    [Group("commandoverride"), Aliases("override")]
    [Description("cmd_override")]
    [RequireOwnerInDm, RequireUserPermissions(Permissions.Administrator)]
    public sealed class PermissionOverride : AkkoCommandModule
    {
        private readonly PermissionOverrideService _service;

        public PermissionOverride(PermissionOverrideService service)
            => _service = service;

        [Command("permission"), Aliases("perm")]
        [Description("cmd_override_permission")]
        public async Task AddOverrideAsync(CommandContext context, [Description("arg_command")] string command, [Description("arg_permissions")] params Permissions[] permissions)
        {
            var cmd = GetCommandFromInput(context, command);

            if (cmd is not null)
                await _service.SetPermissionOverrideAsync(context.Guild?.Id, cmd, x => x.Permissions = permissions.ToFlags());

            await context.Message.CreateReactionAsync((cmd is not null) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("clear")]
        [Description("cmd_override_clear")]
        public async Task RemoveOverrideAsync(CommandContext context, [Description("arg_command")] string command)
        {
            var cmd = GetCommandFromInput(context, command);

            var result = cmd is not null && await _service.RemovePermissionOverrideAsync(context.Guild?.Id, cmd.QualifiedName);

            await context.Message.CreateReactionAsync((result) ? AkkoStatics.SuccessEmoji : AkkoStatics.FailureEmoji);
        }

        [Command("clearall")]
        [Description("cmd_override_clearall")]
        public async Task ClearOverridesAsync(CommandContext context)
        {
            var embed = new SerializableDiscordEmbed();
            var amount = _service.GetPermissionOverrides(context.Guild?.Id).Count;

            if (amount is 0)
            {
                embed.WithDescription("override_list_empty");
                await context.RespondLocalizedAsync(embed);

                return;
            }

            embed.WithDescription(context.FormatLocalized("q_are_you_sure", "q_override_clearall", "q_yes", "q_no"));

            await context.RespondInteractiveAsync(embed, "q_yes", async () =>
            {
                embed.WithDescription(context.FormatLocalized("override_clearall", amount));

                await _service.ClearPermissionOverridesAsync(context.Guild?.Id);
                await context.RespondLocalizedAsync(embed);
            });
        }

        [Command("user")]
        [HiddenOverload]
        public async Task ToggleMemberOverrideAsync(CommandContext context, string command, DiscordMember user)
        {
            var cmd = GetCommandFromInput(context, command);

            if (cmd is null)
                return;

            var result = await _service.SetPermissionOverrideAsync(context.Guild.Id, cmd, x => ToggleCollection(x, x.AllowedUserIds, (long)user.Id));

            await SendToggleResponseAsync(context, (result) ? "override_user_add" : "override_user_remove", cmd.QualifiedName, user.GetFullname());
        }

        [Command("user")]
        [Description("cmd_override_user")]
        public async Task ToggleUserOverrideAsync(CommandContext context, [Description("arg_command")] string command, [Description("arg_discord_user")] DiscordUser user)
        {
            if (!AkkoUtilities.IsOwner(context, context.User.Id))
                return;

            var cmd = GetCommandFromInput(context, command);

            if (cmd is null)
                return;

            var result = await _service.SetPermissionOverrideAsync(context.Guild?.Id, cmd, x => ToggleCollection(x, x.AllowedUserIds, (long)user.Id));

            await SendToggleResponseAsync(context, (result) ? "override_user_add" : "override_user_remove", cmd.QualifiedName, user.GetFullname());
        }

        [Command("role")]
        [Description("cmd_override_role")]
        [RequireGuild]
        public async Task ToggleRoleOverrideAsync(CommandContext context, [Description("arg_command")] string command, [Description("arg_discord_role")] DiscordRole role)
        {
            var cmd = GetCommandFromInput(context, command);

            if (cmd is null)
                return;

            var result = await _service.SetPermissionOverrideAsync(context.Guild?.Id, cmd, x => ToggleCollection(x, x.AllowedRoleIds, (long)role.Id));

            await SendToggleResponseAsync(context, (result) ? "override_role_add" : "override_role_remove", cmd.QualifiedName, role.Name);
        }

        [Command("channel")]
        [Description("cmd_override_channel")]
        [RequireGuild]
        public async Task ToggleChannelOverrideAsync(CommandContext context, [Description("arg_command")] string command, [Description("arg_discord_channel")] DiscordChannel channel)
        {
            var cmd = GetCommandFromInput(context, command);

            if (cmd is null)
                return;

            var result = await _service.SetPermissionOverrideAsync(context.Guild?.Id, cmd, x => ToggleCollection(x, x.AllowedChannelIds, (long)channel.Id));

            await SendToggleResponseAsync(context, (result) ? "override_channel_add" : "override_channel_remove", cmd.QualifiedName, channel.Mention);
        }

        [GroupCommand, Command("list")]
        [Description("cmd_override_list")]
        public async Task ListOverridesAsync(CommandContext context)
        {
            var embed = new SerializableDiscordEmbed();
            var overrides = _service.GetPermissionOverrides(context.Guild?.Id)
                .Where(x => x.IsActive);

            if (!overrides.Any())
            {
                embed.WithDescription("override_list_empty");
                await context.RespondLocalizedAsync(embed, isError: true);

                return;
            }

            embed.WithTitle((context.Guild is null) ? "override_global_list_title" : "override_list_title")
                   .WithDescription(context.FormatLocalized("override_list_description", Formatter.InlineCode(context.Prefix + "help")));

            foreach (var permOverrides in overrides.SplitInto(AkkoConstants.LinesPerPage))
                embed.AddField(AkkoConstants.ValidWhitespace, string.Join("\n", permOverrides.Select(x => Formatter.InlineCode(context.Prefix + x.Command))), true);

            await context.RespondPaginatedByFieldsAsync(embed, 3);
        }

        /// <summary>
        /// Gets the command string, regardless of whether it was input with the prefix or not.
        /// </summary>
        /// <param name="prefix">The prefix used.</param>
        /// <param name="input">The user input.</param>
        /// <returns>The command string to be looked up in the command handler.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string GetCommandString(string prefix, string input)
        {
            return (input.StartsWith(prefix, StringComparison.Ordinal) && input.Length != prefix.Length)
                ? input[prefix.Length..]
                : input;
        }

        /// <summary>
        /// Gets the command from the user input.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="input">The user input.</param>
        /// <returns>The <see cref="Command"/> represented by the input or <see langword="null"/> if it was not found.</returns>
        private Command GetCommandFromInput(CommandContext context, string input)
        {
            var commandString = GetCommandString(context.Prefix, input);

            return context.CommandsNext.GetAllCommands()
                .FirstOrDefault(x => x.QualifiedName.Equals(commandString, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Adds an element to a collection if it's not present or removes it if it's present in the collection.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="entry">The entry being updated.</param>
        /// <param name="collection">The collection.</param>
        /// <param name="element">The element to be added or removed.</param>
        /// <returns><see langword="true"/> if the element got added, <see langword="false"/> if it got removed.</returns>
        private bool ToggleCollection<T>(PermissionOverrideEntity entry, ICollection<T> collection, T element)
        {
            var amount = collection.Count;

            if (collection.Contains(element))
                collection.Remove(element);
            else
                collection.Add(element);

            if (!entry.HasActiveIds && entry.Permissions is Permissions.None)
                entry.IsActive = false;

            return amount < collection.Count;
        }

        /// <summary>
        /// Sends a formatted message when an override toggle is run.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="responseKey">The response key to be formatted.</param>
        /// <param name="command">The qualified name of the command.</param>
        /// <param name="entity">The entity that was toggled.</param>
        private async Task SendToggleResponseAsync(CommandContext context, string responseKey, string command, string entity)
        {
            var embed = new SerializableDiscordEmbed()
                .WithDescription(context.FormatLocalized(responseKey, Formatter.InlineCode(context.Prefix + command), Formatter.Bold(entity)));

            await context.RespondLocalizedAsync(embed);
        }
    }
}