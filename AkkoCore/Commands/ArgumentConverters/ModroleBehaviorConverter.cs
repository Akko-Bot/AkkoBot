using AkkoCore.Services.Database.Enums;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace AkkoCore.Commands.ArgumentConverters;

internal sealed class ModroleBehaviorConverter : IArgumentConverter<ModroleBehavior>
{
    public Task<Optional<ModroleBehavior>> ConvertAsync(string input, CommandContext ctx)
    {
        // Determine the appropriate type specified by the user
        return input?.ToLowerInvariant() switch
        {
            "enforcehierarchy" or "hierarchy" => Task.FromResult(Optional.FromValue(ModroleBehavior.EnforceHierarchy)),
            "exclusive" => Task.FromResult(Optional.FromValue(ModroleBehavior.Exclusive)),
            _ => Task.FromResult(Optional.FromNoValue<ModroleBehavior>())
        };
    }
}