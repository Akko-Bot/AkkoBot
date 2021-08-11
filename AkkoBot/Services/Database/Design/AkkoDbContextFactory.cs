using AkkoBot.Common;
using AkkoDatabase;
using AkkoCore.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;
using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AkkoBot.Services.Database.Design
{
    /// <summary>
    /// This class is only used at design time, when EF Core is asked to perform a migration.
    /// </summary>
    public class AkkoDbContextFactory : IDesignTimeDbContextFactory<AkkoDbContext>
    {
        private readonly IDeserializer _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        public AkkoDbContext CreateDbContext(string[] args)
        {
            var creds = LoadCredentials(AkkoEnvironment.CredsPath);

            var options = new DbContextOptionsBuilder<AkkoDbContext>()
                .UseSnakeCaseNamingConvention()
                .UseNpgsql(
                        @"Server=127.0.0.1;" +
                        @"Port=5432;" +
                        @"Database=AkkoBotDb;" +
                        $"User Id={creds.Database["role"]};" +
                        $"Password={creds.Database["password"]};" +
                        @"CommandTimeout=20;",
                    x => x.MigrationsAssembly(Assembly.GetExecutingAssembly().FullName)
                ).Options;

            return new AkkoDbContext(options);
        }

        private Credentials LoadCredentials(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"It was not possible to locate the credentials file at {filePath}.");

            // Open the file and deserialize it.
            using var reader = new StreamReader(File.OpenRead(filePath));
            return _deserializer.Deserialize<Credentials>(reader);
        }
    }
}