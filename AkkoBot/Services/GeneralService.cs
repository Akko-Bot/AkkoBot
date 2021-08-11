using AkkoBot.Common;
using AkkoBot.Models.Serializable;
using AkkoBot.Models.Serializable.EmbedParts;
using AkkoBot.Services.Caching.Abstractions;
using AkkoBot.Services.Localization;
using AkkoBot.Services.Localization.Abstractions;
using AkkoCore.Abstractions;
using AkkoCore.Common;
using AkkoCore.Config;
using AkkoCore.Extensions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AkkoBot.Services
{
    /// <summary>
    /// Groups utility methods for various purposes.
    /// </summary>
    public static class GeneralService
    {
        private static readonly string[] _newlines = new string[] { "\n", Environment.NewLine };

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
        /// Gets a collection of all concrete classes of the specified type in the AkkoBot namespace.
        /// </summary>
        /// <param name="abstraction">The type implemented by all classes.</param>
        /// <returns>A collection of types.</returns>
        /// <exception cref="ArgumentException">Occurs when <paramref name="abstraction"/> is not an abstraction.</exception>
        internal static IEnumerable<Type> GetConcreteTypesOf(Type abstraction)
        {
            // TODO: add overload with namespace filtering
            if (!abstraction.IsAbstract || !abstraction.IsInterface)
                throw new ArgumentException("Type must be an interface or an abstract class.", nameof(abstraction));

            return AppDomain.CurrentDomain.GetAssemblies()          // Get all assemblies associated with the project
                .SelectMany(assembly => assembly.GetTypes())        // Get all the types in those assemblies
                .Where(type =>
                    abstraction.IsAssignableFrom(type)              // Filter to find any concrete type that can be assigned to the specified abstraction
                    && !type.IsInterface
                    && !type.IsAbstract
                    && !type.IsNested
                    && type.Namespace.Contains("AkkoBot")
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
            return Directory.EnumerateFiles(AkkoEnvironment.CogsDirectory)
                .Where(filePath => filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Select(filePath => Assembly.LoadFrom(filePath));
        }

        /// <summary>
        /// Gets the localized Discord message.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="embed">The message embed.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
        /// <returns>The localized message content, embed, and the message settings.</returns>
        internal static (SerializableDiscordMessage, IMessageSettings) GetLocalizedMessage(CommandContext context, SerializableDiscordMessage embed, bool isError)
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

        /// <summary>
        /// Extracts the contents of embed fields into a formatted code block.
        /// </summary>
        /// <param name="originalFields">The collection of embed fields.</param>
        /// <param name="inlineLimit">Defines how many inline fields should be allowed on a single line. Set to 0 to disable.</param>
        /// <returns>The formatted content of the fields.</returns>
        internal static string DeconstructEmbedFields(IEnumerable<SerializableEmbedField> originalFields, int inlineLimit = 0)
        {
            // Redistribute the fields into groups based on their inline property
            var sisterFields = new List<List<SerializableEmbedField>> { new List<SerializableEmbedField>() };
            int sisterGroup = 0, inlinedEmbeds = 0;

            // Build the groups
            foreach (var field in originalFields)
            {
                if (!field.Inline || (inlineLimit > 0 && ++inlinedEmbeds > inlineLimit))
                {
                    sisterFields.Add(new List<SerializableEmbedField>());
                    sisterGroup++;
                    inlinedEmbeds = 1; // Reset limit for the new line
                }

                sisterFields[sisterGroup].Add(field);
            }

            // Extract the contents
            var result = new StringBuilder();

            foreach (var fieldGroup in sisterFields)
            {
                if (fieldGroup.Count > 1)
                    result.AppendLine(ExtractInLineFields(fieldGroup));
                else
                {
                    foreach (var field in fieldGroup)
                    {
                        result.AppendLine(
                            $"|{field.Title.HardPad(field.Title.Length + 2)}|\n" +
                            new string('-', field.Title.Length + 4) + '\n' +
                            field.Text
                        );
                    }
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// Gets the content of a group of embed fields.
        /// </summary>
        /// <param name="fields">A collection of embed fields.</param>
        /// <returns>The formatted content of all fields.</returns>
        private static string ExtractInLineFields(IEnumerable<SerializableEmbedField> fields)
        {
            // Extract the content of the fields
            var result = new StringBuilder();

            // Get the names and values of the grouped fields
            var names = fields.Select(x => x.Title).ToArray();
            var namesLengthCounter = 0;

            var values = fields
                .Select(x => x.Text.Split(_newlines, StringSplitOptions.None))
                .Fill(string.Empty)
                .Select(x =>
                {
                    var maxLength = Math.Max(x.MaxElementLength(), names[namesLengthCounter++].Length);
                    return x.Select(x => x.HardPad(maxLength + 2)).ToArray();
                }).ToArray();

            var valueLines = new List<string>(values.Length);
            var counter = 0;

            // Format the values
            for (int index = 0, totalIterations = 0; totalIterations < values.Length * values[0].Length; totalIterations++)
            {
                if (counter < names.Length - 1)
                {
                    // If value is not the last in the line
                    valueLines.Add(values[counter++][index]);
                }
                else
                {
                    // If value is the last in the line
                    valueLines.Add(values[counter][index++] + "|\n");
                    counter = 0;
                }
            }

            // Format the header
            for (var index = 0; index < names.Length; index++)
            {
                var toPad = values[index].MaxElementLength();
                if (names[index].Length < toPad)
                    names[index] = names[index].HardPad(toPad);
            }

            // Get the total length of the table
            var totalLength = 1;
            foreach (var column in values)
                totalLength += column.MaxElementLength();

            // Assemble the field string
            result.Append('|');                   // Add the first |
            result.AppendJoin("|", names);        // Add the table's header
            result.AppendLine("|\n" + new string('-', totalLength + values.Length)); // Add header separator
            result.Append('|');                   // Add the first | for the values
            result.AppendJoin("|", valueLines);   // Add the values

            return result.ToString();
        }
    }
}