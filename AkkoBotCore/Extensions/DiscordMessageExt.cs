using AkkoBot.Services;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace AkkoBot.Extensions
{
    public static class DiscordMessageExt
    {
        /// <summary>
        /// Edits the current Discord message with a localized message.
        /// </summary>
        /// <param name="msg">This Discord Message.</param>
        /// <param name="context">The context the message is from.</param>
        /// <param name="embed">The message's new embed.</param>
        /// <param name="isMarked"><see langword="true"/> if the message should be marked with the full name of the user who ran the command, <see langword="false"/> otherwise.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild OkColor, <see langword="false"/> for ErrorColor.</param>
        /// <returns>The newly edited Discord message.</returns>
        public static async Task<DiscordMessage> ModifyLocalizedAsync(this DiscordMessage msg, CommandContext context, DiscordEmbedBuilder embed, bool isMarked = true, bool isError = false)
            => await ModifyLocalizedAsync(msg, context, null, embed, isMarked, isError);

        /// <summary>
        /// Edits the current Discord message with a localized message.
        /// </summary>
        /// <param name="msg">This Discord Message.</param>
        /// <param name="context">The context the message is from.</param>
        /// <param name="message">The message's new content.</param>
        /// <param name="embed">The message's new embed.</param>
        /// <param name="isMarked"><see langword="true"/> if the message should be marked with the full name of the user who ran the command, <see langword="false"/> otherwise.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild OkColor, <see langword="false"/> for ErrorColor.</param>
        /// <returns>The newly edited Discord message.</returns>
        public static async Task<DiscordMessage> ModifyLocalizedAsync(this DiscordMessage msg, CommandContext context, string message, DiscordEmbedBuilder embed, bool isMarked = true, bool isError = false)
        {
            var dbCache = context.Services.GetService<IDbCacher>();
            var localizer = context.Services.GetService<ILocalizer>();

            // Get the message settings (guild or dm)
            IMessageSettings settings = (dbCache.Guilds.TryGetValue(context.Guild?.Id ?? default, out var dbGuild))
                ? dbGuild
                : dbCache.BotConfig;

            // Reset the embed's current color
            if (isError)
                embed.Color = default;

            var responseString = GeneralService.GetLocalizedResponse(localizer, settings.Locale, message); // Localize the content message, if there is one
            var localizedEmbed = GeneralService.LocalizeEmbed(localizer, settings, embed, isError);    // Localize the embed message

            if (isMarked && !string.IsNullOrWhiteSpace(embed?.Description))   // Marks the message with the full name of the user who ran the command
                localizedEmbed.Description = localizedEmbed.Description.Insert(0, Formatter.Bold($"{context.User.GetFullname()} "));

            return settings.UseEmbed
                ? await msg.ModifyAsync(responseString, localizedEmbed.Build())
                : await msg.ModifyAsync(responseString + "\n\n" + GeneralService.DeconstructEmbed(embed));
        }

        /// <summary>
        /// Deletes this <see cref="DiscordMessage"/> after the specified time.
        /// </summary>
        /// <param name="message">This Discord message.</param>
        /// <param name="delay">How long to wait before the message is deleted.</param>
        public static async Task DeleteWithDelayAsync(this DiscordMessage message, TimeSpan delay)
        {
            await Task.Delay(delay).ConfigureAwait(false);
            try { await message.DeleteAsync(); } catch { }  // Message might get deleted by someone else in the meantime
        }
    }
}