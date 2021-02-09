using System;
using System.Windows.Input;
using AkkoBot.Command.Abstractions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using AkkoBot.Extensions;

namespace AkkoBot.Services.Timers
{
    /// <summary>
    /// Encapsulates the set of timed actions a Discord user can create.
    /// </summary>
    public class TimerActions : ICommandService
    {
        private readonly IServiceProvider _services;
        private readonly ILocalizer _localizer;

        public TimerActions(IServiceProvider services, ILocalizer localizer)
        {
            _services = services;
            _localizer = localizer;
        }

        /// <summary>
        /// Unbans a user from a Discord server.
        /// </summary>
        /// <param name="entryId">The ID of the database entry.</param>
        /// <param name="server">The Discord server to unban from.</param>
        /// <param name="userId">The ID of the user to be unbanned.</param>
        public async Task Unban(int entryId, DiscordGuild server, ulong userId)
        {
            using var scope = _services.GetScopedService<IUnitOfWork>(out var db);
            
            var settings = db.GuildConfig.GetGuild(server.Id);
            var localizedReason = _localizer.GetResponseString(settings.Locale, "timedban_title");

            // Perform the action
            await server.UnbanMemberAsync(userId, localizedReason);

            // Remove the entry
            var dbEntity = await db.Timers.GetAsync(entryId);
            db.Timers.Delete(dbEntity);

            await db.SaveChangesAsync();
        }
    }
}