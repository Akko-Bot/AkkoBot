using System;

namespace AkkoCore.Abstractions
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
    }
}