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
        private readonly IUnitOfWork _db;
        private readonly ILocalizer _localizer;
        
        // ReplyLocalizedAsync
        // ErrorLocalizedAsync

        public AkkoCommandModule(IUnitOfWork db, ILocalizer localizer)
        {
            _db = db;
            _localizer = localizer;
        }

        public async Task ReplyLocalizedAsync(CommandContext context, string response)
        {
            var locale = await _db.GuildConfigs.GetLocaleAsync(context.Guild.Id);
            var responseString = _localizer.GetResponseString(locale, response);

            await context.RespondAsync(responseString);
        }

        // public async Task ReplyLocalizedAsync(CommandContext context, params string[] responses)
        // {
        //     var locale = await _db.GuildConfigs.GetLocaleAsync(context.Guild.Id);
        //     var responseString = _localizer.GetResponseStrings(locale, responses);

        //     await context.RespondAsync(responseString);
        // }

        public async override Task BeforeExecutionAsync(CommandContext context)
        {
            // Save or update the user who ran the command
            // This might be a scale bottleneck in the future
            var db = context.CommandsNext.Services.GetService<IUnitOfWork>();
            await db.DiscordUsers.CreateOrUpdateAsync(context.User);
        }
    }
}