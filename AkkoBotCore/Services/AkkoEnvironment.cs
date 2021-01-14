using System;
using System.IO;

namespace AkkoBot.Services
{
    public static class AkkoEnvironment
    {
        /// <summary>
        /// Gets the platform specific character for separating directories in the local path.
        /// </summary>
        public static char OsSlash { get; } = Path.DirectorySeparatorChar;

        /// <summary>
        /// Gets the fully qualified path for the current directory of the application.
        /// </summary>
        public static string CurrentDirectory { get; } = AppDomain.CurrentDomain.BaseDirectory + OsSlash;

        /// <summary>
        /// Gets the fully qualified path for the directory where the credentials file is stored.
        /// </summary>
        public static string CredsDirectory { get; } = CurrentDirectory + "Creds" + OsSlash;

        /// <summary>
        /// Gets the fully qualified path for the directory where the log files are stored.
        /// </summary>
        public static string LogsDirectory { get; } = CurrentDirectory + "Logs" + OsSlash;

        /// <summary>
        /// Gets the fully qualified path for the credentials file.
        /// </summary>
        public static string CredentialsPath { get; } = CredsDirectory + "credentials.yaml";

        public static string GetFileDirectoryPath(string filePath)
        {
            return filePath.Substring(0, filePath.LastIndexOf(OsSlash));
        }

        public static string GetRelativeAkkoPath(string path)
        {
            return "." + OsSlash + Path.GetRelativePath(CurrentDirectory, path);
        }
    }
}
