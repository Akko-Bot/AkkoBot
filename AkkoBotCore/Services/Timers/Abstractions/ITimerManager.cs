using System;
using AkkoBot.Services.Database.Entities;
using DSharpPlus;

namespace AkkoBot.Services.Timers.Abstractions
{
    /// <summary>
    /// Represents an object that initializes timer objects from the database and manages
    /// their execution and lifetime.
    /// </summary>
    public interface ITimerManager : IDisposable
    {
        /// <summary>
        /// Generates an <see cref="IAkkoTimer"/> based on a database entry and adds it to the cache.
        /// </summary>
        /// <param name="client">The Discord client that fetched the database entry.</param>
        /// <param name="entity">The database entry.</param>
        /// <remarks>
        /// This method ensures that only timers triggering within the next 
        /// few days get initialized and added to the cache.
        /// </remarks>
        /// <returns><see langword="true"/> if the timer was generated and added, <see langword="false"/> otherwise.</returns>
        bool AddOrUpdateByEntity(DiscordClient client, TimerEntity entity);

        /// <summary>
        /// Attempts to add the specified <see cref="IAkkoTimer"/> to the cache.
        /// </summary>
        /// <param name="timer">The timer to be added.</param>
        /// <returns><see langword="true"/> if it is successfully added, <see langword="false"/> otherwise.</returns>
        bool TryAdd(IAkkoTimer timer);

        /// <summary>
        /// Attempts to get the <see cref="IAkkoTimer"/> associated with a certain ID.
        /// </summary>
        /// <param name="id">The ID of the <see cref="IAkkoTimer"/>.</param>
        /// <param name="timer">An <see cref="IAkkoTimer"/> if the timer is found, <see langword="null"/> otherwise.</param>
        /// <returns><see langword="true"/> if the timer is found, <see langword="false"/> otherwise.</returns>
        bool TryGetValue(int id, out IAkkoTimer timer);

        /// <summary>
        /// Attempts to remove the specified <see cref="IAkkoTimer"/> from the cache.
        /// </summary>
        /// <param name="timer">The timer to be removed.</param>
        /// <returns><see langword="true"/> if it is successfully removed, <see langword="false"/> otherwise.</returns>
        bool TryRemove(IAkkoTimer timer);

        /// <summary>
        /// Attempts to remove an <see cref="IAkkoTimer"/> with the specified ID.
        /// </summary>
        /// <param name="id">The ID of the timer to be removed.</param>
        /// <returns><see langword="true"/> if it gets successfully removed, <see langword="false"/> otherwise.</returns>
        bool TryRemove(int id);

        /// <summary>
        /// Attempts to update an <see cref="IAkkoTimer"/> object stored in the cache.
        /// </summary>
        /// <param name="timer">The timer to replace the instance present in the cache.</param>
        /// <returns><see langword="true"/> if it gets successfully updated, <see langword="false"/> otherwise.</returns>
        bool TryUpdate(IAkkoTimer timer);
    }
}