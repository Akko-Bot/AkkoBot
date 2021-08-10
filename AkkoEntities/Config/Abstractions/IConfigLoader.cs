namespace AkkoEntities.Config.Abstractions
{
    public interface IConfigLoader
    {
        /// <summary>
        /// Gets a <typeparamref name="T"/> from the specified path.
        /// </summary>
        /// <typeparam name="T">The type of the configuration object.</typeparam>
        /// <param name="filePath">Path to the configuration file, with its name and extension.</param>
        /// <remarks>If the file doesn't exist, it creates one.</remarks>
        /// <returns>A <typeparamref name="T"/> object.</returns>
        T LoadConfig<T>(string filePath) where T : new();

        /// <summary>
        /// Prepare the credentials for bot startup.
        /// </summary>
        /// <param name="filePath">Path to the credentials file, with its name and extension.</param>
        /// <remarks>This method loops until a valid credentials file is provided.</remarks>
        /// <returns>A valid <see cref="Credentials"/> object.</returns>
        Credentials LoadCredentials(string filePath);

        /// <summary>
        /// Serializes a <typeparamref name="T"/> object to a file in the specified path.
        /// </summary>
        /// <typeparam name="T">The type of the object being serialized.</typeparam>
        /// <param name="config">The object to be serialized.</param>
        /// <param name="filePath">Path to the configuration file, with its name and extension.</param>
        void SaveConfig<T>(T config, string filePath);
    }
}