using AkkoBot.Commands.Abstractions;
using AkkoBot.Commands.Attributes;
using AkkoBot.Commands.Modules.Administration.Services;
using AkkoBot.Commands.Modules.Self.Services;
using AkkoBot.Extensions;
using AkkoBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Commands.Modules.Administration
{
    [RequireGuild]
    public class Administration : AkkoCommandModule
    {
        private readonly GuildConfigService _guildService;
        private readonly BotConfigService _botService;

        /// <summary>
        /// Represents the (allowed) permissions of the roles that should be exempt from channel lockdowns.
        /// </summary>
        private const Permissions _lockChannelModPerms = Permissions.KickMembers | Permissions.BanMembers | Permissions.ManageChannels;

        /// <summary>
        /// Represents the permissions that should be denied for lockdown overwrites.
        /// </summary>
        private const Permissions _lockPerms = Permissions.SendMessages | Permissions.AddReactions;

        public Administration(GuildConfigService guildService, BotConfigService botService)
        {
            _guildService = guildService;
            _botService = botService;
        }

        [BotOwner, Hidden]
        [Command("sudo")]
        [Description("cmd_sudo")]
        public async Task Sudo(
            CommandContext context,
            [Description("arg_discord_user")] DiscordUser user,
            [RemainingText, Description("arg_command")] string command)
        {
            var cmd = context.CommandsNext.FindCommand(command, out var args);

            if (cmd is null)
            {
                var embed = new DiscordEmbedBuilder().WithDescription("command_not_found");
                await context.RespondLocalizedAsync(embed, isError: true);

                return;
            }

            var fakeContext = context.CommandsNext.CreateFakeContext(user, context.Channel, command, context.Prefix, cmd, args);
            var failedChecks = await cmd.RunChecksAsync(fakeContext, false);

            if (failedChecks.Any())
            {
                var embed = new DiscordEmbedBuilder().WithDescription("command_check_failed");
                await context.RespondLocalizedAsync(embed, isError: true);
            }
            else
            {
                await cmd.ExecuteAsync(fakeContext);
            }
        }

        [Command("prefix")]
        [Description("cmd_guild_prefix")]
        public async Task ChangePrefix(CommandContext context, [RemainingText, Description("arg_prefix")] string newPrefix = null)
        {
            if (string.IsNullOrWhiteSpace(newPrefix) || !context.Member.PermissionsIn(context.Channel).HasFlag(Permissions.ManageGuild))
            {
                await CheckPrefixAsync(context);
                return;
            }

            // Account for dumb users - this is not 100% accurate
            var prefix = newPrefix.Replace("set ", string.Empty);

            _guildService.GetOrSetProperty(context, x => x.Prefix = prefix);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_prefix_change", Formatter.InlineCode(prefix)));

            await context.RespondLocalizedAsync(embed);
        }

        [Command("prune"), HiddenOverload]
        public async Task Prune(CommandContext context, int amount = 50, string options = "")
            => await Prune(context, null, amount, options);

        [Command("prune"), Aliases("clear")]
        [Description("cmd_prune")]
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task Prune(CommandContext context, 
            [Description("arg_discord_user")] DiscordUser user = null, 
            [Description("arg_int")] int amount = 50, 
            [Description("arg_prune_options")] string options = "")
        {
            amount = Math.Abs(amount) + 1;
            var requestLimit = (int)Math.Ceiling(Math.Min(amount, 400) / 100.0) * 100;  // Limit it to 4 requests at most

            Func<DiscordMessage, bool> userCheck = (user is null) ? (msg) => true : (msg) => msg.Author.Equals(user);
            Func<DiscordMessage, bool> optionsCheck = (!options.Equals(StringComparison.InvariantCultureIgnoreCase, "-s", "--safe")) ? (msg) => true : (msg) => !msg.Pinned;

            var messages = (await context.Channel.GetMessagesAsync(requestLimit))
                .Where(msg => DateTimeOffset.Now.Subtract(msg.CreationTimestamp) < AkkoEntities.MaxMessageDeletionAge && userCheck(msg) && optionsCheck(msg))
                .Take(amount);

            if (!messages.Any(message => !message.Equals(context.Message)))
            {
                var embed = new DiscordEmbedBuilder()
                    .WithDescription("prune_error");

                await context.RespondLocalizedAsync(embed, isError: true);
            }

            await context.Channel.DeleteMessagesAsync(messages);
        }

        [Command("lockchannel"), Aliases("lockdown", "lock")]
        [Description("cmd_lockchannel")]
        [RequirePermissions(Permissions.ManageChannels)]
        public async Task LockChannel(CommandContext context)
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
            ).ToArray();

            // Lock the channel down to regular users
            for (var index = 0; index < toLock.Length; index++)
                await toLock[index].UpdateAsync(null, _lockPerms, context.FormatLocalized("lockchannel_reason"));

            // Unlock the channel to mods
            foreach (var role in modRoles)
                await context.Channel.AddOverwriteAsync(role, _lockPerms, Permissions.None, context.FormatLocalized("lockchannel_reason"));

            await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
        }

        [Command("unlockchannel"), Aliases("release", "unlock")]
        [Description("cmd_unlockchannel")]
        [RequirePermissions(Permissions.ManageChannels)]
        public async Task UnlockChannel(CommandContext context)
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
            ).ToArray();

            // Unlock the channel for regular users
            for (var index = 0; index < toUnlock.Length; index++)
                await toUnlock[index].UpdateAsync(null, Permissions.None, context.FormatLocalized("unlockchannel_reason"));

            // Remove the overwrites for the mod roles
            foreach(var role in modRoles)
            {
                var modOverwrite = context.Channel.PermissionOverwrites
                    .FirstOrDefault(overwrite => overwrite.Id == role.Id && overwrite.Allowed.HasFlag(_lockPerms));

                if (modOverwrite is not null)
                    await modOverwrite.DeleteAsync();
            }

            await context.Message.CreateReactionAsync(AkkoEntities.SuccessEmoji);
        }

        /// <summary>
        /// Sends a message with the guild prefix to the context that triggered it.
        /// </summary>
        /// <param name="context">The command context.</param>
        private async Task CheckPrefixAsync(CommandContext context)
        {
            var prefix = _guildService.GetOrSetProperty(context, x => x?.Prefix)
                ?? _botService.GetOrSetProperty(x => x.BotPrefix);

            var embed = new DiscordEmbedBuilder()
                .WithDescription(context.FormatLocalized("guild_prefix_check", Formatter.InlineCode(prefix)));

            await context.RespondLocalizedAsync(embed);
        }
    }
}