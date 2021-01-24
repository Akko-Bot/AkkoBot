using System.Collections.Generic;
using AkkoBot.Extensions;

namespace AkkoBot.Services.Localization
{
    /// <summary>
    /// A model class for all possible response strings.
    /// </summary>
    public class LocalizedStrings
    {
        public string BlAdded { get; init; } = "Successfully added {0} {1} {2} to the blacklist.";
        public string BlClear { get; init; } = "{0} entries were removed from the blacklist successfully.";
        public string BlEmpty { get; init; } = "There are no entries in the blacklist.";
        public string BlExist { get; init; } = "{0} {1} {2} is blacklisted already.";
        public string BlNotExist { get; init; } = "{0} {1} {2} is not blacklisted.";
        public string BlRemoved { get; init; } = "Successfully removed {0} {1} {2} from the blacklist.";
        public string BlTitle { get; init; } = "Blacklist";
        public string Channel { get; init; } = "Channel";
        public string Days { get; init; } = "Days";
        public string Hours { get; init; } = "Hours";
        public string Id { get; init; } = "Id";
        public string Minutes { get; init; } = "Minutes";
        public string Name { get; init; } = "Name";
        public string OnlineSince { get; init; } = "Online since";
        public string Seconds { get; init; } = "Seconds";
        public string Server { get; init; } = "Server";
        public string Shutdown { get; init; } = "Shutting down.";
        public string Type { get; init; } = "Type";
        public string Unknown { get; init; } = "Unknown";
        public string Unspecified { get; init; } = "Unspecified";
        public string Uptime { get; init; } = "Uptime";
        public string User { get; init; } = "User";
        public string BotSettingsTitle { get; init; } = "Global Settings";
        public string Settings { get; init; } = "Setting";
        public string Value { get; init; } = "Value";
        public string Requires { get; init; } = "Requires";
        public string Subcommands { get; init; } = "Subcommands";
        public string Usage { get; init; } = "Usage";
        public string HelpBotOwner { get; init; } = "Bot Ownership";
        public string HelpAddReactions { get; init; } = "Add Reactions";
        public string HelpAdministrator { get; init; } = "Administrator";
        public string HelpAttachFiles { get; init; } = "Attach Files";
        public string HelpBanMembers { get; init; } = "Ban Members";
        public string HelpChangeNickname { get; init; } = "Change Nickname";
        public string HelpCreateInstantInvite { get; init; } = "Create Invite";
        public string HelpDeafenMembers { get; init; } = "Deafen Members";
        public string HelpKickMembers { get; init; } = "Kick Members";
        public string HelpManageChannels { get; init; } = "Manage Channels";
        public string HelpManageEmojis { get; init; } = "Manage Emojis";
        public string HelpManageGuild { get; init; } = "Manage Server";
        public string HelpManageMessages { get; init; } = "Manage Messages";
        public string HelpManageNicknames { get; init; } = "Manage Nicknames";
        public string HelpManageRoles { get; init; } = "Manage Roles";
        public string HelpManageWebhooks { get; init; } = "Manage Webhooks";
        public string HelpMentionEveryone { get; init; } = "Mention Everyone";
        public string HelpMoveMembers { get; init; } = "Move Members";
        public string HelpMuteMembers { get; init; } = "Mute Members";
        public string HelpPrioritySpeaker { get; init; } = "Priority Speaker";
        public string HelpSendMessages { get; init; } = "Send Messages";
        public string HelpSpeak { get; init; } = "Speak";
        public string HelpStream { get; init; } = "Stream";
        public string HelpUseExternalEmojis { get; init; } = "Use External Emojis";
        public string HelpUseVoice { get; init; } = "Connect";
        public string HelpViewAuditLog { get; init; } = "View Audit Log";
        // start here
        public string CmdBlacklist { get; init; } = "Groups commands related to the bot's blacklist.";
        public string CmdBlacklistAdd { get; init; } = "Adds an entry to the blacklist.";
        public string CmdBlacklistRem { get; init; } = "Removes an entry from the blacklist.";
        public string CmdBlacklistList { get; init; } = "Shows the blacklist. Provide no parameter to show all entry types.";
        public string CmdBlacklistClear { get; init; } = "Clears the blacklist.";
        public string ArgDiscordChannel { get; init; } = "ID or mention to a Discord channel.";
        public string ArgDiscordUser { get; init; } = "ID or mention to a Discord user.";
        public string ArgUlongId { get; init; } = "A valid Discord ID.";
        public string ArgBlType { get; init; } = "The type of the blacklist entry - `user`, `channel` or `server`.";
        public string ErrorNotFound { get; init; } = "Error: the requested response string was not found.";

        /// <summary>
        /// Gets the collection of response strings stored in this object.
        /// </summary>
        /// <returns>
        /// An <see cref="IReadOnlyDictionary{string, string}"/> where the key is the property name in 
        /// snake_case format and the value is the response string.
        /// </returns>
        public IReadOnlyDictionary<string, string> GetStringCollection()
        {
            var props = this.GetType().GetProperties();
            var result = new Dictionary<string, string>(props.Length);

            foreach (var prop in props)
            {
                result.TryAdd(
                    prop.Name.ToSnakeCase(),
                    prop.GetValue(this).ToString()
                );
            }

            return result;
        }
    }
}