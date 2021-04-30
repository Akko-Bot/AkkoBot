﻿using System.Globalization;
using AkkoBot.Common;
using AkkoBot.Services.Logging.Abstractions;
using System;
using System.IO;
using System.Text;

namespace AkkoBot.Services.Logging
{
    /// <summary>
    /// Writes logs to a file once a certain threshold is reached.
    /// </summary>
    public class AkkoFileLogger : IFileLogger
    {
        private const double MB = 1000000.0;    // Byte to Megabyte ratio
        private DateTimeOffset _time = DateTimeOffset.Now;
        private readonly MemoryStream _logStream = new();
        private readonly string _directory = AkkoEnvironment.LogsDirectory;
        public string TimeStampFormat { get; set; }
        public double FileSizeLimitMB { get; set; }

        /// <summary>
        /// Indicates whether this object has been disposed or not.
        /// </summary>
        public bool IsDisposed { get; private set; } = false;

        public AkkoFileLogger(double fileSizeLimit = 1.0, string timeFormat = null)
        {
            FileSizeLimitMB = (fileSizeLimit <= 0.0) ? 1.0 : fileSizeLimit;
            TimeStampFormat = (string.IsNullOrWhiteSpace(timeFormat)) ? "dd-MM-yyyy_HH-mm_fffff" : timeFormat;
        }

        /// <summary>
        /// Stores <paramref name="logEntry"/> into a <see cref="MemoryStream"/>.
        /// </summary>
        /// <remarks>
        /// If the addition of the log causes the stream to exceed its max threshold, it dumps
        /// its content to a text file and the stream is set back to zero.
        /// </remarks>
        /// <param name="logEntry">The log entry to be cached.</param>
        public void CacheLogging(string logEntry)
        {
            var encodedEntry = Encoding.UTF8.GetBytes(logEntry);
            _logStream.Write(encodedEntry);

            if (_logStream.Length / MB > FileSizeLimitMB || DateTimeOffset.Now.Subtract(_time) > TimeSpan.FromDays(1))
                DumpToFile();
        }

        /// <summary>
        /// Writes the log stream to a text file and resets the stream.
        /// </summary>
        public void DumpToFile()
        {
            if (IsDisposed || _logStream.Length is 0)
                return;

            if (!Directory.Exists(_directory))
                Directory.CreateDirectory(_directory);

            using var fileStream = new FileStream(
                _directory + $"AkkoLog_{_time.ToString(TimeStampFormat, CultureInfo.InvariantCulture)}.txt",
                FileMode.Create, FileAccess.ReadWrite, FileShare.Read,
                bufferSize: 4096, useAsync: true
            );

            _logStream.WriteTo(fileStream);

            _logStream.SetLength(0);
            _time = DateTimeOffset.Now;
        }

        /// <summary>
        /// Releases the allocated resources for this file logger.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (!IsDisposed)
            {
                if (isDisposing)
                {
                    DumpToFile();
                    _logStream?.Dispose();
                }

                IsDisposed = true;
            }
        }
    }
}