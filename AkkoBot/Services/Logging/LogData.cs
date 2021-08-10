using DSharpPlus.CommandsNext;

namespace AkkoBot.Services.Logging
{
    /// <summary>
    /// Wrapper for raw data that needs to be included on logs.
    /// </summary>
    public class LogData
    {
        public CommandContext Context { get; }
        public string OptionalMessage { get; }

        public LogData(CommandContext context, string optionalMessage)
        {
            Context = context;
            OptionalMessage = optionalMessage;
        }
    }
}