using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AkkoCore.Extensions
{
    public static class DiscordInteractionResponseBuilderExt
    {
        /// <summary>
        /// Sets the interaction response to be ephemeral.
        /// </summary>
        /// <param name="response">This interaction response.</param>
        /// <returns>This interaction response.</returns>
        public static DiscordInteractionResponseBuilder AsEphemeral(this DiscordInteractionResponseBuilder response)
            => response.AsEphemeral(true);

        /// <summary>
        /// Localizes the content of this message.
        /// </summary>
        /// <param name="response">This interaction response.</param>
        /// <param name="localizer">The cache of response strings.</param>
        /// <param name="locale">The locale to be used.</param>
        /// <param name="color">A hexadecimal color to set the embeds if they don't have one.</param>
        /// <returns>A new message builder with its content localized.</returns>
        public static DiscordInteractionResponseBuilder WithLocalization(this DiscordInteractionResponseBuilder response, ILocalizer localizer, string locale, string color = default)
        {
            if (!string.IsNullOrWhiteSpace(response.Content))
                response.Content = localizer.GetResponseString(locale, response.Content);

            if (response.Embeds.Count is not 0 && response.Embeds is List<DiscordEmbed> embeds)
            {
                var newEmbeds = response.Embeds
                    .Select(embed => embed.WithLocalization(localizer, locale, color))
                    .ToArray();

                embeds.Clear();
                response.AddEmbeds(newEmbeds);
            }

            return response;
        }

        /// <summary>
        /// Converts all text content from this response into a string.
        /// </summary>
        /// <param name="response">This interaction response.</param>
        /// <returns>A formatted string with the contents of this response.</returns>
        public static string Deconstruct(this DiscordInteractionResponseBuilder response)
        {
            var dEmbed = new StringBuilder(((response.Content is null) ? string.Empty : response.Content + "\n\n"));

            foreach (var embed in response.Embeds ?? Enumerable.Empty<DiscordEmbed>())
                dEmbed = embed.Deconstruct(dEmbed);

            return dEmbed.ToString();
        }
    }
}