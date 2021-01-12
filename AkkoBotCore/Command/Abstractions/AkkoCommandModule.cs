using System.Threading.Tasks;
using AkkoBot.Command.Attributes;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Entities;
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

        // public override Task BeforeExecutionAsync(CommandContext context)
        // {
        //     // Save or update the user who ran the command
        //     var db = context.CommandsNext.Services.GetService<AkkoUnitOfWork>();
        //     db.DiscordUsers.CreateOrUpdate(context.User);

        //     return Task.CompletedTask;
        // }

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