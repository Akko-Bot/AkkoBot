using System.Threading.Tasks;
using AkkoBot.Command.Attributes;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
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
    }
}