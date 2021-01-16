using System.Threading.Tasks;
using AkkoBot.Command.Attributes;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.DependencyInjection;

namespace AkkoBot.Command.Abstractions
{
    [IsNotBot, IsNotBlacklisted]
    public abstract class AkkoCommandModule : BaseCommandModule
    {
        public async override Task BeforeExecutionAsync(CommandContext context)
        {
            // Save or update the user who ran the command
            // This might be a scale bottleneck in the future
            using var scope = context.CommandsNext.Services.CreateScope();
            var db = scope.ServiceProvider.GetService<IUnitOfWork>();
            await db.DiscordUsers.CreateOrUpdateAsync(context.User);
        }
    }
}