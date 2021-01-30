using System.Threading.Tasks;
using AkkoBot.Command.Attributes;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using DSharpPlus.CommandsNext;

namespace AkkoBot.Command.Abstractions
{
    [IsNotBot, IsNotBlacklisted]
    public abstract class AkkoCommandModule : BaseCommandModule
    {
        public override async Task BeforeExecutionAsync(CommandContext context)
        {
            // Save or update the user who ran the command
            // This might be a scale bottleneck in the future
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);
            await db.DiscordUsers.CreateOrUpdateAsync(context.User);
        }
    }
}