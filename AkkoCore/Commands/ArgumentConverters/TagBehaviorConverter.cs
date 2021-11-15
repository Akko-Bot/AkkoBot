using AkkoCore.Services.Database.Enums;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoCore.Commands.ArgumentConverters;

internal sealed class TagBehaviorConverter : IArgumentConverter<TagBehavior>
{
    public Task<Optional<TagBehavior>> ConvertAsync(string input, CommandContext ctx)
    {
        return input.ToLowerInvariant() switch
        {
            //"none" => Task.FromResult(Optional.FromValue(TagBehavior.None)),
            "delete" => Task.FromResult(Optional.FromValue(TagBehavior.Delete)),
            "anywhere" => Task.FromResult(Optional.FromValue(TagBehavior.Anywhere)),
            "directmessage" or "direct message" or "dm" => Task.FromResult(Optional.FromValue(TagBehavior.DirectMessage)),
            "sanitizeroleping" or "sanitizerole" or "sanitize role" or "sr" => Task.FromResult(Optional.FromValue(TagBehavior.SanitizeRolePing)),
            _ => Task.FromResult(Optional.FromNoValue<TagBehavior>())
        };
    }
}