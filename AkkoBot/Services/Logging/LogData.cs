using DSharpPlus.CommandsNext;

namespace AkkoBot.Services.Logging
{
    /// <summary>
    /// Wrapper for raw data that needs to be included on logs.
    /// </summary>
    public record LogData(CommandContext Context, string OptionalMessage);
}