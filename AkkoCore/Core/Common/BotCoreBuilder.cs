using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Common;
using AkkoCore.Commands.Formatters;
using AkkoCore.Common;
using AkkoCore.Config;
using AkkoCore.Config.Abstractions;
using AkkoCore.Config.Models;
using AkkoCore.Core.Abstractions;
using AkkoCore.Core.Services;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Events;
using AkkoCore.Services.Events.Abstractions;
using AkkoCore.Services.Events.Common;
using AkkoCore.Services.Localization;
using AkkoCore.Services.Localization.Abstractions;
using AkkoCore.Services.Logging;
using AkkoCore.Services.Logging.Abstractions;
using AkkoCore.Services.Timers;
using AkkoCore.Services.Timers.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AkkoCore.Core.Common
{
    /// <summary>
    /// Wrapper class for building a <see cref="BotCore"/>.
    /// </summary>
    public class BotCoreBuilder
    {
        private ILoggerFactory _loggerFactory;
        private readonly Credentials _creds;
        private readonly BotConfig _botConfig;
        private readonly LogConfig _logConfig;
        private readonly IServiceCollection _cmdServices;

        public BotCoreBuilder(Credentials creds, BotConfig botConfig, LogConfig logConfig, ILoggerFactory loggerFactory = null, IServiceCollection cmdServices = null)
        {
            _creds = creds ?? throw new ArgumentNullException(nameof(creds), "The credentials cannot be null.");
            _botConfig = botConfig ?? throw new ArgumentNullException(nameof(botConfig), "Configuration objects cannot be null.");
            _logConfig = logConfig ?? throw new ArgumentNullException(nameof(logConfig), "Configuration objects cannot be null.");
            _loggerFactory = loggerFactory;
            _cmdServices = cmdServices ?? new ServiceCollection();
        }

        /// <summary>
        /// Overrides DSharpPlus' default logger with Akko loggers.
        /// </summary>
        /// <remarks>The factory will be configured with the settings defined in the database, if possible.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithDefaultLogging()
        {
            var fileLogger = (_logConfig.IsLoggedToFile)
                ? new AkkoFileLogger(_logConfig.LogSizeMb, _logConfig.LogTimeStamp)
                : null;

            return WithDefaultLogging(_logConfig.LogLevel, fileLogger, _logConfig.LogFormat, _logConfig.LogTimeFormat);
        }

        /// <summary>
        /// Overrides DSharpPlus' default logger with Akko loggers.
        /// </summary>
        /// <param name="logLevel">Minimum log severity to be logged.</param>
        /// <param name="fileLogger">An object responsible for writing the logs to a text file. Default is <see langword="null"/> (no file logging).</param>
        /// <param name="logFormat">The type of logging to be output. Default is "Default".</param>
        /// <param name="timeFormat">The format of the time stamp to be used in the logs. Default depends on <paramref name="logFormat"/>.</param>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithDefaultLogging(LogLevel logLevel = LogLevel.Information, IFileLogger fileLogger = default, string logFormat = "Default", string timeFormat = default)
        {
            var logProvider = new AkkoLoggerProvider(
                (logLevel is LogLevel.Information) ? _logConfig.LogLevel : logLevel,
                fileLogger,
                logFormat ?? _logConfig.LogFormat,
                timeFormat ?? _logConfig.LogTimeFormat
            );

            _loggerFactory = LoggerFactory.Create(builder =>
                builder.AddProvider(logProvider)
                    .AddFilter((category, level) =>
                        (category.EqualsAny(DbLoggerCategory.Database.Command.Name, "LinqToDB") && level is LogLevel.Information)   // Add database queries
                        || category.Equals(typeof(BaseDiscordClient).FullName)                                                      // Add DiscordClient event logs
                    )
            );

            _cmdServices.AddSingleton<IAkkoLoggerProvider>(logProvider);
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
        /// Adds the specified concrete object as a singleton service to this <see cref="BotCore"/>
        /// under the <typeparamref name="T"/> abstraction.
        /// </summary>
        /// <typeparam name="T">The abstraction implemented by <paramref name="implementation"/>.</typeparam>
        /// <param name="implementation">The service implementation.</param>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithSingletonService<T>(T implementation)
        {
            _cmdServices.AddSingleton(typeof(T), implementation);
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
        /// <remarks>This database context has a direct dependency with EF Core and Npgsql.</remarks>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        /// <exception cref="NpgsqlException">Occurs when it's not possible to establish a connection with the database.</exception>
        public BotCoreBuilder WithDefaultDbContext()
        {
            var assemblyName = Assembly.GetEntryAssembly().FullName;

            // Apply pending database migrations
            using var dbContext = new AkkoDbContext(
                new DbContextOptionsBuilder<AkkoDbContext>()
                    .UseSnakeCaseNamingConvention()
                    .UseNpgsql(GetDefaultConnectionString(), x => x.MigrationsAssembly(assemblyName))
                    .Options
            );

            dbContext.Database.Migrate();

            // Register the database context
            return WithDbContext<AkkoDbContext>(options =>
                options.UseSnakeCaseNamingConvention()
                    .UseNpgsql(GetDefaultConnectionString(), x => x.MigrationsAssembly(assemblyName))
                    .UseLoggerFactory(_loggerFactory)
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            );
        }

        /// <summary>
        /// Adds an EF Core <see cref="DbContext"/> as a service to this <see cref="BotCore"/>.
        /// </summary>
        /// <typeparam name="T">Type of the <see cref="DbContext"/>.</typeparam>
        /// <param name="optionsAction">The options for the database connection.</param>
        /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
        public BotCoreBuilder WithDbContext<T>(Action<DbContextOptionsBuilder> optionsAction) where T : DbContext
        {
            _cmdServices.AddDbContextPool<T>(optionsAction);
            return this;
        }

        /// <summary>
        /// Builds a <see cref="BotCore"/> from the provided settings passed to this builder.
        /// </summary>
        /// <returns>A <see cref="BotCore"/>.</returns>
        public async Task<BotCore> BuildDefaultAsync()
        {
            return await BuildAsync(
                _botConfig.InteractiveTimeout.Value.TotalSeconds,
                _botConfig.CaseSensitiveCommands,
                _botConfig.RespondToDms,
                _botConfig.MentionPrefix
            );
        }

        /// <summary>
        /// Builds a <see cref="BotCore"/> from the provided settings passed to this builder.
        /// </summary>
        /// <param name="isCaseSensitive">Sets whether the bot ignores case sensitivity on commands or not.</param>
        /// <param name="withDms">Sets whether the bot responds to commands in direct messages.</param>
        /// <param name="withMentionPrefix">Sets whether the bot accepts a mention to itself as a command prefix.</param>
        /// <returns>A <see cref="BotCore"/>.</returns>
        public async Task<BotCore> BuildAsync(double? timeout = null, bool isCaseSensitive = false, bool withDms = true, bool withMentionPrefix = true)
        {
            var shardedClient = await GetBotClientAsync(timeout);   // Initialize the sharded clients

            RegisterFinalServices(shardedClient);                   // Add the last services needed
            var services = _cmdServices.BuildServiceProvider();     // Initialize the IoC container

            // Initialize the command handlers
            var cmdHandlers = await GetCommandHandlersAsync(shardedClient, services, isCaseSensitive, withDms, withMentionPrefix);

            // Register the events
            var events = services.GetRequiredService<IDiscordEventManager>();
            events.RegisterStartupEvents();
            events.RegisterEvents();

            // Build the bot
            return new BotCore(shardedClient, cmdHandlers);
        }

        /// <summary>
        /// Adds the last default services needed for the bot to function.
        /// </summary>
        /// <param name="shardedClient">The bot's sharded clients.</param>
        /// <remarks>It won't add services whose interface type has already been registered to <see cref="_cmdServices"/>.</remarks>
        private void RegisterFinalServices(DiscordShardedClient shardedClient)
        {
            // Add the clients to the IoC container
            _cmdServices.AddSingleton(shardedClient);
            _cmdServices.AddHttpClient();   // Adds the default IHttpClientFactory

            var servicesList = new ServiceDescriptor[]
            {
                /* Add subsystems in here as needed */
                // > Base Settings
                ServiceDescriptor.Singleton(_creds),
                ServiceDescriptor.Singleton(_botConfig),
                ServiceDescriptor.Singleton(_logConfig),

                // > Caching
                ServiceDescriptor.Singleton<IDbCache, AkkoDbCache>(),
                ServiceDescriptor.Singleton<IAkkoCache, AkkoCache>(),

                // > Localization
                ServiceDescriptor.Singleton<ILocalizer, AkkoLocalizer>(),

                // > Timers
                ServiceDescriptor.Singleton<ITimerActions, TimerActions>(),
                ServiceDescriptor.Singleton<ITimerManager, TimerManager>(),

                // > Utilities
                ServiceDescriptor.Transient<IMemberAggregator, MemberAggregator>(),
                ServiceDescriptor.Singleton<IAntiAltActions, AntiAltActions>(),
                ServiceDescriptor.Singleton<IConfigLoader, ConfigLoader>(),
                ServiceDescriptor.Singleton(_ => new DiscordWebhookClient(loggerFactory: _loggerFactory, minimumLogLevel: LogLevel.None)),
                ServiceDescriptor.Singleton(_ => new Random()),

                // > Commands
                ServiceDescriptor.Singleton<IPlaceholderFormatter, CommandPlaceholders>(),
                ServiceDescriptor.Transient<IHelpFormatter, HelpFormatter>(),
                ServiceDescriptor.Singleton<IPrefixResolver, PrefixResolver>(),
                ServiceDescriptor.Singleton<ICommandCooldown, AkkoCooldown>(),

                // > Event Handlers
                ServiceDescriptor.Singleton<IDiscordEventManager, DiscordEventManager>(),
                ServiceDescriptor.Singleton<IStartupEventHandler, StartupEventHandler>(),
                ServiceDescriptor.Singleton<IVoiceRoleConnectionHandler, VoiceRoleConnectionHandler>(),
                ServiceDescriptor.Singleton<IGuildLoadHandler, GuildLoadHandler>(),
                ServiceDescriptor.Singleton<IGuildEventsHandler, GuildEventsHandler>(),
                ServiceDescriptor.Singleton<IGlobalEventsHandler, GlobalEventsHandler>(),
                ServiceDescriptor.Singleton<ICommandLogHandler, CommandLogHandler>(),
                ServiceDescriptor.Singleton<IGatekeepEventHandler, GatekeepEventHandler>(),
                ServiceDescriptor.Singleton<IGuildLogEventHandler, GuildLogEventHandler>(),
                ServiceDescriptor.Singleton<IGuildLogGenerator, GuildLogGenerator>(),
                ServiceDescriptor.Singleton<ITagEventHandler, TagEventHandler>()
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
        private async Task<IReadOnlyDictionary<int, CommandsNextExtension>> GetCommandHandlersAsync(DiscordShardedClient botClients, IServiceProvider services, bool isCaseSensitive, bool withDms, bool withMentionPrefix)
        {
            // Setup command handler configuration
            var cmdExtConfig = new CommandsNextConfiguration()
            {
                CaseSensitive = isCaseSensitive,                // Sets whether commands are case-sensitive
                EnableDms = withDms,                            // Sets whether the bot responds in dm or not
                EnableMentionPrefix = withMentionPrefix,        // Sets whether the bot accepts its own mention as a prefix for commands
                IgnoreExtraArguments = false,                   // Sets whether the bot ignores extra arguments on commands or not
                Services = services,                            // Sets the dependencies used by the command modules
                EnableDefaultHelp = false,                      // Sets whether the bot should use the default help command from the library
                PrefixResolver = services.GetRequiredService<IPrefixResolver>().ResolvePrefixAsync   // Sets the prefix, defined by the users
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
                MessageCacheSize = 200,         // Defines how many messages should be cached per DiscordClient
                LoggerFactory = _loggerFactory  // Overrides D#+ default logger with my own
            };

            // Setup client interactivity
            var interactivityOptions = new InteractivityConfiguration()
            {
                AckPaginationButtons = true,
                ResponseBehavior = InteractionResponseBehavior.Ignore,      // Sets whether interactive messages should ignore or respond to invalid input
                ButtonBehavior = ButtonPaginationBehavior.DeleteButtons,    // Sets what should happen to buttons when an interaction is done running.
                PaginationBehaviour = PaginationBehaviour.WrapAround,       // Sets whether paginated responses should wrap from first page to last page and vice-versa
                PaginationDeletion = PaginationDeletion.DeleteEmojis,       // Sets whether emojis or the paginated message should be deleted after timeout
                PollBehaviour = PollBehaviour.KeepEmojis,                   // Sets whether emojis should be deleted after a poll ends
                Timeout = TimeSpan.FromSeconds(timeout ?? 30.0),            // Sets how long it takes for an interactive response to timeout
                PaginationButtons = AkkoStatics.PaginationButtons
            };

            var shardedClients = new DiscordShardedClient(botConfig);   // Initialize the sharded clients

            // Add interactivity to the sharded clients
            await shardedClients.UseInteractivityAsync(interactivityOptions);

            return shardedClients;
        }

        /// <summary>
        /// Gets the default database connection string.
        /// </summary>
        /// <returns>The connection string.</returns>
        /// <exception cref="NullReferenceException">Occurs when no credentials object is provided to this builder.</exception>
        private string GetDefaultConnectionString()
        {
            return
                @"Server=127.0.0.1;" +
                @"Port=5432;" +
                @"Database=AkkoBotDb;" +
                $"User Id={_creds.Database["role"]};" +
                $"Password={_creds.Database["password"]};" +
                @"CommandTimeout=20;";
        }
    }
}