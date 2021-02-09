using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using DSharpPlus;
using AkkoBot.Services;

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
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);
            var localizer = context.CommandsNext.Services.GetService<ILocalizer>();

            // Get the message settings (guild or dm)
            IMessageSettings settings = (context.Guild is null)
                ? db.BotConfig.GetAllSync().FirstOrDefault()
                : db.GuildConfig.GetGuild(context.Guild.Id);

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
    }
}