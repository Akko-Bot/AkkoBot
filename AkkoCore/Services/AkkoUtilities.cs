using AkkoCore.Common;
using AkkoCore.Config.Abstractions;
using AkkoCore.Config.Models;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Models.Serializable.EmbedParts;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Localization;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

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
        /// Checks if the specified <paramref name="id"/> is registered as a bot owner.
        /// </summary>
        /// <param name="context">The interaction context.</param>
        /// <param name="id">The user ID to be checked.</param>
        /// <returns><see langword="true"/> if the user is a bot owner, <see langword="false"/> otherwise.</returns>
        public static bool IsOwner(InteractionContext context, ulong id)
            => context.Client.CurrentApplication.Owners.Any(x => x.Id == id) || context.Services.GetRequiredService<Credentials>().OwnerIds.Contains(id);

        /// <summary>
        /// Gets a collection of all concrete classes of the specified type in the currently calling assembly.
        /// </summary>
        /// <param name="abstraction">The type implemented by all classes.</param>
        /// <returns>A collection of types.</returns>
        /// <exception cref="ArgumentException">Occurs when <paramref name="abstraction"/> is not an abstraction.</exception>
        public static IEnumerable<Type> GetConcreteTypesOf(Type abstraction)
            => GetConcreteTypesOf(Assembly.GetCallingAssembly(), abstraction);

        /// <summary>
        /// Gets a collection of all concrete classes of the specified type in the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to get the types from.</param>
        /// <param name="abstraction">The type implemented by all classes.</param>
        /// <returns>A collection of types.</returns>
        /// <exception cref="ArgumentException">Occurs when <paramref name="abstraction"/> is not an abstraction.</exception>
        public static IEnumerable<Type> GetConcreteTypesOf(Assembly assembly, Type abstraction)
        {
            return (!abstraction.IsAbstract && !abstraction.IsInterface)
                ? throw new ArgumentException("Type must be an interface or an abstract class.", nameof(abstraction))
                : assembly.GetTypes()                       // Get all types in the calling assembly.
                    .Where(type =>
                        abstraction.IsAssignableFrom(type)  // Filter to find any concrete type that can be assigned to the specified abstraction
                        && !type.IsInterface
                        && !type.IsAbstract
                        && !type.IsNested
                );
        }

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
        /// Gets a collection of assemblies from the cogs directory
        /// </summary>
        /// <remarks>
        /// This method assumes all assemblies have AkkoBot as a dependency reference and
        /// contain commands that can be registered on CommandsNext.
        /// </remarks>
        /// <returns>A collection of assemblies.</returns>
        internal static IEnumerable<Assembly> GetCogAssemblies()
        {
            // Create directory if it doesn't exist already.
            if (!Directory.Exists(AkkoEnvironment.CogsDirectory))
                Directory.CreateDirectory(AkkoEnvironment.CogsDirectory);

            // Get all cogs from the cogs directory
            return Directory.EnumerateFiles(AkkoEnvironment.CogsDirectory, "*.dll", SearchOption.AllDirectories)
                .Select(filePath => Assembly.LoadFrom(filePath));
        }

        /// <summary>
        /// Gets all cog setups from the loaded cogs.
        /// </summary>
        /// <returns>A collection of cog setups.</returns>
        internal static IEnumerable<ICogSetup> GetCogSetups()
        {
            return GetCogAssemblies()
                .SelectMany(x => GetConcreteTypesOf(x, typeof(ICogSetup)))
                .Select(x => Activator.CreateInstance(x) as ICogSetup);
        }

        /// <summary>
        /// Gets all cog setups from the specified assembly.
        /// </summary>
        /// <param name="assembly">The assembly to get the cog setups from.</param>
        /// <returns>A collection of cog setups.</returns>
        internal static IEnumerable<ICogSetup> GetCogSetups(Assembly assembly)
        {
            return GetConcreteTypesOf(assembly, typeof(ICogSetup))
                .Select(x => Activator.CreateInstance(x) as ICogSetup);
        }

        /// <summary>
        /// Gets the localized Discord message.
        /// </summary>
        /// <param name="ioc">The IoC container.</param>
        /// <param name="message">The message.</param>
        /// <param name="sid">The ID of the Discord guild or <see langword="null"/> if it's a direct message.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the context ErrorColor, <see langword="false"/> for OkColor.</param>
        /// <returns>The localized message.</returns>
        internal static SerializableDiscordMessage GetLocalizedMessage(IServiceProvider ioc, SerializableDiscordMessage message, ulong? sid, bool isError)
        {
            // Get the message settings (guild or dm)
            var settings = GetMessageSettings(ioc, sid);
            var localizer = ioc.GetRequiredService<ILocalizer>();

            message.WithLocalization(localizer, settings.Locale, (isError) ? settings.ErrorColor : settings.OkColor);

            return (settings.UseEmbed)
                ? message
                : new SerializableDiscordMessage() { Content = message.Deconstruct() };
        }

        /// <summary>
        /// Gets the localized Discord interaction response.
        /// </summary>
        /// <param name="ioc">The IoC container.</param>
        /// <param name="response">The response.</param>
        /// <param name="sid">The ID of the Discord guild or <see langword="null"/> if it's a direct message.</param>
        /// <param name="isError"><see langword="true"/> if the embeds should contain the context ErrorColor, <see langword="false"/> for OkColor.</param>
        /// <returns>The localized response.</returns>
        internal static DiscordInteractionResponseBuilder GetLocalizedMessage(IServiceProvider ioc, DiscordInteractionResponseBuilder response, ulong? sid, bool isError)
        {
            // Get the message settings (guild or dm)
            var settings = GetMessageSettings(ioc, sid);
            var localizer = ioc.GetRequiredService<ILocalizer>();

            response.WithLocalization(localizer, settings.Locale, (isError) ? settings.ErrorColor : settings.OkColor);

            return (settings.UseEmbed)
                ? response
                : new DiscordInteractionResponseBuilder() { Content = response.Deconstruct() };
        }

        /// <summary>
        /// Localizes a message and generates pages of it.
        /// </summary>
        /// <param name="settings">This message settings.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="input">The string to be split across the description of multiple embeds.</param>
        /// <param name="embed">The embed to be used as a template.</param>
        /// <param name="maxLength">Maximum amount of characters in each embed description.</param>
        /// <param name="content">The content outside the embed.</param>
        /// <remarks>The only thing that changes across the pages is the description.</remarks>
        /// <returns>A collection of paginable embeds.</returns>
        internal static IEnumerable<Page> GenerateLocalizedPages(IMessageSettings settings, ILocalizer localizer, string input, SerializableDiscordEmbed embed, int maxLength, string content)
        {
            if (content is not null)
                content = localizer.GetResponseString(settings.Locale, content);

            var amount = input.Length / maxLength;
            var inputLength = input.Length;

            var result = new List<Page>();
            embed.WithLocalization(localizer, settings.Locale, settings.OkColor);
            var footerPrepend = localizer.GetResponseString(settings.Locale, "pages");

            for (var counter = 0; inputLength > 0;)
            {
                var embedCopy = embed.Build();
                embedCopy.Description = input.Substring(counter++ * maxLength, Math.Min(inputLength, maxLength));

                if (embedCopy?.Footer is null)
                    embedCopy?.WithFooter(string.Format(footerPrepend, counter, amount));
                else
                    embedCopy.WithFooter(string.Format(footerPrepend + " | ", counter, amount) + embedCopy.Footer.Text);

                result.Add(new Page(content, embedCopy));
                inputLength -= maxLength;
            }

            return result;
        }

        /// <summary>
        /// Localizes a message and generates pages of it.
        /// </summary>
        /// <param name="settings">This message settings.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="embed">The embed to create pages from.</param>
        /// <param name="maxFields">The maximum amount of fields each page is allowed to have.</param>
        /// <remarks>The only thing that changes across the pages are its embed fields.</remarks>
        /// <returns>A collection of paginable embeds.</returns>
        internal static IEnumerable<Page> GenerateLocalizedPagesByFields(IMessageSettings settings, ILocalizer localizer, SerializableDiscordEmbed embed, int maxFields, string content)
        {
            if (content is not null)
                content = localizer.GetResponseString(settings.Locale, content);

            var result = new List<Page>();
            var splitFields = embed.Fields.SplitInto(maxFields).ToArray();

            embed.WithLocalization(localizer, settings.Locale, settings.OkColor);
            var footerPrepend = localizer.GetResponseString(settings.Locale, "pages");
            var counter = 0;

            foreach (var fields in splitFields)
            {
                var embedCopy = embed.Build(fields);

                if (embedCopy?.Footer is null)
                    embedCopy?.WithFooter(string.Format(footerPrepend, ++counter, splitFields.Length));
                else
                    embedCopy.WithFooter(string.Format(footerPrepend + " | ", ++counter, splitFields.Length) + embedCopy.Footer.Text);

                result.Add(new Page(content, embedCopy));
            }

            return result;
        }

        /// <summary>
        /// Localizes a message and generates pages of it.
        /// </summary>
        /// <param name="settings">This message settings.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="embed">The embed to create pages from.</param>
        /// <param name="fields">The fields to be added to a page message.</param>
        /// <param name="maxFields">The maximum amount of fields each page is allowed to have.</param>
        /// <param name="content">The content outside the embed.</param>
        /// <remarks>The only thing that changes across the pages are its embed fields.</remarks>
        /// <returns>A collection of paginable embeds.</returns>
        internal static IEnumerable<Page> GenerateLocalizedPagesByFields(IMessageSettings settings, ILocalizer localizer, SerializableDiscordEmbed embed, IEnumerable<SerializableEmbedField> fields, int maxFields, string content)
        {
            if (content is not null)
                content = localizer.GetResponseString(settings.Locale, content);

            var result = new List<Page>();
            var serializableFields = fields.ToArray();
            embed.ClearFields();
            embed.WithLocalization(localizer, settings.Locale, settings.OkColor);
            var footerPrepend = localizer.GetResponseString(settings.Locale, "pages");

            for (int counter = 1, index = 0, footerCounter = 0; index < serializableFields.Length; counter++)
            {
                var embedCopy = embed.Build();

                while (index < maxFields * counter && index != serializableFields.Length)
                    embedCopy?.AddLocalizedField(localizer, settings.Locale, serializableFields[index].Title, serializableFields[index].Text, serializableFields[index++].Inline);

                if (embedCopy?.Footer is null)
                    embedCopy?.WithFooter(string.Format(footerPrepend, ++footerCounter, Math.Ceiling((double)serializableFields.Length / maxFields)));
                else
                    embedCopy.WithFooter(string.Format(footerPrepend + " | ", ++footerCounter, Math.Ceiling((double)serializableFields.Length / maxFields)) + embedCopy.Footer.Text);

                result.Add(new Page(content, embedCopy));
            }

            return result;
        }

        /// <summary>
        /// Converts a collection of embed pages to a collection of page content.
        /// </summary>
        /// <param name="pages">The pages to be converted.</param>
        /// <returns>A collection of pages whose embed is <see langword="null"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IEnumerable<Page> ConvertToContentPages(IEnumerable<Page> pages)
        {
            foreach (var page in pages)
            {
                page.Content += ("\n\n" + page.Embed.Deconstruct()).MaxLength(AkkoConstants.MaxMessageLength - page.Content?.Length ?? 0);
                page.Embed = null;
            }

            return pages;
        }

        /// <summary>
        /// Gets the message settings of the current context.
        /// </summary>
        /// <param name="ioc">The IoC container.</param>
        /// <param name="sid">The ID of the Discord guild or <see langword="null"/> if it's a direct message.</param>
        /// <returns>The message settings for the given context.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IMessageSettings GetMessageSettings(IServiceProvider ioc, ulong? sid)
        {
            var dbCache = ioc.GetRequiredService<IDbCache>();

            // Get the message settings (guild or dm)
            return (dbCache.Guilds.TryGetValue(sid ?? default, out var dbGuild))
                ? dbGuild
                : ioc.GetRequiredService<BotConfig>();
        }
    }
}