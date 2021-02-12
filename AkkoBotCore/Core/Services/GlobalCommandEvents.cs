using System.Linq;
using System;
using AkkoBot.Core.Common;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.EventArgs;
using System.Threading.Tasks;
using AkkoBot.Extensions;
using DSharpPlus;

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
            _botCore.BotShardedClient.GuildMemberAdded += ReMute;
        }


        /* Event Methods */

        /// <summary>
        /// Mutes a user that has been previously muted.
        /// </summary>
        private async Task ReMute(object sender, GuildMemberAddEventArgs eventArgs)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);

            var anyChannel = eventArgs.Guild.Channels.FirstOrDefault().Value;
            var botPerms = eventArgs.Guild.CurrentMember.PermissionsIn(anyChannel);

            // Check if user is in the database
            var mutedUser = db.MutedUsers.GetMutedUser(eventArgs.Guild, eventArgs.Member);
            var guildSettings = db.GuildConfig.GetGuild(eventArgs.Guild.Id);

            if (mutedUser is not null && botPerms.HasFlag(Permissions.ManageRoles))
            {
                if (eventArgs.Guild.Roles.TryGetValue(guildSettings.MuteRoleId, out var muteRole))
                {
                    // If mute role exists, apply to the user
                    muteRole = eventArgs.Guild.GetRole(guildSettings.MuteRoleId);
                    await eventArgs.Member.GrantRoleAsync(muteRole);
                }
                else
                {
                    // If mute role doesn't exist anymore, delete all registers of it
                    var toDelete = db.MutedUsers.Table
                        .Where(x => x.GuildIdFK == eventArgs.Guild.Id)
                        .ToArray();

                    db.MutedUsers.DeleteRange(toDelete);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}