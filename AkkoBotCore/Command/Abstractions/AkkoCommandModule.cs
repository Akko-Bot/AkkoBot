using System.Threading.Tasks;
using AkkoBot.Command.Attributes;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Command.Abstractions
{
    [DbBlacklist]
    public abstract class AkkoCommandModule : BaseCommandModule
    {
        // ReplyLocalizedAsync
        // ErrorLocalizedAsync

        public override Task AfterExecutionAsync(CommandContext context)
        {
            context.Client.Logger.BeginScope(context);

            context.Client.Logger.LogInformation(
                new EventId(LoggerEvents.WebSocketReceive.Id, "Command"),
                context.Message.Content
            );

            return Task.CompletedTask;
        }
    }
}