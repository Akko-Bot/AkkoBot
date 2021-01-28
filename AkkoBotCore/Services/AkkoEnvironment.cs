﻿using System;
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
        public static string CurrentDirectory { get; }
            = (AppDomain.CurrentDomain.BaseDirectory.Contains("bin", StringComparison.InvariantCultureIgnoreCase)
            && !AppDomain.CurrentDomain.BaseDirectory.Contains("publish"))
                ? Directory.GetParent(Environment.CurrentDirectory).FullName + OsSlash
                : AppDomain.CurrentDomain.BaseDirectory + OsSlash;

        /// <summary>
        /// Gets the fully qualified path for the directory where the credentials file is stored.
        /// </summary>
        public static string CredsDirectory { get; } = CurrentDirectory + "Creds" + OsSlash;

        /// <summary>
        /// Gets the fully qualified path for the directory where the log files are stored.
        /// </summary>
        public static string LogsDirectory { get; } = CurrentDirectory + "Logs" + OsSlash;

        /// <summary>
        /// Gets the fully qualified path for the directory where the cog files are stored.
        /// </summary>
        public static string CogsDirectory { get; } = CurrentDirectory + "Cogs" + OsSlash;

        /// <summary>
        /// Gets the fully qualified path for the directory where the translated response strings are stored.
        /// </summary>
        public static string LocalesDirectory { get; } = CurrentDirectory + "Localization" + OsSlash;

        /// <summary>
        /// Gets the fully qualified path for the credentials file.
        /// </summary>
        public static string CredsPath { get; } = CredsDirectory + "credentials.yaml";

        /// <summary>
        /// Gets either the absolute or relative path for <paramref name="filePath"/>.
        /// </summary>
        /// <param name="filePath">Relative or absolute directory path to the file.</param>
        /// <remarks><paramref name="filePath"/> must contain at least one <see cref="Path.DirectorySeparatorChar"/>.</remarks>
        /// <returns>The directory path of the specified file.</returns>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public static string GetFileDirectoryPath(string filePath)
            => filePath.Substring(0, filePath.LastIndexOf(OsSlash) + 1);

        /// <summary>
        /// Gets the relative path from <see cref="CurrentDirectory"/> to <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The specified path.</param>
        /// <returns>The relative path to <paramref name="path"/>.</returns>
        public static string GetRelativeAkkoPath(string path)
            => Path.GetRelativePath(CurrentDirectory, path);
    }
}
