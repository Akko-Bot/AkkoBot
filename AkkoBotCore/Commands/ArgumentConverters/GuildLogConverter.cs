using AkkoBot.Services.Database.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Commands.ArgumentConverters
{
    public class GuildLogConverter : IArgumentConverter<GuildLog>
    {
        public Task<Optional<GuildLog>> ConvertAsync(string input, CommandContext ctx)
        {
            return input?.ToLowerInvariant() switch
            {
                //"unknown" => Task.FromResult(Optional.FromValue(GuildLog.Unknown)),   // This event sends no guild-related data
                "channelevents" or "channel" or "channels" => Task.FromResult(Optional.FromValue(GuildLog.ChannelEvents)),
                "banevents" or "ban" => Task.FromResult(Optional.FromValue(GuildLog.BanEvents)),
                "memberevents" or "member" or "members" => Task.FromResult(Optional.FromValue(GuildLog.MemberEvents)),
                "messageevents" or "message" or "messages" => Task.FromResult(Optional.FromValue(GuildLog.MessageEvents)),
                "voiceevents" or "voice" => Task.FromResult(Optional.FromValue(GuildLog.VoiceEvents)),
                "roleevents" or "role" or "roles" => Task.FromResult(Optional.FromValue(GuildLog.RoleEvents)),
                "inviteevents" or "invite" or "invites" => Task.FromResult(Optional.FromValue(GuildLog.InviteEvents)),
                "emojievents" or "emoji" or "emojis" => Task.FromResult(Optional.FromValue(GuildLog.EmojiEvents)),
                //"userpresence" or "presence" => Task.FromResult(Optional.FromValue(GuildLog.UserPresence)),   // Status changes offer nothing of value
                _ => Task.FromResult(Optional.FromNoValue<GuildLog>())
            };
        }
    }
}
