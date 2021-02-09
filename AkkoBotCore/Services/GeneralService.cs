using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AkkoBot.Services
{
    public class GeneralService
    {
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

            if (embed.Url is not null)
                embed.Url = GetLocalizedResponse(localizer, settings.Locale, embed.Url);

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
        {
            if (embed is null)
                return null;

            var dEmbed = new StringBuilder(
                ((embed.Author is null) ? string.Empty : embed.Author.Name + "\n\n") +
                ((embed.Title is null) ? string.Empty : Formatter.Bold(embed.Title) + "\n") +
                ((embed.Description is null) ? string.Empty : embed.Description + "\n\n")
            );

            // var fieldNames = embed.Fields.Select(x => x.Name).ToArray();
            // var fieldValues = embed.Fields.Select(x => x.Value).ToArray();

            // for (int index = 0; index < embed.Fields.Count; index++)
            // {
            //     if (embed.Fields[index].Inline)
            //     {
            //         if (index == fieldNames.Length - 1)
            //             continue;

            //         var valueLines = fieldValues[index].Split("\n"); // 15
            //         var valueLines2 = fieldValues[index + 1].Split("\n");

            //         dEmbed.AppendLine("```");
            //         dEmbed.AppendLine($"{fieldNames[index].MaxLength(15), -15} {fieldNames[++index]}");

            //         for (int line = 0; line < valueLines.Length; line++)
            //         {
            //             dEmbed.AppendLine($"{valueLines[line].MaxLength(15), -15} {valueLines2[line]}");
            //         }

            //         dEmbed.Append("```");
            //     }
            //     else
            //     {
            //         dEmbed.AppendLine(Formatter.BlockCode(fieldNames[index] + "\n" + fieldValues[index] + "\n"));
            //     }
            // }

            foreach (var field in embed.Fields)
                dEmbed.AppendLine(Formatter.BlockCode(field.Name + "\n" + field.Value + "\n"));

            dEmbed.Append(
                ((embed.ImageUrl is null) ? string.Empty : $"{embed.ImageUrl}\n\n") +
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
    }
}