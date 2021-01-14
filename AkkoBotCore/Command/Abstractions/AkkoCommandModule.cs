using System.Threading.Tasks;
using AkkoBot.Command.Attributes;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Command.Abstractions
{
    [IsNotBot, IsNotBlacklisted]
    public abstract class AkkoCommandModule : BaseCommandModule
    {
        // ReplyLocalizedAsync
        // ErrorLocalizedAsync

        public async override Task BeforeExecutionAsync(CommandContext context)
        {
            // Save or update the user who ran the command
            // This might be a scale bottleneck in the future
            var db = context.CommandsNext.Services.GetService<IUnitOfWork>();
            await db.DiscordUsers.CreateOrUpdateAsync(context.User);
        }

        /*public override Task AfterExecutionAsync(CommandContext context)
        {
            context.Client.Logger.BeginScope(context);

            context.Client.Logger.LogInformation(
                new EventId(LoggerEvents.WebSocketReceive.Id, "Command"),
                context.Message.Content
            );

            return Task.CompletedTask;
        }*/
    }
}