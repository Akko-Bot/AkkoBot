using AkkoBot.Common;
using AkkoCore.Config;
using AkkoCore.Config.Abstractions;
using AkkoCore.Extensions;
using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AkkoBot.Core.Common
{
    /// <summary>
    /// Saves and loads Yaml configuration files.
    /// </summary>
    public class ConfigLoader : IConfigLoader
    {
        private readonly ISerializer _serializer;
        private readonly IDeserializer _deserializer;

        public ConfigLoader(ISerializer serializer = default, IDeserializer deserializer = default)
        {
            _serializer = serializer ?? new SerializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
            _deserializer = deserializer ?? new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance).Build();
        }

        public Credentials LoadCredentials(string filePath)
        {
            while (!IsValidCredential(filePath)) ;

            return GetCredentials(filePath);
        }

        public T LoadConfig<T>(string filePath) where T : new()
        {
            // Load the config file
            if (!File.Exists(filePath))
                return CreateConfigFile<T>(filePath);

            using var reader = new StreamReader(File.OpenRead(filePath));
            return reader.FromYaml<T>(_deserializer);
        }

        public void SaveConfig<T>(T config, string filePath)
        {
            using var writer = File.CreateText(filePath);
            config.ToYaml(writer, _serializer);
        }

        /// <summary>
        /// Serializes a <typeparamref name="T"/> object to a Yaml file in the specified path.
        /// </summary>
        /// <typeparam name="T">The data type to be serialized.</typeparam>
        /// <param name="filePath">Path to the credentials file, with its name and extension.</param>
        /// <returns>The <typeparamref name="T"/> that has been serialized.</returns>
        private T CreateConfigFile<T>(string filePath) where T : new()
        {
            // Ensure the folder exists
            if (!Directory.Exists(AkkoEnvironment.GetFileDirectoryPath(filePath)))
                Directory.CreateDirectory(AkkoEnvironment.GetFileDirectoryPath(filePath));

            // Serialize to a file
            var result = new T();
            using var writer = File.CreateText(filePath);
            result.ToYaml(writer, _serializer);

            return result;
        }

        /// <summary>
        /// Checks if the credentials file is remotely valid.
        /// </summary>
        /// <param name="filePath">Path to the credentials file, with its name and extension.</param>
        /// <returns><see langword="true"/> if it seems to be valid, <see langword="false"/> otherwise.</returns>
        private bool IsValidCredential(string filePath)
        {
            // Open the file and deserialize it.
            var creds = GetCredentials(filePath);

            // Check if token and database password are remotely valid.
            if (creds.Token.Length < 50)
            {
                PauseProgram(
                    "Your token is probably invalid." + Environment.NewLine +
                    "Please, add a valid token and"
                );

                return false;
            }
            else if (string.IsNullOrWhiteSpace(creds.Database["password"]) || creds.Database["password"].Equals("postgres_password_here"))
            {
                PauseProgram(
                    "You forgot to set your database password." + Environment.NewLine +
                    "Please, add it and"
                );

                return false;
            }

            return true;
        }

        /// <summary>
        /// Ensures the credentials file exists and deserializes it into a <see cref="Credentials"/> object.
        /// </summary>
        /// <param name="filePath">Path to the credentials file, with its name and extension.</param>
        /// <returns>A <see cref="Credentials"/> object.</returns>
        private Credentials GetCredentials(string filePath)
        {
            // If directory or file don't exist, return false
            if (!Directory.Exists(AkkoEnvironment.GetFileDirectoryPath(filePath)) || !File.Exists(filePath))
            {
                CreateConfigFile<Credentials>(filePath);

                PauseProgram(
                    @"A credentials file has been generated for you at " +
                    AkkoEnvironment.CredsPath + Environment.NewLine +
                    @"Please, add your data to it and"
                );
            }

            // Open the file and deserialize it.
            using var reader = new StreamReader(File.OpenRead(filePath));
            return reader.FromYaml<Credentials>(_deserializer);
        }

        /// <summary>
        /// Pauses the program for the user to read the <paramref name="message"/>.
        /// </summary>
        /// <param name="message">A message to be displayed to the user.</param>
        private void PauseProgram(string message)
        {
            Console.WriteLine(message + " press Enter when you are ready.");
            Console.ReadLine();
        }
    }
}