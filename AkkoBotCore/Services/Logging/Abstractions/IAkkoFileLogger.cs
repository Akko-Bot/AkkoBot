using System;

namespace AkkoBot.Services.Logging.Abstractions
{
    public interface IAkkoFileLogger : IDisposable
    {
        bool IsDisposed { get; }
        void CacheLogging(string logEntry);
        void DumpToFile();
    }
}
