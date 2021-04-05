using AkkoBot.Services.Database.Repository;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Services.Database.Abstractions
{
    /// <summary>
    /// Represents an object that centralizes all database operations.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        public DiscordUserRepo DiscordUsers { get; }
        public BlacklistRepo Blacklist { get; }
        public BotConfigRepo BotConfig { get; }
        public LogConfigRepo LogConfig { get; }
        public GuildConfigRepo GuildConfig { get; }
        public TimerRepo Timers { get; }
        public PlayingStatusRepo PlayingStatuses { get; }
        public AliasRepo Aliases { get; }
        public ReminderRepo Reminders { get; }
        public CommandRepo AutoCommands { get; }

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>The number of state entries written to the database.</returns>
        int SaveChanges();

        /// <summary>
        /// Saves all changes made in this context to the database.
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous save operation.
        /// The task result contains the number of state entries written to the database.
        /// </returns>
        Task<int> SaveChangesAsync();
    }
}