using System;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using AkkoBot.Credential;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Logging;
using AkkoBot.Services.Logging.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AkkoBot.Core.Common
{
    public class BotCoreBuilder
    {
        private Credentials _creds;
        private ILoggerFactory _loggerFactory;
        private readonly IServiceCollection _cmdServices;

        public BotCoreBuilder(Credentials creds = null, ILoggerFactory loggerFactory = null, IServiceCollection cmdServices = null)
        {
            _creds = creds;
            _loggerFactory = loggerFactory;
            _cmdServices = cmdServices ?? new ServiceCollection();
        }

        public BotCoreBuilder WithCredentials(Credentials creds)
        {
            _creds = creds;
            return WithCmdServices(creds);
        }

        public BotCoreBuilder WithDefaultLogging(LogLevel? logLevel = null, IAkkoFileLogger fileLogger = null, string logFormat = null, string timeFormat = null)
        {
            _loggerFactory = new AkkoLoggerFactory(logLevel, fileLogger, logFormat, timeFormat);
            return this;
        }

        public BotCoreBuilder WithLogging(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            return this;
        }

        public BotCoreBuilder WithDefaultCmdServices()
        {
            _cmdServices.AddSingletonServices(typeof(ICommandService));
            return this;
        }

        public BotCoreBuilder WithCmdServices(Type serviceType)
        {
            _cmdServices.AddSingletonServices(serviceType);
            return this;
        }

        public BotCoreBuilder WithCmdServices(params object[] implementations)
        {
            _cmdServices.AddSingletonServices(implementations);
            return this;
        }

        public BotCoreBuilder WithDefaultDbContext()
        {
            _cmdServices.AddDbContext<AkkoDbContext>(options =>
                options.UseSnakeCaseNamingConvention()
                    .UseNpgsql(
                        @"Server=127.0.0.1;" +
                        @"Port=5432;" +
                        @"Database=AkkoBotDb;" +
                        $"User Id={_creds.Database["Role"]};" +
                        $"Password={_creds.Database["Password"]};" +
                        @"CommandTimeout=20;"
                )
            );

            return this;
        }

        public BotCoreBuilder WithDbContext<T>(string connectionString) where T : DbContext
        {
            _cmdServices.AddDbContext<T>(options =>
                options.UseSnakeCaseNamingConvention()
                    .UseNpgsql(connectionString)
            );

            return this;
        }

        public async Task<BotCore> BuildAsync()
        {
            if (_creds is null)
                throw new InvalidOperationException("No 'Credentials' object was provided.");

            var services = _cmdServices.BuildServiceProvider();
            var pResolver = new PrefixResolver(services.GetService<AkkoUnitOfWork>());

            // Setup client configuration
            var botConfig = new DiscordConfiguration()
            {
                Token = _creds.Token,           // Sets the bot token
                TokenType = TokenType.Bot,      // Defines the type of token; User = 0, Bot = 1, Bearer = 2
                AutoReconnect = true,           // Sets whether the bot should automatically reconnect in case it disconnects
                ReconnectIndefinitely = false,  // Sets whether the bot should attempt to reconnect indefinitely
                MessageCacheSize = 200,         // Defines how many messages should be cached by the library, per channel
                LoggerFactory = _loggerFactory  // Overrides D#+ default logger with your own
            };

            // Setup command handler configuration
            var cmdExtConfig = new CommandsNextConfiguration()
            {
                CaseSensitive = true,                                   // Sets whether commands are case-sensitive
                EnableDms = _creds.EnableDms,                           // Sets whether the bot responds in dm or not
                EnableMentionPrefix = _creds.EnableMentionPrefix,       // Sets whether the bot accepts its own mention as a prefix for commands
                IgnoreExtraArguments = true,                            // Sets whether the bot ignores extra arguments on commands or not
                Services = services,                                    // Sets the dependencies used by the command modules
                EnableDefaultHelp = _creds.EnableHelpCommand,           // Sets whether the bot should use the default help command from the library
                PrefixResolver = async (msg) => await pResolver.ResolvePrefix(msg)  // Sets the prefix, defined by the users
            };

            var botClient = new DiscordShardedClient(botConfig);
            var cmdHandlers = await botClient.UseCommandsNextAsync(cmdExtConfig);

            return new BotCore(botClient, cmdHandlers);
        }
    }
}
