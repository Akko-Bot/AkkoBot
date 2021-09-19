using AkkoCore.Abstractions;
using AkkoCore.Common;
using AkkoCore.Config.Models;
using AkkoCore.Models.Serializable;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Localization;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace AkkoCore.Services
{
    /// <summary>
    /// Groups utility methods for various purposes.
    /// </summary>
    public static class AkkoUtilities
    {
        /// <summary>
        /// Gets the most optimal amount of messages to request from Discord from the specified amount of messages to get.
        /// </summary>
        /// <param name="msgAmount">The desired amount of messages to get.</param>
        /// <param name="maxRequests">The maximum amount of requests to be sent to Discord.</param>
        /// <returns>The amount of messages to request from Discord.</returns>
        public static int GetMaxMessageRequest(int msgAmount, int maxRequests)
            => (int)Math.Ceiling(Math.Min(Math.Abs(msgAmount), maxRequests * 100) / 100.0) * 100;

        /// <summary>
        /// Checks if the specified <paramref name="id"/> is registered as a bot owner.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="id">The user ID to be checked.</param>
        /// <returns><see langword="true"/> if the user is a bot owner, <see langword="false"/> otherwise.</returns>
        public static bool IsOwner(CommandContext context, ulong id)
            => context.Client.CurrentApplication.Owners.Any(x => x.Id == id) || context.Services.GetRequiredService<Credentials>().OwnerIds.Contains(id);

        /// <summary>
        /// Checks if the specified time format is valid.
        /// </summary>
        /// <param name="timeFormat">The time format to be checked.</param>
        /// <returns><see langword="true"/> if the format is valid, <see langword="false"/> otherwise.</returns>
        public static bool IsValidTimeFormat(string timeFormat)
        {
            try
            {
                _ = DateTimeOffset.Now.ToString(timeFormat, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Safely gets the culture info of the specified locale.
        /// </summary>
        /// <param name="locale">The locale.</param>
        /// <param name="getDefault">
        /// <see langword="true"/> to get the <see cref="CultureInfo"/> of <see cref="AkkoLocalizer.DefaultLanguage"/> if locale is invalid,
        /// <see langword="false"/> to get <see langword="null"/> if the locale is invalid.
        /// </param>
        /// <returns>A <see cref="CultureInfo"/> object, <see langword="null"/> if the locale is invalid.</returns>
        public static CultureInfo GetCultureInfo(string locale, bool getDefault = false)
        {
            try { return CultureInfo.CreateSpecificCulture(locale); }
            catch { return (getDefault) ? CultureInfo.CreateSpecificCulture(AkkoConstants.DefaultLanguage) : null; }
        }

        /// <summary>
        /// Safely gets the specified timezone info.
        /// </summary>
        /// <param name="id">The timezone ID.</param>
        /// <param name="getDefault"><see langword="true"/> to return the local timezone if the specified ID is invalid, <see langword="false"/> to return <see langword="null"/>.</param>
        /// <returns>A <see cref="TimeZoneInfo"/> object, <see langword="null"/> if the specified ID is invalid.</returns>
        public static TimeZoneInfo GetTimeZone(string id, bool getDefault = false)
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch { return (getDefault) ? TimeZoneInfo.Local : null; }
        }

        /// <summary>
        /// Safely gets a color for the specified hexadecimal color code.
        /// </summary>
        /// <param name="colorCode">The hexadecimal color code.</param>
        /// <returns>A <see cref="DiscordColor"/> with the specified color, an empty <see cref="Optional{T}"/> if the code is invalid.</returns>
        public static Optional<DiscordColor> GetColor(string colorCode)
        {
            try { return Optional.FromValue(new DiscordColor(colorCode)); }
            catch { return Optional.FromNoValue<DiscordColor>(); }
        }

        /// <summary>
        /// Gets a collection of all concrete classes of the specified type in the current calling assembly.
        /// </summary>
        /// <param name="abstraction">The type implemented by all classes.</param>
        /// <returns>A collection of types.</returns>
        /// <exception cref="ArgumentException">Occurs when <paramref name="abstraction"/> is not an abstraction.</exception>
        public static IEnumerable<Type> GetConcreteTypesOf(Type abstraction)
        {
            return (!abstraction.IsAbstract || !abstraction.IsInterface)
                ? throw new ArgumentException("Type must be an interface or an abstract class.", nameof(abstraction))
                : Assembly.GetCallingAssembly()
                    .GetTypes()                             // Get all types in the calling assembly.
                    .Where(type =>
                        abstraction.IsAssignableFrom(type)  // Filter to find any concrete type that can be assigned to the specified abstraction
                        && !type.IsInterface
                        && !type.IsAbstract
                        && !type.IsNested
                );
        }

        /// <summary>
        /// Gets a collection of assemblies from the cogs directory
        /// </summary>
        /// <remarks>
        /// This method assumes all assemblies have AkkoBot as a dependency reference and
        /// contain commands that can be registered on CommandsNext.
        /// </remarks>
        /// <returns>A collection of assemblies.</returns>
        internal static IEnumerable<Assembly> GetCogs()
        {
            // Create directory if it doesn't exist already.
            if (!Directory.Exists(AkkoEnvironment.CogsDirectory))
                Directory.CreateDirectory(AkkoEnvironment.CogsDirectory);

            // Get all cogs from the cogs directory
            return Directory.EnumerateFiles(AkkoEnvironment.CogsDirectory, "*.dll", SearchOption.AllDirectories)
                //.Where(filePath => filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Select(filePath => Assembly.LoadFrom(filePath));
        }

        /// <summary>
        /// Gets all cog setups from the loaded cogs.
        /// </summary>
        /// <returns>A collection of cog setups.</returns>
        internal static IEnumerable<ICogSetup> GetCogSetups()
        {
            return GetCogs()
                .SelectMany(x => x.ExportedTypes)
                .Where(x => x.IsAssignableTo(typeof(ICogSetup)))
                .Select(x => Activator.CreateInstance(x) as ICogSetup);
        }

        /// <summary>
        /// Gets the localized Discord message.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="message">The message.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
        /// <returns>The localized message content, embed, and the message settings.</returns>
        internal static (SerializableDiscordMessage, IMessageSettings) GetLocalizedMessage(CommandContext context, SerializableDiscordMessage message, bool isError)
        {
            var dbCache = context.Services.GetRequiredService<IDbCache>();
            var localizer = context.Services.GetRequiredService<ILocalizer>();

            // Get the message settings (guild or dm)
            IMessageSettings settings = (dbCache.Guilds.TryGetValue(context.Guild?.Id ?? default, out var dbGuild))
                ? dbGuild
                : context.Services.GetRequiredService<BotConfig>();

            foreach (var embed in message.Embeds ?? Enumerable.Empty<SerializableDiscordEmbed>())
                embed.Color ??= (isError) ? settings.ErrorColor : settings.OkColor;

            message.WithLocalization(localizer, settings.Locale);

            return (message, settings);
        }

        /// <summary>
        /// Gets the localized Discord embed.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="embed">The embed.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
        /// <returns>The localized embed and the message settings.</returns>
        internal static (SerializableDiscordEmbed, IMessageSettings) GetLocalizedMessage(CommandContext context, SerializableDiscordEmbed embed, bool isError)
        {
            var dbCache = context.Services.GetRequiredService<IDbCache>();
            var localizer = context.Services.GetRequiredService<ILocalizer>();

            // Get the message settings (guild or dm)
            IMessageSettings settings = (dbCache.Guilds.TryGetValue(context.Guild?.Id ?? default, out var dbGuild))
                ? dbGuild
                : context.Services.GetRequiredService<BotConfig>();

            embed.Color ??= (isError) ? settings.ErrorColor : settings.OkColor;
            embed.WithLocalization(localizer, settings.Locale);

            return (embed, settings);
        }
    }
}