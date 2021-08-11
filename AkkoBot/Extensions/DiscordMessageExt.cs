using AkkoBot.Common;
using AkkoBot.Models.Serializable;
using AkkoBot.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AkkoBot.Extensions
{
    public static class DiscordMessageExt
    {
        /// <summary>
        /// Edits the current Discord message with a localized message.
        /// </summary>
        /// <param name="msg">This Discord message.</param>
        /// <param name="context">The context the message is from.</param>
        /// <param name="embed">The message's new embed.</param>
        /// <param name="isMarked"><see langword="true"/> if the message should be marked with the full name of the user who ran the command, <see langword="false"/> otherwise.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
        /// <returns>The newly edited Discord message.</returns>
        public static async Task<DiscordMessage> ModifyLocalizedAsync(this DiscordMessage msg, CommandContext context, SerializableDiscordMessage embed, bool isMarked = true, bool isError = false)
        {
            // Reset the embed's current color
            if (isError)
                embed.Color = default;
            var (localizedEmbed, settings) = GeneralService.GetLocalizedMessage(context, embed, isError);    // Localize the embed message

            if (isMarked && !string.IsNullOrWhiteSpace(embed?.Body?.Description))   // Marks the message with the full name of the user who ran the command
                localizedEmbed.Body.Description = localizedEmbed.Body.Description.Insert(0, Formatter.Bold($"{context.User.GetFullname()} "));

            return (settings.UseEmbed)
                ? await msg.ModifyAsync(localizedEmbed.BuildMessage())
                : await msg.ModifyAsync(localizedEmbed.Deconstruct());
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

        /// <summary>
        /// Adds multiple reactions to this message.
        /// </summary>
        /// <param name="message">This Discord message.</param>
        /// <param name="emojis">A collection of Discord emojis.</param>
        /// <remarks>There is a delay of <see cref="AkkoStatics.SafetyDelay"/> between the addition of each reaction.</remarks>
        public static async Task CreateReactionsAsync(this DiscordMessage message, params DiscordEmoji[] emojis)
        {
            foreach (var emoji in emojis)
            {
                await message.CreateReactionAsync(emoji);
                await Task.Delay(AkkoStatics.SafetyDelay);
            }
        }

        /// <summary>
        /// Adds multiple reactions to this message.
        /// </summary>
        /// <param name="message">This Discord message.</param>
        /// <param name="emojis">A collection of Discord emojis.</param>
        /// <remarks>There is a delay of <see cref="AkkoStatics.SafetyDelay"/> between the addition of each reaction.</remarks>
        public static async Task CreateReactionsAsync(this DiscordMessage message, IEnumerable<DiscordEmoji> emojis)
        {
            foreach (var emoji in emojis)
            {
                await message.CreateReactionAsync(emoji);
                await Task.Delay(AkkoStatics.SafetyDelay);
            }
        }
    }
}