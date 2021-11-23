using AkkoCore.Commands.Abstractions;
using AkkoCore.Commands.Attributes;
using AkkoCore.Common;
using AkkoCore.Config.Abstractions;
using AkkoCore.Config.Models;
using AkkoCore.Core.Abstractions;
using AkkoCore.Extensions;
using AkkoCore.Services;
using AkkoCore.Services.Database;
using AkkoCore.Services.Events.Abstractions;
using AkkoCore.Services.Localization.Abstractions;
using AkkoCore.Services.Logging;
using AkkoCore.Services.Logging.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Executors;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AkkoCore.Core.Common;

/// <summary>
/// Wrapper class for building a <see cref="BotCore"/>.
/// </summary>
public sealed class BotCoreBuilder
{
    private readonly string _dbConnectionString;
    private ILoggerFactory? _loggerFactory;
    private readonly Credentials _creds;
    private readonly BotConfig _botConfig;
    private readonly LogConfig _logConfig;
    private readonly IServiceCollection _cmdServices;

    public BotCoreBuilder(Credentials creds, BotConfig botConfig, LogConfig logConfig, ILoggerFactory? loggerFactory = default, IServiceCollection? cmdServices = default)
    {
        _creds = creds ?? throw new ArgumentNullException(nameof(creds), "The credentials cannot be null.");
        _botConfig = botConfig ?? throw new ArgumentNullException(nameof(botConfig), "Configuration objects cannot be null.");
        _logConfig = logConfig ?? throw new ArgumentNullException(nameof(logConfig), "Configuration objects cannot be null.");
        _loggerFactory = loggerFactory;
        _cmdServices = cmdServices ?? new ServiceCollection();

        _dbConnectionString =
            @"Server=127.0.0.1;" +
            @"Port=5432;" +
            @"Database=AkkoBotDb;" +
            $"User Id={_creds.Database["role"]};" +
            $"Password={_creds.Database["password"]};" +
            @"CommandTimeout=20;";
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
    /// <param name="logFormat">The type of logging to be output.</param>
    /// <param name="timeFormat">The format of the time stamp to be used in the logs. Default depends on <paramref name="logFormat"/>.</param>
    /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
    public BotCoreBuilder WithDefaultLogging(LogLevel logLevel = LogLevel.Information, IFileLogger? fileLogger = default, string? logFormat = default, string? timeFormat = default)
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
    /// Adds all concrete types of this assembly that have an attribute of type
    /// <see cref="CommandServiceAttribute"/> as a service to this <see cref="BotCore"/>.
    /// </summary>
    /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
    public BotCoreBuilder WithDefaultServices()
        => WithDefaultServices(Assembly.GetCallingAssembly());

    /// <summary>
    /// Adds all concrete types of the specified assembly that have an attribute of type
    /// <see cref="CommandServiceAttribute"/> as a service to this <see cref="BotCore"/>.
    /// </summary>
    /// <param name="assembly">The assembly to register the services from.</param>
    /// <returns>This <see cref="BotCoreBuilder"/>.</returns>
    /// <exception cref="ArgumentException">Occurs when a service is registered more than once.</exception>
    /// <exception cref="InvalidOperationException">Occurs when a service is registered with an invalid interface.</exception>
    public BotCoreBuilder WithDefaultServices(Assembly assembly)
    {
        // Need to get multiple attributes per type to account for derived types,
        // but only get the first one, as this is the one that got applied to the derived type.
        // ---
        // This will cause issues for services that don't have the attribute applied to them,
        // but inherit from a service that does.
        var typesAndAttributes = AkkoUtilities.GetConcreteTypesWithAttribute<CommandServiceAttributeBase>(assembly)
            .Select(x => (Type: x, Attribute: x.GetCustomAttributes<CommandServiceAttributeBase>().First()));

        foreach (var (type, attribute) in typesAndAttributes)
            attribute.RegisterService(_cmdServices, type);

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
    /// <exception cref="ArgumentNullException">Occurs when <paramref name="implementation"/> is <see langword="null"/>.</exception>
    public BotCoreBuilder WithSingletonService<T>(T implementation)
    {
        if (implementation is null)
            throw new ArgumentNullException(nameof(implementation), "Singleton object cannot be null.");

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
    /// <exception cref="InvalidOperationException">Occurs when the entry assembly is unmanaged.</exception>
    /// <exception cref="NpgsqlException">Occurs when it's not possible to establish a connection with the database.</exception>
    public BotCoreBuilder WithDefaultDbContext()
    {
        var assemblyName = Assembly.GetEntryAssembly()?.FullName;

        if (assemblyName is null)
            throw new InvalidOperationException("Invoking this method from unmanaged code is not supported.");

        // Apply pending database migrations
        using var dbContext = new AkkoDbContext(
            new DbContextOptionsBuilder<AkkoDbContext>()
                .UseSnakeCaseNamingConvention()
                .UseNpgsql(_dbConnectionString, x => x.MigrationsAssembly(assemblyName))
                .Options
        );

        dbContext.Database.Migrate();

        // Register the database context
        return WithDbContext<AkkoDbContext>(options =>
            options.UseSnakeCaseNamingConvention()
                .UseNpgsql(_dbConnectionString, x => x.MigrationsAssembly(assemblyName))
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
    /// <exception cref="FileNotFoundException">Occurs when no localization file is found.</exception>
    /// <exception cref="UnauthorizedException">Occurs when the bot token is invalid.</exception>
    public async Task<BotCore> BuildDefaultAsync()
    {
        return await BuildAsync(
            _botConfig.InteractiveTimeout?.TotalSeconds,
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
    /// <exception cref="FileNotFoundException">Occurs when no localization file is found.</exception>
    /// <exception cref="UnauthorizedException">Occurs when the bot token is invalid.</exception>
    public async Task<BotCore> BuildAsync(double? timeout = null, bool isCaseSensitive = false, bool withDms = true, bool withMentionPrefix = true)
    {
        var shardedClient = await GetBotClientAsync(timeout);   // Initialize the sharded clients

        var cogSetups = RegisterFinalServices(shardedClient);   // Add the last services needed
        var services = _cmdServices.BuildServiceProvider();     // Initialize the IoC container

        // Initialize the command handlers
        var cmdHandlers = await GetCommandHandlersAsync(shardedClient, services, isCaseSensitive, withDms, withMentionPrefix);
        var slashHandlers = await GetSlashHandlersAsync(shardedClient, services);

        // Register the events
        var responseGenerator = services.GetRequiredService<IInteractionResponseManager>();
        var localizer = services.GetRequiredService<ILocalizer>();
        var events = services.GetRequiredService<IDiscordEventManager>();
        events.RegisterStartupEvents();
        events.RegisterDefaultEvents();

        foreach (var cogSetup in cogSetups)
        {
            cogSetup.RegisterComponentResponses(responseGenerator);         // Register interactive responses
            cogSetup.RegisterCallbacks(shardedClient);                      // Register cog callbacks
            localizer.LoadLocalizedStrings(cogSetup.LocalizationDirectory); // Load response strings from cogs
        }

        // Build the bot
        return new BotCore(shardedClient, cmdHandlers, slashHandlers);
    }

    /// <summary>
    /// Adds the last default services needed for the bot to function.
    /// </summary>
    /// <param name="shardedClient">The bot's sharded clients.</param>
    /// <remarks>It won't add services whose interface type has already been registered to <see cref="_cmdServices"/>.</remarks>
    /// <returns>A collection of cog setups.</returns>
    private IReadOnlyList<ICogSetup> RegisterFinalServices(DiscordShardedClient shardedClient)
    {
        // Load cog services and response strings
        var cogAssemblies = AkkoUtilities.GetCogAssemblies();
        var cogSetups = AkkoUtilities.GetCogSetups().ToArray();

        // Register attribute services
        foreach (var cogAssembly in cogAssemblies)
            WithDefaultServices(cogAssembly);

        // Register factory and implementation services
        foreach (var cogSetup in cogSetups)
            cogSetup.RegisterServices(_cmdServices);

        _cmdServices.AddSingleton(shardedClient);   // Add the sharded client to the IoC container
        _cmdServices.AddHttpClient();               // Add the default IHttpClientFactory

        var servicesList = new ServiceDescriptor[]
        {
                /* Add subsystems in here as needed */
                // > Base Settings
                ServiceDescriptor.Singleton(_creds),
                ServiceDescriptor.Singleton(_botConfig),
                ServiceDescriptor.Singleton(_logConfig),

                // > Utilities
                ServiceDescriptor.Singleton(_ => new DiscordWebhookClient(loggerFactory: _loggerFactory, minimumLogLevel: LogLevel.None)),
                ServiceDescriptor.Singleton(_ => new Random())
        };

        foreach (var service in servicesList)
        {
            // Add the services. Ignore any whose interface has already been registered.
            if (!_cmdServices.Any(x => x.ServiceType == service.ServiceType))
                _cmdServices.Add(service);
        }

        return cogSetups;
    }

    /// <summary>
    /// Gets the command handlers for this bot.
    /// </summary>
    /// <param name="shardedClient">The bot's sharded clients.</param>
    /// <param name="services">The services to be accessible through the command handlers.</param>
    /// <param name="isCaseSensitive">Sets whether the bot ignores case sensitivity on commands or not.</param>
    /// <param name="withDms">Sets whether the bot responds to commands in direct messages.</param>
    /// <param name="withMentionPrefix">Sets whether the bot accepts a mention to itself as a command prefix.</param>
    /// <returns>A collection of command handlers.</returns>
    private Task<IReadOnlyDictionary<int, CommandsNextExtension>> GetCommandHandlersAsync(DiscordShardedClient shardedClient, IServiceProvider services, bool isCaseSensitive, bool withDms, bool withMentionPrefix)
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
            UseDefaultCommandHandler = false,               // Sets whether the bot uses the D#+ built-in command handler or not
            CommandExecutor = new ParallelQueuedCommandExecutor(),  // Sets how commands should be executed
            PrefixResolver = services.GetRequiredService<IPrefixResolver>().ResolvePrefixAsync   // Sets the prefix, defined by the users
        };

        // Initialize the command handlers
        return shardedClient.UseCommandsNextAsync(cmdExtConfig);
    }

    /// <summary>
    /// Gets the command handlers for slash commands for this bot.
    /// </summary>
    /// <param name="shardedClient">The bot's sharded clients.</param>
    /// <param name="services">The services to be accessible through the command handlers.</param>
    /// <returns>A collection of slash command handlers.</returns>
    private Task<IReadOnlyDictionary<int, SlashCommandsExtension>> GetSlashHandlersAsync(DiscordShardedClient shardedClient, IServiceProvider services)
    {
        // Setup slash commands
        var slashOptions = new SlashCommandsConfiguration() { Services = services };
        return shardedClient.UseSlashCommandsAsync(slashOptions);
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
            Intents = DiscordIntents.All,                       // Sign up for all intents. Forgetting to enable them on the developer portal will throw an exception!
            Token = _creds.Token,                               // Sets the bot token
            TokenType = TokenType.Bot,                          // Defines the type of token; User = 0, Bot = 1, Bearer = 2
            AutoReconnect = true,                               // Sets whether the bot should automatically reconnect in case it disconnects
            ReconnectIndefinitely = false,                      // Sets whether the bot should attempt to reconnect indefinitely
            MessageCacheSize = _botConfig.MessageSizeCache,     // Defines how many messages should be cached per DiscordClient
            LoggerFactory = _loggerFactory                      // Overrides D#+ default logger with my own
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
            Timeout = TimeSpan.FromSeconds(timeout ?? 60.0),            // Sets how long it takes for an interactive response to timeout
            PaginationButtons = AkkoStatics.PaginationButtons           // Sets the default pagination buttons
        };

        var shardedClients = new DiscordShardedClient(botConfig);   // Initialize the sharded clients

        // Add interactivity to the sharded clients
        await shardedClients.UseInteractivityAsync(interactivityOptions);

        return shardedClients;
    }
}