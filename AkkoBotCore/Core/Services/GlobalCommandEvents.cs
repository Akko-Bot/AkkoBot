using AkkoBot.Core.Common;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Core.Services
{
    internal class GlobalCommandEvents
    {
        private readonly IServiceProvider _services;
        private readonly BotCore _botCore;

        internal GlobalCommandEvents(BotCore botCore, IServiceProvider services)
        {
            _services = services;
            _botCore = botCore;
        }

        /// <summary>
        /// Defines the behaviors the bot should have for specific user actions.
        /// </summary>
        internal void RegisterEvents()
        {
            // Prevent mute evasion
            _botCore.BotShardedClient.GuildMemberAdded += ReMuteAsync;

            // Show prefix regardless of current config
            _botCore.BotShardedClient.MessageCreated += DefaultPrefix;
        }

        /* Event Methods */

        /// <summary>
        /// Mutes a user that has been previously muted.
        /// </summary>
        private async Task ReMuteAsync(object sender, GuildMemberAddEventArgs eventArgs)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var anyChannel = eventArgs.Guild.Channels.FirstOrDefault().Value;
            var botPerms = eventArgs.Guild.CurrentMember.PermissionsIn(anyChannel);

            // Check if user is in the database
            var guildSettings = await db.GuildConfig.GetGuildWithMutesAsync(eventArgs.Guild.Id).ConfigureAwait(false);
            var mutedUser = guildSettings.MutedUserRel.FirstOrDefault(x => x.UserId == eventArgs.Member.Id);

            if (mutedUser is not null && botPerms.HasFlag(Permissions.ManageRoles))
            {
                if (eventArgs.Guild.Roles.TryGetValue(guildSettings.MuteRoleId, out var muteRole))
                {
                    // If mute role exists, apply to the user
                    muteRole = eventArgs.Guild.GetRole(guildSettings.MuteRoleId);
                    await eventArgs.Member.GrantRoleAsync(muteRole).ConfigureAwait(false);
                }
                else
                {
                    // If mute role doesn't exist anymore, delete the mute from the database
                    guildSettings.MutedUserRel.Remove(mutedUser);

                    db.GuildConfig.Update(guildSettings);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Makes the bot always respond to "!prefix", regardless of the currently set prefix.
        /// </summary>
        private Task DefaultPrefix(object sender, MessageCreateEventArgs eventArgs)
        {
            if (eventArgs.Message.Content.StartsWith("!prefix", StringComparison.InvariantCultureIgnoreCase))
            {
                using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
                var prefix = db.GuildConfig.GetGuild(eventArgs.Guild?.Id ?? 0)?.Prefix
                    ?? db.BotConfig.Cache.BotPrefix;

                if (eventArgs.Guild is not null && prefix.Equals("!"))
                    return Task.CompletedTask;

                // Get client, command handler and prefix command
                var client = sender as DiscordClient;
                var cmdHandler = client.GetExtension<CommandsNextExtension>();
                var cmd = cmdHandler.FindCommand(eventArgs.Message.Content.Remove(0, prefix.Length), out var cmdArgs);

                // Create the context and execute the command
                if (string.IsNullOrWhiteSpace(cmdArgs) || eventArgs.Guild is not null)
                {
                    var context = cmdHandler.CreateContext(eventArgs.Message, prefix, cmd, cmdArgs);
                    _ = cmd.ExecuteAndLogAsync(context);
                }
            }

            return Task.CompletedTask;
        }
    }
}