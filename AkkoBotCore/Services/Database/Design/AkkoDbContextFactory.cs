using AkkoBot.Credential;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;
using YamlDotNet.Serialization;

namespace AkkoBot.Services.Database.Design
{
    /// <summary>
    /// This class is only used at design time, when EF Core is asked to perform a migration.
    /// </summary>
    public class AkkoDbContextFactory : IDesignTimeDbContextFactory<AkkoDbContext>
    {
        public AkkoDbContext CreateDbContext(string[] args)
        {
            var creds = LoadCredentials(AkkoEnvironment.CredsPath);

            var options = new DbContextOptionsBuilder<AkkoDbContext>()
                .UseSnakeCaseNamingConvention()
                .UseNpgsql(
                        @"Server=127.0.0.1;" +
                        @"Port=5432;" +
                        @"Database=AkkoBotDb;" +
                        $"User Id={creds.Database["Role"]};" +
                        $"Password={creds.Database["Password"]};" +
                        @"CommandTimeout=20;"
                )
                .Options;

            return new AkkoDbContext(options);
        }

        private Credentials LoadCredentials(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"It was not possible to locate the credentials file at {filePath}.");

            // Open the file and deserialize it.
            using var reader = new StreamReader(File.OpenRead(filePath));
            return new Deserializer().Deserialize<Credentials>(reader);
        }
    }
}