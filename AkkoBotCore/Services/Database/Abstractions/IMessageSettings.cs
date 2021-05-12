using System;
using System.Collections.Generic;

namespace AkkoBot.Services.Database.Abstractions
{
    /// <summary>
    /// Contains information about how a message should be sent.
    /// </summary>
    public interface IMessageSettings
    {
        /// <summary>
        /// The locale to be used.
        /// </summary>
        string Locale { get; }

        /// <summary>
        /// The embed color to be used.
        /// </summary>
        string OkColor { get; }

        /// <summary>
        /// The embed error color to be used.
        /// </summary>
        string ErrorColor { get; }

        /// <summary>
        /// Determines whether an embed should be used in the response.
        /// </summary>
        bool UseEmbed { get; }

        /// <summary>
        /// Defines the amount of time an interactive command waits for user input.
        /// </summary>
        TimeSpan? InteractiveTimeout { get; }

        /// <summary>
        /// Gets all properties from this table.
        /// </summary>
        /// <remarks><c>Id</c>, <c>DateAdded</c>, relationship properties and collections are removed.</remarks>
        /// <returns>A dictionary of setting name/value pairs.</returns>
        IReadOnlyDictionary<string, string> GetSettings();
    }
}