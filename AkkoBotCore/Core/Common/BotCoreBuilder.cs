using AkkoBot.Commands.Abstractions;
using AkkoBot.Core.Services;
using AkkoBot.Credential;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Localization;
using AkkoBot.Services.Localization.Abstractions;
using AkkoBot.Services.Logging;
using AkkoBot.Services.Logging.Abstractions;
using AkkoBot.Services.Timers;
using AkkoBot.Services.Timers.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AkkoBot.Core.Common
{
    /// <summary>
    /// Wrapper class for building a <see cref="BotCore"/>.
    /// </summary>
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

        /// <summary>
        /// Adds the credentials to be used for authentication with Discord.
        /// </summary>
        /// <param name="creds">The credentials.</param>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithCredentials(Credentials creds)
        {
            _creds = creds;
            return WithSingletonServices(creds);
        }

        /// <summary>
        /// Overrides DSharpPlus' default logger with an <see cref="AkkoLoggerFactory"/>.
        /// </summary>
        /// <remarks>The factory will be configured with the settings defined in the database, if possible.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithDefaultLogging()
        {
            var (_, logConfig) = GetBotSettings();

            _loggerFactory = new AkkoLoggerFactory(
                logConfig.LogLevel,
                (logConfig.IsLoggedToFile) ? new AkkoFileLogger(logConfig.LogSizeMB, logConfig.LogTimeFormat) : null,
                logConfig.LogFormat,
                logConfig.LogTimeFormat
            );

            return this;
        }

        /// <summary>
        /// Overrides DSharpPlus' default logger with an <see cref="AkkoLoggerFactory"/>.
        /// </summary>
        /// <param name="logLevel">Minimum log severity to be logged. Default is "Information".</param>
        /// <param name="fileLogger">An object responsible for writing the logs to a text file. Default is <see langword="null"/> (no file logging).</param>
        /// <param name="logFormat">The type of logging to be output. Default is "Default".</param>
        /// <param name="timeFormat">The format of the time stamp to be used in the logs. Default depends on <paramref name="logFormat"/>.</param>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithDefaultLogging(LogLevel? logLevel = null, IFileLogger fileLogger = null, string logFormat = null, string timeFormat = null)
        {
            _loggerFactory = new AkkoLoggerFactory(logLevel, fileLogger, logFormat, timeFormat);
            return this;
        }

        /// <summary>
        /// Overrides DSharpPlus' default logger factory with your own implementation.
        /// </summary>
        /// <param name="loggerFactory">A concrete <see cref="ILoggerFactory"/>.</param>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithLogging(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            return this;
        }

        /// <summary>
        /// Adds all objects that implement <see cref="ICommandService"/> as a singleton service to this <see cref="BotCore"/>.
        /// </summary>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithDefaultServices()
        {
            _cmdServices.AddSingletonServices(typeof(ICommandService));

            return this;
        }

        /// <summary>
        /// Adds a <typeparamref name="T2"/> object with a <typeparamref name="T1"/> interface as a singleton service to this <see cref="BotCore"/>.
        /// </summary>
        /// <typeparam name="T1">An abstract Type.</typeparam>
        /// <typeparam name="T2">A concrete Type.</typeparam>
        /// <remarks>Use this to inject the command modules with your own services.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithSingletonService<T1, T2>() where T1 : Type where T2 : T1
        {
            _cmdServices.AddSingleton<T1, T2>();
            return this;
        }

        /// <summary>
        /// Adds a <typeparamref name="T2"/> object with a <typeparamref name="T1"/> interface as a scoped service to this <see cref="BotCore"/>.
        /// </summary>
        /// <typeparam name="T1">An abstract Type.</typeparam>
        /// <typeparam name="T2">A concrete Type.</typeparam>
        /// <remarks>Use this to inject the command modules with your own services.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithScopedService<T1, T2>() where T1 : Type where T2 : T1
        {
            _cmdServices.AddScoped<T1, T2>();
            return this;
        }

        /// <summary>
        /// Adds a <typeparamref name="T2"/> object with a <typeparamref name="T1"/> interface as a transient service to this <see cref="BotCore"/>.
        /// </summary>
        /// <typeparam name="T1">An abstract Type.</typeparam>
        /// <typeparam name="T2">A concrete Type.</typeparam>
        /// <remarks>Use this to inject the command modules with your own services.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithTransientService<T1, T2>() where T1 : Type where T2 : T1
        {
            _cmdServices.AddTransient<T1, T2>();
            return this;
        }

        /// <summary>
        /// Adds all concrete classes that implement <typeparamref name="T"/> as singleton services to this <see cref="BotCore"/>.
        /// </summary>
        /// <typeparam name="T">An abstract Type.</typeparam>
        /// <remarks>Use this to inject the command modules with your own services.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithSingletonServices<T>()
        {
            _cmdServices.AddSingletonServices(typeof(T));
            return this;
        }

        /// <summary>
        /// Adds all concrete classes that implement <paramref name="serviceType"/> as singleton services to this <see cref="BotCore"/>.
        /// </summary>
        /// <param name="serviceType">An abstract Type.</param>
        /// <remarks>Use this to inject the command modules with your own services.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithSingletonServices(Type serviceType)
        {
            _cmdServices.AddSingletonServices(serviceType);
            return this;
        }

        /// <summary>
        /// Adds the specified concrete objects as singleton services to this <see cref="BotCore"/>.
        /// </summary>
        /// <param name="implementations">Collection of service objects.</param>
        /// <remarks>Use this to inject the command modules with your own services.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithSingletonServices(params object[] implementations)
        {
            _cmdServices.AddSingletonServices(implementations);
            return this;
        }

        /// <summary>
        /// Adds all concrete classes that implement <typeparamref name="T"/> as scoped services to this <see cref="BotCore"/>.
        /// </summary>
        /// <typeparam name="T">An abstract Type.</typeparam>
        /// <remarks>Use this to inject the command modules with your own services.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithScopedServices<T>()
        {
            _cmdServices.AddScopedServices(typeof(T));
            return this;
        }

        /// <summary>
        /// Adds all concrete classes that implement <paramref name="serviceType"/> as scoped services to this <see cref="BotCore"/>.
        /// </summary>
        /// <param name="serviceType">An abstract Type.</param>
        /// <remarks>Use this to inject the command modules with your own services.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithScopedServices(Type serviceType)
        {
            _cmdServices.AddScopedServices(serviceType);
            return this;
        }

        /// <summary>
        /// Adds all concrete classes that implement <typeparamref name="T"/> as transient services to this <see cref="BotCore"/>.
        /// </summary>
        /// <typeparam name="T">An abstract Type.</typeparam>
        /// <remarks>Use this to inject the command modules with your own services.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithTransientServices<T>()
        {
            _cmdServices.AddTransientServices(typeof(T));
            return this;
        }

        /// <summary>
        /// Adds all concrete classes that implement <paramref name="serviceType"/> as transient services to this <see cref="BotCore"/>.
        /// </summary>
        /// <param name="serviceType">An abstract Type.</param>
        /// <remarks>Use this to inject the command modules with your own services.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithTransientServices(Type serviceType)
        {
            _cmdServices.AddTransientServices(serviceType);
            return this;
        }

        /// <summary>
        /// Adds the default <see cref="AkkoDbContext"/> as a service to this <see cref="BotCore"/>.
        /// </summary>
        /// <remarks>This database context depends directly on EF Core and Npgsql.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithDefaultDbContext()
        {
            _cmdServices.AddDbContext<AkkoDbContext>(options =>
                options.UseSnakeCaseNamingConvention()
                    .UseNpgsql(GetConnectionString())
            );

            return this;
        }

        /// <summary>
        /// Adds an EF Core <see cref="DbContext"/> as a service to this <see cref="BotCore"/>.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
        /// <param name="connectionString">The connection string for the database.</param>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithDbContext<T>(string connectionString) where T : DbContext
        {
            _cmdServices.AddDbContext<T>(options =>
                options.UseSnakeCaseNamingConvention()
                    .UseNpgsql(connectionString)
            );

            return this;
        }

        /// <summary>
        /// Builds a <see cref="BotCore"/> from the provided settings passed to this builder,
        /// assuming <see langword="abstract"/> default database is being used.
        /// </summary>
        /// <returns>A <see cref="BotCore"/>.</returns>
        /// <exception cref="NullReferenceException">Occurs when no credentials file is provided to this builder.</exception>
        public async Task<BotCore> BuildDefaultAsync()
        {
            var (botSettings, _) = GetBotSettings();

            return await BuildAsync(
                botSettings.InteractiveTimeout.Value.TotalSeconds,
                botSettings.CaseSensitiveCommands,
                botSettings.RespondToDms,
                botSettings.MentionPrefix
            );
        }

        /// <summary>
        /// Builds a <see cref="BotCore"/> from the provided settings passed to this builder.
        /// </summary>
        /// <param name="isCaseSensitive">Sets whether the bot ignores case sensitivity on commands or not.</param>
        /// <param name="withDms">Sets whether the bot responds to commands in direct messages.</param>
        /// <param name="withMentionPrefix">Sets whether the bot accepts a mention to itself as a command prefix.</param>
        /// <returns>A <see cref="BotCore"/>.</returns>
        /// <exception cref="NullReferenceException">Occurs when no credentials object is provided to this builder.</exception>
        public async Task<BotCore> BuildAsync(double? timeout = null, bool isCaseSensitive = false, bool withDms = true, bool withMentionPrefix = true)
        {
            if (_creds is null)
                throw new NullReferenceException("No 'Credentials' object was provided.");

            var botClients = await GetBotClientAsync(timeout); // Initialize the sharded clients

            RegisterFinalServices(botClients);                  // Add the last services needed
            var services = _cmdServices.BuildServiceProvider(); // Initialize the IoC container

            // Initialize the command handlers
            var cmdHandlers = await GetCommandHandlers(botClients, services, isCaseSensitive, withDms, withMentionPrefix);

            // Build the bot
            var bot = new BotCore(botClients, cmdHandlers);

            // Register core events
            var startup = new Startup(bot, services);
            startup.RegisterEvents();

            var globalEvents = new GlobalEvents(bot, services);
            globalEvents.RegisterEvents();

            return bot;
        }

        /// <summary>
        /// Adds the last default services needed for the bot to function.
        /// </summary>
        /// <param name="client">The bot's sharded clients.</param>
        /// <remarks>It won't add services whose interface type has already been registered to <see cref="_cmdServices"/>.</remarks>
        private void RegisterFinalServices(DiscordShardedClient client)
        {
            // Add the clients to the IoC container
            _cmdServices.AddSingleton(client);

            var servicesList = new ServiceDescriptor[]
            {
                // Add subsystems in here as needed
                // > Database
                ServiceDescriptor.Singleton<IDbCacher, AkkoDbCacher>(),
                ServiceDescriptor.Scoped<IUnitOfWork, AkkoUnitOfWork>(),

                // > Localization
                ServiceDescriptor.Singleton<ILocalizer, AkkoLocalizer>(),

                // > Timers
                ServiceDescriptor.Singleton<ITimerManager, TimerManager>(),
                //ServiceDescriptor.Singleton(typeof(TimerActions))

                // > Utilities
                ServiceDescriptor.Singleton(new HttpClient())
            };

            foreach (var service in servicesList)
            {
                // Add the services. Ignore any whose interface has already been registered.
                if (!_cmdServices.Any(x => x.ServiceType == service.ServiceType))
                    _cmdServices.Add(service);
            }
        }

        /// <summary>
        /// Gets the command handlers for this bot.
        /// </summary>
        /// <param name="botClients">The bot's sharded clients.</param>
        /// <param name="services">The services to be accessible through the command handlers.</param>
        /// <param name="isCaseSensitive">Sets whether the bot ignores case sensitivity on commands or not.</param>
        /// <param name="withDms">Sets whether the bot responds to commands in direct messages.</param>
        /// <param name="withMentionPrefix">Sets whether the bot accepts a mention to itself as a command prefix.</param>
        /// <returns>A collection of command handlers.</returns>
        private async Task<IReadOnlyDictionary<int, CommandsNextExtension>> GetCommandHandlers(DiscordShardedClient botClients, IServiceProvider services, bool isCaseSensitive, bool withDms, bool withMentionPrefix)
        {
            var pResolver = new PrefixResolver(services.GetService<IUnitOfWork>());

            // Setup command handler configuration
            var cmdExtConfig = new CommandsNextConfiguration()
            {
                CaseSensitive = isCaseSensitive,                        // Sets whether commands are case-sensitive
                EnableDms = withDms,                                    // Sets whether the bot responds in dm or not
                EnableMentionPrefix = withMentionPrefix,                // Sets whether the bot accepts its own mention as a prefix for commands
                IgnoreExtraArguments = false,                           // Sets whether the bot ignores extra arguments on commands or not
                Services = services,                                    // Sets the dependencies used by the command modules
                EnableDefaultHelp = false,                              // Sets whether the bot should use the default help command from the library
                PrefixResolver = (msg) => pResolver.ResolvePrefix(msg)  // Sets the prefix, defined by the users
            };

            // Initialize the command handlers
            return await botClients.UseCommandsNextAsync(cmdExtConfig);
        }

        /// <summary>
        /// Gets the sharded client for this bot.
        /// </summary>
        /// <param name="timeout">Sets the interactivity action timeout.</param>
        /// <returns>A sharded client properly configured.</returns>
        private async Task<DiscordShardedClient> GetBotClientAsync(double? timeout)
        {
            // Setup client configuration
            var botConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,   // Sign up for all intents. Forgetting to enable them on the developer portal will throw an exception!
                Token = _creds.Token,           // Sets the bot token
                TokenType = TokenType.Bot,      // Defines the type of token; User = 0, Bot = 1, Bearer = 2
                AutoReconnect = true,           // Sets whether the bot should automatically reconnect in case it disconnects
                ReconnectIndefinitely = false,  // Sets whether the bot should attempt to reconnect indefinitely
                MessageCacheSize = 200,         // Defines how many messages should be cached by the library, per channel
                LoggerFactory = _loggerFactory  // Overrides D#+ default logger with my own
            };

            // Setup client interactivity
            var interactivityOptions = new InteractivityConfiguration()
            {
                PaginationBehaviour = PaginationBehaviour.WrapAround,   // Sets whether paginated responses should wrap from first page to last page and vice-versa
                PaginationDeletion = PaginationDeletion.DeleteEmojis,   // Sets whether emojis or the paginated message should be deleted after timeout
                PollBehaviour = PollBehaviour.KeepEmojis,               // Sets whether emojis should be deleted after a poll ends
                Timeout = TimeSpan.FromSeconds(timeout ?? 30.0),        // Sets how long it takes for an interactive response to timeout
                // setup customized paginated emojis
            };

            var shardedClients = new DiscordShardedClient(botConfig);   // Initialize the sharded clients

            // Add interactivity to the sharded clients
            await shardedClients.UseInteractivityAsync(interactivityOptions);

            return shardedClients;
        }

        /// <summary>
        /// Gets the settings stored in the default database, if there are any.
        /// </summary>
        /// <returns>The settings stored in the database, default ones if none is found.</returns>
        /// <exception cref="NullReferenceException">Occurs when no dbContext is provided to this builder.</exception>
        private (BotConfigEntity, LogConfigEntity) GetBotSettings()
        {
            using var dbContext = new AkkoDbContext(
                new DbContextOptionsBuilder<AkkoDbContext>()
                    .UseSnakeCaseNamingConvention()
                    .UseNpgsql(GetConnectionString())
                    .Options
            );

            var botConfig = dbContext.BotConfig.FirstOrDefault() ?? new BotConfigEntity();
            var logConfig = dbContext.LogConfig.FirstOrDefault() ?? new LogConfigEntity();
            dbContext.Dispose();

            return (botConfig, logConfig);
        }

        /// <summary>
        /// Gets the default database connection string.
        /// </summary>
        /// <returns>The connection string.</returns>
        /// <exception cref="NullReferenceException">Occurs when no credentials object is provided to this builder.</exception>
        private string GetConnectionString()
        {
            return
                @"Server=127.0.0.1;" +
                @"Port=5432;" +
                @"Database=AkkoBotDb;" +
                $"User Id={_creds.Database["Role"]};" +
                $"Password={_creds.Database["Password"]};" +
                @"CommandTimeout=20;";
        }
    }
}