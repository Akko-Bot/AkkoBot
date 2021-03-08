using System;
using System.IO;

namespace AkkoBot.Services.Logging.Abstractions
{
    public interface IFileLogger : IDisposable
    {
        /// <summary>
        /// Indicates whether this object has been disposed or not.
        /// </summary>
        bool IsDisposed { get; }

        /// <summary>
        /// Stores <paramref name="logEntry"/> into a <see cref="MemoryStream"/>.
        /// </summary>
        /// <remarks>
        /// If the addition of the log causes the stream to exceed its max threshold, it dumps
        /// its content to a text file and the stream is set back to zero.
        /// </remarks>
        /// <param name="logEntry">The log entry to be cached.</param>
        void CacheLogging(string logEntry);

        /// <summary>
        /// Writes the log stream to a text file and resets the stream.
        /// </summary>
        void DumpToFile();
    }
}