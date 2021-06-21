using AkkoBot.Commands.Common;
using AkkoBot.Commands.Modules.Utilities.Services;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Events.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoBot.Services.Events
{
    /// <summary>
    /// Handles events related to gatekeeping.
    /// </summary>
    public class GatekeepingEventHandler : IGatekeepingEventHandler
    {
        private readonly IDbCache _dbCache;
        private readonly UtilitiesService _utilitiesService;

        public GatekeepingEventHandler(IDbCache dbCache, UtilitiesService utilitiesService)
        {
            _dbCache = dbCache;
            _utilitiesService = utilitiesService;
        }

        public async Task SanitizeNameOnUpdateAsync(DiscordClient _, GuildMemberUpdateEventArgs eventArgs)
        {
            if (!_dbCache.Gatekeeping.TryGetValue(eventArgs.Guild.Id, out var gatekeeper) || !gatekeeper.SanitizeNames
                || (!char.IsPunctuation(eventArgs.Member.DisplayName[0]) && !char.IsSymbol(eventArgs.Member.DisplayName[0]))
                || !eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.ManageNicknames)))
                return;

            await eventArgs.Member.ModifyAsync(user =>
                    user.Nickname = (string.IsNullOrWhiteSpace(gatekeeper.CustomSanitizedName))
                        ? EnsureNameSanitization(eventArgs.Member)
                        : gatekeeper.CustomSanitizedName
            );
        }

        public async Task SanitizeNameOnJoinAsync(DiscordClient _, GuildMemberAddEventArgs eventArgs)
        {
            var dbGuild = await _dbCache.GetDbGuildAsync(eventArgs.Guild.Id);

            if (!_dbCache.Gatekeeping.TryGetValue(eventArgs.Guild.Id, out var gatekeeper) || !gatekeeper.SanitizeNames
                || (!char.IsPunctuation(eventArgs.Member.DisplayName[0]) && !char.IsSymbol(eventArgs.Member.DisplayName[0]))
                || !eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.ManageNicknames)))
                return;

            await eventArgs.Member.ModifyAsync(user =>
                    user.Nickname = (string.IsNullOrWhiteSpace(gatekeeper.CustomSanitizedName))
                        ? EnsureNameSanitization(eventArgs.Member)
                        : gatekeeper.CustomSanitizedName
            );
        }

        public async Task SendGreetDmMessageAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs)
        {
            if (!_dbCache.Gatekeeping.TryGetValue(eventArgs.Guild.Id, out var gatekeeper)
                || !gatekeeper.GreetDm || eventArgs.Member.IsBot || string.IsNullOrWhiteSpace(gatekeeper.GreetMessage))
                return;

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);
            eventArgs.Guild.Channels.TryGetValue(gatekeeper.GreetChannelId ?? default, out var channel);
            
            channel ??= eventArgs.Guild.GetDefaultChannel();
            var cmdHandler = client.GetCommandsNext();
            var fakeContext = cmdHandler.CreateFakeContext(eventArgs.Member, channel, gatekeeper.GreetMessage, dbGuild.Prefix, null);
            var parsedString = new SmartString(fakeContext, gatekeeper.GreetMessage);

            if (_utilitiesService.DeserializeEmbed(parsedString, out var message))
                await eventArgs.Member.SendMessageSafelyAsync(message);
            else
                await eventArgs.Member.SendMessageSafelyAsync(parsedString);
        }

        public async Task SendGreetMessageAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs)
        {
            if (!_dbCache.Gatekeeping.TryGetValue(eventArgs.Guild.Id, out var gatekeeper)
                || !gatekeeper.GreetChannelId.HasValue || gatekeeper.GreetDm || eventArgs.Member.IsBot
                || string.IsNullOrWhiteSpace(gatekeeper.GreetMessage)
                || !eventArgs.Guild.Channels.TryGetValue(gatekeeper.GreetChannelId.Value, out var channel)
                || !eventArgs.Guild.CurrentMember.PermissionsIn(channel).HasPermission(Permissions.AccessChannels | Permissions.SendMessages))
                return;

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var cmdHandler = client.GetCommandsNext();
            var fakeContext = cmdHandler.CreateFakeContext(eventArgs.Member, channel, gatekeeper.GreetMessage, dbGuild.Prefix, null);
            var parsedString = new SmartString(fakeContext, gatekeeper.GreetMessage);

            if (_utilitiesService.DeserializeEmbed(parsedString, out var message))
                await channel.SendMessageAsync(message);
            else
                await channel.SendMessageAsync(parsedString);
        }

        public async Task SendFarewellMessageAsync(DiscordClient client, GuildMemberRemoveEventArgs eventArgs)
        {
            if (!_dbCache.Gatekeeping.TryGetValue(eventArgs.Guild.Id, out var gatekeeper)
                || !gatekeeper.FarewellChannelId.HasValue || eventArgs.Member.IsBot
                || string.IsNullOrWhiteSpace(gatekeeper.FarewellMessage)
                || !eventArgs.Guild.Channels.TryGetValue(gatekeeper.FarewellChannelId.Value, out var channel)
                || !eventArgs.Guild.CurrentMember.PermissionsIn(channel).HasPermission(Permissions.AccessChannels | Permissions.SendMessages))
                return;

            _dbCache.Guilds.TryGetValue(eventArgs.Guild.Id, out var dbGuild);

            var cmdHandler = client.GetCommandsNext();
            var fakeContext = cmdHandler.CreateFakeContext(eventArgs.Member, channel, gatekeeper.FarewellMessage, dbGuild.Prefix, null);
            var parsedString = new SmartString(fakeContext, gatekeeper.FarewellMessage);

            if (_utilitiesService.DeserializeEmbed(parsedString, out var message))
                await channel.SendMessageAsync(message);
            else
                await channel.SendMessageAsync(parsedString);
        }

        /// <summary>
        /// Returns a valid display name for a user.
        /// </summary>
        /// <param name="user">The user to have its name sanitized.</param>
        /// <returns>
        /// The sanitized display name or "No Symbols Allowed" if their nickname AND username
        /// are comprised of special characters only.
        /// </returns>
        private string EnsureNameSanitization(DiscordMember user)
        {
            var result = user.DisplayName.SanitizeUsername();

            return (string.IsNullOrWhiteSpace(result))
                ? (user.Username.All(x => char.IsPunctuation(x) || char.IsSymbol(x))) ? "No Symbols Allowed" : user.Username.SanitizeUsername()
                : result;
        }
    }
}
