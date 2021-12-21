using AkkoCore.Services.Database.Enums;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoCore.Commands.ArgumentConverters;

internal sealed class PunishmentTypeConverter : IArgumentConverter<PunishmentType>
{
    public Task<Optional<PunishmentType>> ConvertAsync(string input, CommandContext ctx)
    {
        // Determine the appropriate type specified by the user
        return input?.ToLowerInvariant() switch
        {
            "mute" => Task.FromResult(Optional.FromValue(PunishmentType.Mute)),
            "k" or "kick" => Task.FromResult(Optional.FromValue(PunishmentType.Kick)),
            "sb" or "softban" => Task.FromResult(Optional.FromValue(PunishmentType.Softban)),
            "b" or "ban" => Task.FromResult(Optional.FromValue(PunishmentType.Ban)),
            "ar" or "addrole" => Task.FromResult(Optional.FromValue(PunishmentType.AddRole)),
            "rr" or "remrole" or "removerole" => Task.FromResult(Optional.FromValue(PunishmentType.RemoveRole)),
            _ => Task.FromResult(Optional.FromNoValue<PunishmentType>())
        };
    }
}