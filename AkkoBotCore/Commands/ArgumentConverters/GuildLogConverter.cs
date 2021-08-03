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
                "unknown" => Task.FromResult(Optional.FromValue(GuildLog.Unknown)),
                "channel" => Task.FromResult(Optional.FromValue(GuildLog.ChannelEvents)),
                "ban" => Task.FromResult(Optional.FromValue(GuildLog.BanEvents)),
                "member" => Task.FromResult(Optional.FromValue(GuildLog.MemberEvents)),
                "message" => Task.FromResult(Optional.FromValue(GuildLog.MessageEvents)),
                "voice" => Task.FromResult(Optional.FromValue(GuildLog.VoiceEvents)),
                "role" => Task.FromResult(Optional.FromValue(GuildLog.RoleEvents)),
                "invite" => Task.FromResult(Optional.FromValue(GuildLog.InviteEvents)),
                "integration" => Task.FromResult(Optional.FromValue(GuildLog.Integration)),
                "emoji" or "emojis" => Task.FromResult(Optional.FromValue(GuildLog.Emojis)),
                "userpresence" or "presence" => Task.FromResult(Optional.FromValue(GuildLog.UserPresence)),
                _ => Task.FromResult(Optional.FromNoValue<GuildLog>())
            };
        }
    }
}
