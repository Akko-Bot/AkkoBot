using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
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
        /// <returns>The <see cref="DiscordMessage"/> that has been sent.</returns>
        public static async Task<DiscordMessage> RespondLocalizedAsync(this CommandContext context, DiscordEmbedBuilder embed, bool isMarked = true, bool isError = false)
            => await RespondLocalizedAsync(context, null, embed, isMarked, isError);

        /// <summary>
        /// Sends a localized interactive message to the context that triggered the command and executes a follow-up task.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="embed">The embed to be sent.</param>
        /// <param name="expectedResponse">The response that's expected from the user. It gets localized internally.</param>
        /// <param name="action">The task to be performed if the user comfirms the interaction.</param>
        /// <param name="isMarked"><see langword="true"/> if the message should be marked with the full name of the user who ran the command, <see langword="false"/> otherwise.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild OkColor, <see langword="false"/> for ErrorColor.</param>
        /// <returns>The interaction between the user and the message.</returns>
        public static async Task<InteractivityResult<DiscordMessage>> RespondInteractiveAsync(this CommandContext context, DiscordEmbedBuilder embed, string expectedResponse, Action action, bool isMarked = true, bool isError = false)
            => await RespondInteractiveAsync(context, null, embed, expectedResponse, action, isMarked, isError);

        /// <summary>
        /// Gets the guild prefix of the current context.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <returns>The prefix used in the guild.</returns>
        public static string GetGuildPrefix(this CommandContext context)
        {
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);
            return db.GuildConfigs.GetGuild(context.Guild.Id).Prefix;
        }

        /// <summary>
        /// Sends a localized interactive message to the context that triggered the command and executes a follow-up task.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="message">The message content.</param>
        /// <param name="embed">The embed to be sent.</param>
        /// <param name="expectedResponse">The response that's expected from the user. It gets localized internally.</param>
        /// <param name="action">The operations to be performed if the user comfirms the interaction.</param>
        /// <param name="isMarked"><see langword="true"/> if the message should be marked with the full name of the user who ran the command, <see langword="false"/> otherwise.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild OkColor, <see langword="false"/> for ErrorColor.</param>
        /// <returns>The interaction between the user and the message.</returns>
        public static async Task<InteractivityResult<DiscordMessage>> RespondInteractiveAsync(this CommandContext context, string message, DiscordEmbedBuilder embed, string expectedResponse, Action action, bool isMarked = true, bool isError = false)
        {
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);

            // Get the timeout
            var timeout = (await db.GuildConfigs.GetAsync(context.Guild.Id)).InteractiveTimeout;
            var globalTimeout = (await db.BotConfig.GetAllAsync()).FirstOrDefault().InteractiveTimeout;

            // Send the question
            var question = await context.RespondLocalizedAsync(message, embed, isMarked, isError);

            // Await interaction, proceed after any message no matter its content
            var result = await context.Message.GetNextMessageAsync(x => true, timeout ?? globalTimeout);

            // Delete the confirmation message
            await context.Channel.DeleteMessageAsync(question);

            // Localize the response expected from the user
            var response = context.FormatLocalized(expectedResponse);

            // If user replied with the expected response, execute the action
            if (!result.TimedOut && result.Result.Content.EqualsOrStartsWith(response))
                action();
                
            return result;
        }

        /// <summary>
        /// Sends a localized Discord message to the context that triggered the command.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="message">The message content.</param>
        /// <param name="embed">The embed to be sent.</param>
        /// <param name="isMarked"><see langword="true"/> if the message should be marked with the full name of the user who ran the command, <see langword="false"/> otherwise.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild OkColor, <see langword="false"/> for ErrorColor.</param>
        /// <returns>The <see cref="DiscordMessage"/> that has been sent.</returns>
        public static async Task<DiscordMessage> RespondLocalizedAsync(this CommandContext context, string message, DiscordEmbedBuilder embed, bool isMarked = true, bool isError = false)
        {
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);
            var localizer = context.CommandsNext.Services.GetService<ILocalizer>();

            // Get the message settings (guild or dm)
            IMessageSettings settings = (context.Guild is null)
                ? db.BotConfig.GetAllSync().FirstOrDefault()
                : db.GuildConfigs.GetGuild(context.Guild.Id);

            var responseString = GetLocalizedResponse(localizer, settings.Locale, message); // Localize the content message, if there is one
            var localizedEmbed = LocalizeEmbed(localizer, settings, embed, isError);    // Localize the embed message

            if (isMarked && !string.IsNullOrWhiteSpace(embed?.Description))   // Marks the message with the full name of the user who ran the command
                localizedEmbed.Description = localizedEmbed.Description.Insert(0, Formatter.Bold($"{context.User.GetFullname()} "));

            return settings.UseEmbed
                ? await context.RespondAsync(responseString, false, localizedEmbed)
                : await context.RespondAsync(responseString + "\n\n" + DeconstructEmbed(embed));
        }

        /// <summary>
        /// Sends a localized direct message to the specified user.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="user">The user to receive the direct message.</param>
        /// <param name="embed">The embed to be sent.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild OkColor, <see langword="false"/> for ErrorColor.</param>
        /// <returns>The <see cref="DiscordMessage"/> that has been sent, <see langword="null"/> if it failed to send the message.</returns>
        public static async Task<DiscordMessage> SendLocalizedDmAsync(this CommandContext context, DiscordMember user, DiscordEmbedBuilder embed, bool isError = false)
            => await SendLocalizedDmAsync(context, user, null, embed, isError);

        /// <summary>
        /// Sends a localized direct message to the specified user.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="userId">Discord ID of the user.</param>
        /// <param name="embed">The embed to be sent.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild OkColor, <see langword="false"/> for ErrorColor.</param>
        /// <returns>The <see cref="DiscordMessage"/> that has been sent, <see langword="null"/> if it failed to send the message.</returns>
        public static async Task<DiscordMessage> SendLocalizedDmAsync(this CommandContext context, ulong userId, DiscordEmbedBuilder embed, bool isError = false)
            => await SendLocalizedDmAsync(context, await context.Guild.GetMemberAsync(userId), null, embed, isError);

        /// <summary>
        /// Sends a localized direct message to the specified user.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="userId">Discord ID of the user.</param>
        /// <param name="message">The message content.</param>
        /// <param name="embed">The embed to be sent.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild OkColor, <see langword="false"/> for ErrorColor.</param>
        /// <returns>The <see cref="DiscordMessage"/> that has been sent, <see langword="null"/> if it failed to send the message.</returns>
        public static async Task<DiscordMessage> SendLocalizedDmAsync(this CommandContext context, ulong userId, string message, DiscordEmbedBuilder embed, bool isError = false)
            => await SendLocalizedDmAsync(context, await context.Guild.GetMemberAsync(userId), message, embed, isError);

        /// <summary>
        /// Sends a localized direct message to the specified user.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="user">The user to receive the direct message.</param>
        /// <param name="message">The message content.</param>
        /// <param name="embed">The embed to be sent.</param>
        /// <param name="isError"><see langword="true"/> if the embed should contain the guild OkColor, <see langword="false"/> for ErrorColor.</param>
        /// <returns>The <see cref="DiscordMessage"/> that has been sent, <see langword="null"/> if it failed to send the message.</returns>
        public static async Task<DiscordMessage> SendLocalizedDmAsync(this CommandContext context, DiscordMember user, string message, DiscordEmbedBuilder embed, bool isError = false)
        {
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);
            var localizer = context.CommandsNext.Services.GetService<ILocalizer>();

            // Get the message settings (guild or dm)
            IMessageSettings settings = (context.Guild is null)
                ? db.BotConfig.GetAllSync().FirstOrDefault()
                : db.GuildConfigs.GetGuild(context.Guild.Id);

            var responseString = GetLocalizedResponse(localizer, settings.Locale, message); // Localize the content message, if there is one
            var localizedEmbed = LocalizeEmbed(localizer, settings, embed, isError);    // Localize the embed message

            try
            {
                return settings.UseEmbed
                    ? await user.SendMessageAsync(responseString, false, localizedEmbed)
                    : await user.SendMessageAsync(responseString + "\n\n" + DeconstructEmbed(embed));
            }
            catch
            {
                return null;
            }
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
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);
            var localizer = context.CommandsNext.Services.GetService<ILocalizer>();

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
        private static DiscordEmbedBuilder LocalizeEmbed(ILocalizer localizer, IMessageSettings settings, DiscordEmbedBuilder embed, bool isError = false)
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
        private static string DeconstructEmbed(DiscordEmbedBuilder embed)
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
        private static string GetLocalizedResponse(ILocalizer localizer, string locale, string sample)
        {
            return (sample is not null && localizer.ContainsResponse(locale, sample))
                ? localizer.GetResponseString(locale, sample)
                : sample ?? string.Empty;
        }
    }
}