using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus.Entities;

namespace AkkoCore.Extensions
{
    public static class DiscordSelectComponentOptionExt
    {
        /// <summary>
        /// Returns a new instance of this menu option localized to the specified <paramref name="locale"/>.
        /// </summary>
        /// <param name="option">This Discord menu option.</param>
        /// <param name="localizer">The localizer.</param>
        /// <param name="locale">The locale to localize to.</param>
        /// <param name="isDefault">Defines whether this option should be the default, or <see langword="null"/> if the original value should be used.</param>
        /// <returns>A localized copy of this menu option.</returns>
        public static DiscordSelectComponentOption WithLocalization(this DiscordSelectComponentOption option, ILocalizer localizer, string locale, bool? isDefault = default)
        {
            return new(
                localizer.GetResponseString(locale, option.Label),
                option.Value,
                localizer.GetResponseString(locale, option.Description),
                isDefault ?? option.Default,
                option.Emoji
            );
        }
    }
}