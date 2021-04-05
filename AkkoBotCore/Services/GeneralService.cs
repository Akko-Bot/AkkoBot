using AkkoBot.Common;
using AkkoBot.Credential;
using AkkoBot.Extensions;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AkkoBot.Services
{
    public static class GeneralService
    {
        /// <summary>
        /// Checks if the specified <paramref name="id"/> is registered as a bot owner.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="id">The user ID to be checked.</param>
        /// <returns><see langword="true"/> if the user is a bot owner, <see langword="false"/> otherwise.</returns>
        public static bool IsOwner(CommandContext context, ulong id)
            => context.Client.CurrentApplication.Owners.Any(x => x.Id == id) || context.Services.GetService<Credentials>().OwnerIds.Contains(id);

        /// <summary>
        /// Gets a collection of all classes of the specified type in the AkkoBot namespace.
        /// </summary>
        /// <param name="abstraction">The type implemented by all classes.</param>
        /// <returns>A collection of types.</returns>
        public static IEnumerable<Type> GetImplementables(Type abstraction)
        {
            return AppDomain.CurrentDomain.GetAssemblies()          // Get all assemblies associated with the project
                .SelectMany(assemblies => assemblies.GetTypes())    // Get all the types in those assemblies
                .Where(types =>
                    abstraction.IsAssignableFrom(types)             // Filter to find any concrete type that can be assigned to the specified abstraction
                    && !types.IsInterface
                    && !types.IsAbstract
                    && !types.IsNested
                    && types.Namespace.Contains("AkkoBot")
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
        public static IEnumerable<Assembly> GetCogs()
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
        /// Localizes the content of an embed to its corresponding response string(s).
        /// </summary>
        /// <param name="localizer">The response strings cache.</param>
        /// <param name="embed">Embed to be localized.</param>
        /// <param name="locale">Locale to localize to.</param>
        /// <param name="okColor">OkColor to set the embed to, if it doesn't have one already.</param>
        /// <param name="errorColor">ErrorColor to set the embed to, if it doesn't have one already.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild OkColor, <see langword="false"/> for ErrorColor.</param>
        /// <remarks>It ignores strings that don't match any key for a response string.</remarks>
        /// <returns>The localized embed or <see langword="null"/> if the embed is null.</returns>
        internal static DiscordEmbedBuilder LocalizeEmbed(ILocalizer localizer, IMessageSettings settings, DiscordEmbedBuilder embed, bool isError = false)
        {
            if (embed is null)
                return null;

            if (embed.Title is not null)
                embed.Title = GetLocalizedResponse(localizer, settings.Locale, embed.Title);

            if (embed.Description is not null)
                embed.Description = GetLocalizedResponse(localizer, settings.Locale, embed.Description);

            if (!embed.Color.HasValue)
                embed.Color = new DiscordColor((isError) ? settings.ErrorColor : settings.OkColor);

            if (embed.Author is not null)
                embed.Author.Name = GetLocalizedResponse(localizer, settings.Locale, embed.Author.Name);

            if (embed.Footer is not null)
                embed.Footer.Text = GetLocalizedResponse(localizer, settings.Locale, embed.Footer.Text);

            foreach (var field in embed.Fields)
            {
                field.Name = GetLocalizedResponse(localizer, settings.Locale, field.Name);
                field.Value = GetLocalizedResponse(localizer, settings.Locale, field.Value);
            }

            return embed;
        }

        /// <summary>
        /// Converts all text content from an embed into a string.
        /// </summary>
        /// <param name="embed">Embed to be deconstructed.</param>
        /// <remarks>It ignores image links, except for the one on the image field.</remarks>
        /// <returns>A formatted string with the contents of the embed or <see langword="null"/> if the embed is null.</returns>
        internal static string DeconstructEmbed(DiscordEmbedBuilder embed)
            => DeconstructEmbed(embed.Build());

        /// <summary>
        /// Converts all text content from an embed into a string.
        /// </summary>
        /// <param name="embed">Embed to be deconstructed.</param>
        /// <remarks>It ignores image links, except for the one on the image field.</remarks>
        /// <returns>A formatted string with the contents of the embed or <see langword="null"/> if the embed is null.</returns>
        internal static string DeconstructEmbed(DiscordEmbed embed)
        {
            if (embed is null)
                return null;

            var dEmbed = new StringBuilder(
                ((embed.Author is null) ? string.Empty : embed.Author.Name + "\n\n") +
                ((embed.Title is null) ? string.Empty : Formatter.Bold(embed.Title) + "\n") +
                ((embed.Description is null) ? string.Empty : embed.Description + "\n\n")
            );

            if (embed.Fields.Count != 0)
                dEmbed.Append(DeconstructEmbedFields(embed.Fields));

            dEmbed.Append(
                ((embed.Image is null) ? string.Empty : $"{embed.Image.Url}\n\n") +
                ((embed.Footer is null) ? string.Empty : embed.Footer.Text + "\n") +
                ((embed.Timestamp is null) ? string.Empty : embed.Timestamp.ToString())
            );

            return dEmbed.ToString();
        }

        /// <summary>
        /// Gets the localized response string.
        /// </summary>
        /// <param name="localizer">The response strings cache.</param>
        /// <param name="locale">The locale of the response string.</param>
        /// <param name="sample">The key of the response string.</param>
        /// <returns>The localized response string. If it does not exist, returns <paramref name="sample"/>.</returns>
        internal static string GetLocalizedResponse(ILocalizer localizer, string locale, string sample)
        {
            return (sample is not null && localizer.ContainsResponse(locale, sample))
                ? localizer.GetResponseString(locale, sample)
                : sample ?? string.Empty;
        }

        /// <summary>
        /// Gets the localized Discord message.
        /// </summary>
        /// <param name="context">The command context.</param>
        /// <param name="message">The message content.</param>
        /// <param name="embed">The message embed.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild OkColor, <see langword="false"/> for ErrorColor.</param>
        /// <returns>The localized message content, embed, and the message settings.</returns>
        internal static (string, DiscordEmbedBuilder, IMessageSettings) GetLocalizedMessage(CommandContext context, string message, DiscordEmbedBuilder embed, bool isError)
        {
            var dbCache = context.Services.GetService<IDbCacher>();
            var localizer = context.Services.GetService<ILocalizer>();

            // Get the message settings (guild or dm)
            IMessageSettings settings = (dbCache.Guilds.TryGetValue(context.Guild?.Id ?? default, out var dbGuild))
                ? dbGuild
                : dbCache.BotConfig;

            var responseString = GetLocalizedResponse(localizer, settings.Locale, message);  // Localize the content message, if there is one
            var localizedEmbed = LocalizeEmbed(localizer, settings, embed, isError);         // Localize the embed message

            return (responseString, localizedEmbed, settings);
        }

        /// <summary>
        /// Extracts the contents of embed fields into a formatted code block.
        /// </summary>
        /// <param name="originalFields">The collection of embed fields.</param>
        /// <returns>The formatted content of the fields.</returns>
        private static string DeconstructEmbedFields(IEnumerable<DiscordEmbedField> originalFields)
        {
            // Redistribute the fields into groups based on their inline property
            var sisterFields = new List<List<DiscordEmbedField>> { new List<DiscordEmbedField>() };
            int sisterGroup = 0, inlinedEmbeds = 0;

            // Build the groups
            foreach (var field in originalFields)
            {
                if (!field.Inline || ++inlinedEmbeds > 3)
                {
                    sisterFields.Add(new List<DiscordEmbedField>());
                    sisterGroup++;
                    inlinedEmbeds = 0; // Don't have more than 3 fields
                }

                sisterFields[sisterGroup].Add(field);
            }

            // Extract the contents
            var result = new StringBuilder();

            foreach (var fieldGroup in sisterFields)
            {
                if (fieldGroup.Count > 1)
                    result.AppendLine(Formatter.BlockCode(ExtractInLineFields(fieldGroup)));
                else
                {
                    foreach (var field in fieldGroup)
                    {
                        result.AppendLine(
                            Formatter.BlockCode(
                                $"|{field.Name.HardPad(field.Name.Length + 2)}|\n" +
                                new string('-', field.Name.Length + 4) + '\n' +
                                field.Value
                            )
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
        private static string ExtractInLineFields(IEnumerable<DiscordEmbedField> fields)
        {
            // Extract the content of the fields
            var result = new StringBuilder();

            // Get the names and values of the grouped fields
            var names = fields.Select(x => x.Name).ToArray();
            var namesLengthCounter = 0;

            var values = fields
                .Select(x => x.Value.Split('\n'))
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