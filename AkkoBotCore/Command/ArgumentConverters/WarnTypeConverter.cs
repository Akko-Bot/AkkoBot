using AkkoBot.Services.Database.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Command.ArgumentConverters
{
    public class WarnTypeConverter : IArgumentConverter<WarnPunishType>
    {
        public Task<Optional<WarnPunishType>> ConvertAsync(string input, CommandContext ctx)
        {
            // Determine the appropriate type specified by the user
            return input?.ToLowerInvariant() switch
            {
                "mute" => Task.FromResult(Optional.FromValue(WarnPunishType.Mute)),
                "k" or "kick" => Task.FromResult(Optional.FromValue(WarnPunishType.Kick)),
                "sb" or "softban" => Task.FromResult(Optional.FromValue(WarnPunishType.Softban)),
                "b" or "ban" => Task.FromResult(Optional.FromValue(WarnPunishType.Ban)),
                "ar" or "addrole" => Task.FromResult(Optional.FromValue(WarnPunishType.AddRole)),
                "rr" or "remrole" or "removerole" => Task.FromResult(Optional.FromValue(WarnPunishType.RemoveRole)),
                _ => Task.FromResult(Optional.FromNoValue<WarnPunishType>())
            };
        }
    }
}