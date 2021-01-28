using System;
using System.Collections.Generic;

namespace AkkoBot.Services.Database.Abstractions
{
    /// <summary>
    /// Represents a database table that holds information about how a message should be sent.
    /// </summary>
    public interface IMessageSettings
    {
        string Locale { get; }
        string OkColor { get; }
        string ErrorColor { get; }
        bool UseEmbed { get; }
        TimeSpan? InteractiveTimeout { get; }
        IReadOnlyDictionary<string, string> GetSettings();
    }
}