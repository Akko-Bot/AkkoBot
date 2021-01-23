using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Database.Entities;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace AkkoBot.Extensions
{
    public static class CommandContextExt
    {
        /// <summary>
        /// Sends a localized Discord message to the context that triggered the command.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="embed">The embed to be sent.</param>
        /// <param name="isMarked"><see langword="true"/> if the message should be marked with the full name of the user who ran the command, <see langword="false"/> otherwise.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild OkColor, <see langword="false"/> for ErrorColor.</param>
        /// <returns></returns>
        public static async Task RespondLocalizedAsync(this CommandContext context, DiscordEmbedBuilder embed, bool isMarked = true, bool isError = false)
            => await RespondLocalizedAsync(context, null, embed, isMarked, isError);

        /// <summary>
        /// Sends a localized Discord message to the context that triggered the command.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="message">The message content.</param>
        /// <param name="embed">The embed to be sent.</param>
        /// <param name="isMarked"><see langword="true"/> if the message should be marked with the full name of the user who ran the command, <see langword="false"/> otherwise.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild OkColor, <see langword="false"/> for ErrorColor.</param>
        /// <returns></returns>
        public static async Task RespondLocalizedAsync(this CommandContext context, string message, DiscordEmbedBuilder embed, bool isMarked = true, bool isError = false)
        {
            using var scope = context.CommandsNext.Services.CreateScope();  // Create service scope
            var (localizer, db) = GetContextServices(scope);                // Get scoped services

            // Get the message settings (guild or dm)
            IMessageSettings settings = (context.Guild is null)
                ? db.BotConfig.GetAllSync().FirstOrDefault()
                : db.GuildConfigs.GetGuild(context.Guild.Id);

            var responseString = localizer.GetResponseString(settings.Locale, message); // Localize the content message, if there is one
            var localizedEmbed = LocalizeEmbed(localizer, settings, embed, isError);    // Localize the embed message

            if (isMarked && embed.Description is not null)   // Marks the message with the full name of the user who ran the command
                localizedEmbed.Description = localizedEmbed.Description.Insert(0, Formatter.Bold($"{context.User.GetFullname()} "));

            if (settings.UseEmbed) // Send the message
                await context.RespondAsync(responseString, false, localizedEmbed);
            else
                await context.RespondAsync(responseString + "\n\n" + DeconstructEmbed(embed));
        }

        /// <summary>
        /// Localizes a response string that contains string formatters.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="key">The key for the response string.</param>
        /// <param name="args">Variables to be included into the formatted response string.</param>
        /// <returns>A formatted and localized response string.</returns>
        public static string FormatLocalized(this CommandContext context, string key, params object[] args)
        {
            using var scope = context.Services.CreateScope();
            var (localizer, db) = GetContextServices(scope);

            var locale = (context.Guild is null)
                ? db.BotConfig.GetAllSync().FirstOrDefault().Locale
                : db.GuildConfigs.GetSync(context.Guild.Id).Locale;

            for (int index = 0; index < args.Length; index++)
            {
                if (args[index] is string)
                    args[index] = GetLocalizedResponse(localizer, locale, args[index] as string);
            }

            key = GetLocalizedResponse(localizer, locale, key);

            return string.Format(key, args);
        }

        /// <summary>
        /// Gets the scoped services needed to localize a Discord message.
        /// </summary>
        /// <param name="scope">The scoped service resolver.</param>
        /// <returns>The response strings cache and the guild settings.</returns>
        private static (ILocalizer, IUnitOfWork) GetContextServices(IServiceScope scope)
        {
            var db = scope.ServiceProvider.GetService<IUnitOfWork>();
            var localizer = scope.ServiceProvider.GetService<ILocalizer>();

            return (localizer, db);
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
        /// <returns>The localized embed.</returns>
        private static DiscordEmbedBuilder LocalizeEmbed(ILocalizer localizer, IMessageSettings settings, DiscordEmbedBuilder embed, bool isError = false)
        {
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
        /// <returns>A formatted string with the contents of the embed.</returns>
        private static string DeconstructEmbed(DiscordEmbedBuilder embed)
        {
            var dEmbed = new StringBuilder(
                ((embed.Author is null) ? string.Empty : embed.Author.Name + "\n\n") +
                ((embed.Title is null) ? string.Empty : embed.Title + "\n") +
                ((embed.Description is null) ? string.Empty : embed.Description + "\n\n")
            );

            foreach (var field in embed.Fields)
                dEmbed.AppendLine(field.Name + "\n" + field.Value + "\n");

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
        private static string GetLocalizedResponse(ILocalizer localizer, string locale, string sample)
        {
            return (localizer.ContainsResponse(locale, sample))
                ? localizer.GetResponseString(locale, sample)
                : sample;
        }
    }
}