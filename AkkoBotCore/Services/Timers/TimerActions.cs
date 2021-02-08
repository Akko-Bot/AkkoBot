using System.Linq;
using AkkoBot.Command.Abstractions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.Entities;
using System.Threading.Tasks;

namespace AkkoBot.Services.Timers
{
    /// <summary>
    /// Encapsulates the set of timed actions a Discord user can create.
    /// </summary>
    public class TimerActions : ICommandService
    {
        private readonly IUnitOfWork _db;
        private readonly ILocalizer _localizer;

        public TimerActions(IUnitOfWork db, ILocalizer localizer)
        {
            _db = db;
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
            var settings = _db.GuildConfig.GetGuild(server.Id);
            var localizedReason = _localizer.GetResponseString(settings.Locale, "timedban_title");

            // Perform the action
            await server.UnbanMemberAsync(userId, localizedReason);

            // Remove the entry
            var dbEntity = await _db.Timers.GetAsync(entryId);
            _db.Timers.Delete(dbEntity);

            await _db.SaveChangesAsync();
        }
    }
}