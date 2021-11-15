using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus.Entities;

namespace AkkoCore.Extensions;

public static class DiscordButtonComponentExt
{
    /// <summary>
    /// Returns a new instance of this button localized to the specified <paramref name="locale"/>.
    /// </summary>
    /// <param name="button">This Discord button.</param>
    /// <param name="localizer">The localizer.</param>
    /// <param name="locale">The locale to localize to.</param>
    /// <param name="isDisabled">Defines whether this button should be clickable or not.</param>
    /// <returns>A localized copy of this button.</returns>
    public static DiscordButtonComponent WithLocalization(this DiscordButtonComponent button, ILocalizer localizer, string locale, bool isDisabled = false)
        => new(button.Style, button.CustomId, localizer.GetResponseString(locale, button.Label), isDisabled, button.Emoji);
}