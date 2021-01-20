using System;
using System.Threading.Tasks;
using AkkoBot.Command.Abstractions;
using AkkoBot.Credential;
using AkkoBot.Extensions;
using AkkoBot.Services.Database;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization;
using AkkoBot.Services.Localization.Abstractions;
using AkkoBot.Services.Logging;
using AkkoBot.Services.Logging.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
            _cmdServices.AddSingletonServices(typeof(ICommandService))
                .AddSingleton<ILocalizer, AkkoLocalizer>();

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
                    .UseNpgsql(
                        @"Server=127.0.0.1;" +
                        @"Port=5432;" +
                        @"Database=AkkoBotDb;" +
                        $"User Id={_creds.Database["Role"]};" +
                        $"Password={_creds.Database["Password"]};" +
                        @"CommandTimeout=20;"
                )
            ).AddSingleton<IDbCacher, AkkoDbCacher>()
            .AddScoped<IUnitOfWork, AkkoUnitOfWork>();

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
        /// Builds a <see cref="BotCore"/> from the provided settings passed to this builder.
        /// </summary>
        /// <returns>A <see cref="BotCore"/>.</returns>
        /// <exception cref="NullReferenceException"/>
        public async Task<BotCore> BuildAsync()
        {
            if (_creds is null)
                throw new NullReferenceException("No 'Credentials' object was provided.");

            var services = _cmdServices.BuildServiceProvider();
            var botSettings = services.GetService<IUnitOfWork>().BotConfig.Cache;
            var pResolver = new PrefixResolver(services.GetService<IUnitOfWork>());

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
                EnableDms = botSettings.RespondToDms,                           // Sets whether the bot responds in dm or not
                EnableMentionPrefix = botSettings.MentionPrefix,       // Sets whether the bot accepts its own mention as a prefix for commands
                IgnoreExtraArguments = false,                            // Sets whether the bot ignores extra arguments on commands or not
                Services = services,                                    // Sets the dependencies used by the command modules
                EnableDefaultHelp = botSettings.EnableHelp,           // Sets whether the bot should use the default help command from the library
                PrefixResolver = async (msg) => await pResolver.ResolvePrefix(msg)  // Sets the prefix, defined by the users
            };

            var botClient = new DiscordShardedClient(botConfig);
            var cmdHandlers = await botClient.UseCommandsNextAsync(cmdExtConfig);
            var bot = new BotCore(botClient, cmdHandlers);

            // Register core events
            var startup = new Startup(services.GetService<IUnitOfWork>());
            startup.RegisterEvents(bot);

            return bot;
        }
    }
}
