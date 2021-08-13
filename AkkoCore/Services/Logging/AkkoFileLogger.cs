using AkkoCore.Common;
using AkkoCore.Services.Logging.Abstractions;
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace AkkoCore.Services.Logging
{
    /// <summary>
    /// Writes logs to a file once a certain threshold is reached.
    /// </summary>
    public class AkkoFileLogger : IFileLogger
    {
        private const double _mb = 1000000.0;    // Byte to Megabyte ratio
        private DateTimeOffset _time = DateTimeOffset.Now;
        private readonly MemoryStream _logStream = new();
        private readonly string _directory = AkkoEnvironment.LogsDirectory;
        public string TimeStampFormat { get; set; }
        public double FileSizeLimitMB { get; set; }
        public bool IsDisposed { get; private set; } = false;

        public AkkoFileLogger(double fileSizeLimit = 1.0, string timeFormat = null)
        {
            FileSizeLimitMB = (fileSizeLimit <= 0.0) ? 1.0 : fileSizeLimit;
            TimeStampFormat = (string.IsNullOrWhiteSpace(timeFormat)) ? "dd-MM-yyyy_HH-mm_fffff" : timeFormat;
        }

        public void CacheLogging(string logEntry)
        {
            _logStream.Write(Encoding.UTF8.GetBytes(logEntry));

            if (_logStream.Length / _mb > FileSizeLimitMB || DateTimeOffset.Now.Subtract(_time) > TimeSpan.FromDays(1))
                DumpToFile();
        }

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