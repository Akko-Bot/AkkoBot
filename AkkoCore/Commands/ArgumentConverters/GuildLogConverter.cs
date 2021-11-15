using AkkoCore.Services.Database.Enums;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoCore.Commands.ArgumentConverters;

internal sealed class GuildLogConverter : IArgumentConverter<GuildLogType>
{
    public Task<Optional<GuildLogType>> ConvertAsync(string input, CommandContext ctx)
    {
        return input?.ToLowerInvariant() switch
        {
            //"unknown" => Task.FromResult(Optional.FromValue(GuildLog.Unknown)),   // This event sends no guild-related data
            "channelevents" or "channel" or "channels" => Task.FromResult(Optional.FromValue(GuildLogType.ChannelEvents)),
            "banevents" or "ban" => Task.FromResult(Optional.FromValue(GuildLogType.BanEvents)),
            "memberevents" or "member" or "members" => Task.FromResult(Optional.FromValue(GuildLogType.MemberEvents)),
            "messageevents" or "message" or "messages" => Task.FromResult(Optional.FromValue(GuildLogType.MessageEvents)),
            "voiceevents" or "voice" => Task.FromResult(Optional.FromValue(GuildLogType.VoiceEvents)),
            "roleevents" or "role" or "roles" => Task.FromResult(Optional.FromValue(GuildLogType.RoleEvents)),
            "inviteevents" or "invite" or "invites" => Task.FromResult(Optional.FromValue(GuildLogType.InviteEvents)),
            "emojievents" or "emoji" or "emojis" => Task.FromResult(Optional.FromValue(GuildLogType.EmojiEvents)),
            //"userpresence" or "presence" => Task.FromResult(Optional.FromValue(GuildLog.UserPresence)),   // Status changes offer nothing of value
            _ => Task.FromResult(Optional.FromNoValue<GuildLogType>())
        };
    }
}