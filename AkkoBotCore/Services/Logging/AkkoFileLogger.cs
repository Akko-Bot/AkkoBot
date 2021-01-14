using System;
using System.IO;
using System.Text;
using AkkoBot.Services.Logging.Abstractions;

namespace AkkoBot.Services.Logging
{
    public class AkkoFileLogger : IAkkoFileLogger
    {
        private const double MB = 1000000.0;
        private DateTimeOffset _time = DateTimeOffset.Now;
        private readonly MemoryStream _logStream = new();
        private readonly string _directory = AkkoEnvironment.LogsDirectory;
        private readonly double _fileSizeLimitMB;
        private readonly string _timeStamp;

        public bool IsDisposed { get; private set; } = false;

        public AkkoFileLogger(double fileSizeLimit = 1.0, string timeFormat = null)
        {
            _fileSizeLimitMB = (fileSizeLimit <= 0.0) ? 1.0 : fileSizeLimit;
            _timeStamp = (string.IsNullOrWhiteSpace(timeFormat)) ? "dd-MM-yyyy_HH-mm_fffff" : timeFormat;
        }

        public void CacheLogging(string logEntry)
        {
            var encodedEntry = Encoding.Unicode.GetBytes(logEntry);
            _logStream.Write(encodedEntry);

            if (_logStream.Length / MB > _fileSizeLimitMB || DateTimeOffset.Now.Subtract(_time) > TimeSpan.FromDays(1))
                DumpToFile();
        }

        public void DumpToFile()
        {
            if (!Directory.Exists(_directory))
                Directory.CreateDirectory(_directory);

            using var fileStream = new FileStream(
                _directory + $"AkkoLog_{_time.ToString(_timeStamp)}.txt",
                FileMode.Create, FileAccess.ReadWrite, FileShare.Read,
                bufferSize: 4096, useAsync: true
            );

            _logStream.WriteTo(fileStream);

            _logStream.SetLength(0);
            _time = DateTimeOffset.Now;
        }

        public void Dispose()
        {
            if (IsDisposed)
                return;

            _logStream.Dispose();
            IsDisposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
