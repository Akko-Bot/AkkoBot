using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Services;
using AkkoBot.Services.Database.Abstractions;
using AkkoBot.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
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
            var timeout = (await db.GuildConfig.GetAsync(context.Guild.Id)).InteractiveTimeout;
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
            // Get the localized message and its settings (guild or dm)
            var (responseString, localizedEmbed, settings) = GeneralService.GetLocalizedMessage(context, message, embed, isError);

            if (isMarked && !string.IsNullOrWhiteSpace(embed?.Description))   // Marks the message with the full name of the user who ran the command
                localizedEmbed.Description = localizedEmbed.Description.Insert(0, Formatter.Bold($"{context.User.GetFullname()} "));

            return settings.UseEmbed
                ? await context.RespondAsync(responseString, localizedEmbed)
                : await context.RespondAsync(responseString + "\n\n" + GeneralService.DeconstructEmbed(embed));
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
                : db.GuildConfig.GetGuild(context.Guild.Id);

            var responseString = GeneralService.GetLocalizedResponse(localizer, settings.Locale, message); // Localize the content message, if there is one
            var localizedEmbed = GeneralService.LocalizeEmbed(localizer, settings, embed, isError);    // Localize the embed message

            try
            {
                return settings.UseEmbed
                    ? await user.SendMessageAsync(responseString, localizedEmbed)
                    : await user.SendMessageAsync(responseString + "\n\n" + GeneralService.DeconstructEmbed(embed));
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
                : db.GuildConfig.GetGuild(context.Guild.Id).Locale;

            for (var index = 0; index < args.Length; index++)
            {
                if (args[index] is string)
                    args[index] = GeneralService.GetLocalizedResponse(localizer, locale, args[index] as string);
            }

            key = GeneralService.GetLocalizedResponse(localizer, locale, key);

            return string.Format(key, args);
        }

        /// <summary>
        /// Sends a localized, paginated message to the context that triggered the command.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="input">The string to be split across the description of multiple embeds.</param>
        /// <param name="embed">The embed to be used as a template.</param>
        /// <param name="maxLength">Maximum amount of characters in each embed description.</param>
        /// <param name="content">The message outside of the embed.</param>
        /// <remarks>
        /// If you want to paginate the embed fields, use
        /// <see cref="RespondPaginatedByFieldsAsync(CommandContext, DiscordEmbedBuilder, int, string)"/>
        /// instead.
        /// </remarks>
        public static async Task RespondPaginatedAsync(this CommandContext context, string input, DiscordEmbedBuilder embed, int maxLength = 500, string content = null)
        {
            if (input.Length <= maxLength)
            {
                embed.Description = input;
                await context.RespondLocalizedAsync(content, embed, false);

                return;
            }

            using var scope = context.Services.GetScopedService<IUnitOfWork>(out var db);

            // Get the message settings (guild or dm)
            IMessageSettings settings = (context.Guild is null)
                ? db.BotConfig.Cache
                : db.GuildConfig.GetGuild(context.Guild.Id);

            var pages = (settings.UseEmbed)
                ? context.GenerateLocalizedPages(input, embed, maxLength, content)
                : ConvertToContentPages(context.GenerateLocalizedPages(input, embed, maxLength, content));

            await context.Channel.SendPaginatedMessageAsync(context.Member, pages);
        }

        /// <summary>
        /// Localizes an embed and generates paginable embeds of it.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="input">The string to be split across the description of multiple embeds.</param>
        /// <param name="embed">The embed to be used as a template.</param>
        /// <param name="maxLength">Maximum amount of characters in each embed description.</param>
        /// <param name="content">The message outside of the embed.</param>
        /// <remarks>The only thing that changes across the pages is the description.</remarks>
        /// <returns>A collection of paginable embeds.</returns>
        private static IEnumerable<Page> GenerateLocalizedPages(this CommandContext context, string input, DiscordEmbedBuilder embed, int maxLength, string content = null)
        {
            var localizer = context.Services.GetService<ILocalizer>();
            var amount = input.Length / maxLength;
            var inputLength = input.Length;

            var result = new List<Page>();
            var (localizedMessage, localizedEmbed, settings) = GeneralService.GetLocalizedMessage(context, content, embed, false);
            var footerPrepend = GeneralService.GetLocalizedResponse(localizer, settings.Locale, "pages");

            for (var counter = 0; inputLength > 0;)
            {
                var embedCopy = localizedEmbed.DeepCopy();
                embedCopy.Description = input.Substring(counter++ * maxLength, Math.Min(inputLength, maxLength));

                if (embedCopy?.Footer is null)
                    embedCopy?.WithFooter(string.Format(footerPrepend, counter, amount));
                else
                    embedCopy.WithFooter(string.Format(footerPrepend + " | ", counter, amount) + embedCopy.Footer.Text);

                result.Add(new Page(localizedMessage, embedCopy));
                inputLength -= maxLength;
            }

            return result;
        }

        /// <summary>
        /// Sends a localized, paginated message to the context that triggered the command.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="embed">The embed to be split into multiple pages.</param>
        /// <param name="maxFields">The maximum amount of fields each page is allowed to have.</param>
        /// <param name="message">The message outside of the embed.</param>
        /// <remarks>
        /// If you want to paginate a large string in the embed description, use
        /// <see cref="RespondPaginatedAsync(CommandContext, string, DiscordEmbedBuilder, int, string)"/>
        /// instead.
        /// </remarks>
        public static async Task RespondPaginatedByFieldsAsync(this CommandContext context, DiscordEmbedBuilder embed, int maxFields = 5, string message = null)
        {
            if (embed.Fields.Count <= maxFields)
            {
                await context.RespondLocalizedAsync(message, embed, false);
                return;
            }

            using var scope = context.Services.GetScopedService<IUnitOfWork>(out var db);

            // Get the message settings (guild or dm)
            IMessageSettings settings = (context.Guild is null)
                ? db.BotConfig.Cache
                : db.GuildConfig.GetGuild(context.Guild.Id);

            var pages = (settings.UseEmbed)
                ? context.GenerateLocalizedPagesByFields(embed, maxFields, message)
                : ConvertToContentPages(context.GenerateLocalizedPagesByFields(embed, maxFields, message));

            await context.Channel.SendPaginatedMessageAsync(context.Member, pages, timeoutoverride: settings.InteractiveTimeout);
        }

        /// <summary>
        /// Localizes an embed and generates paginable embeds of it.
        /// </summary>
        /// <param name="context">This command context.</param>
        /// <param name="embed">The embed to create pages from.</param>
        /// <param name="maxFields">The maximum amount of fields a page is allowed to have.</param>
        /// <param name="content">The message outside the embed.</param>
        /// <remarks>The only thing that changes across the pages are its embed fields.</remarks>
        /// <returns>A collection of paginable embeds.</returns>
        private static IEnumerable<Page> GenerateLocalizedPagesByFields(this CommandContext context, DiscordEmbedBuilder embed, int maxFields, string content)
        {
            var localizer = context.Services.GetService<ILocalizer>();

            var result = new List<Page>();
            var sanitizedContent = content.MaxLength(AkkoEntities.MessageMaxLength);
            var splitFields = embed.Fields.SplitInto(maxFields).ToArray();

            var (localizedMessage, localizedEmbed, settings) = GeneralService.GetLocalizedMessage(context, sanitizedContent, embed, false);
            var footerPrepend = GeneralService.GetLocalizedResponse(localizer, settings.Locale, "pages");
            var counter = 0;

            foreach (var fields in splitFields)
            {
                var embedCopy = localizedEmbed?.DeepCopy(fields);

                if (embedCopy?.Footer is null)
                    embedCopy?.WithFooter(string.Format(footerPrepend, ++counter, splitFields.Length));
                else
                    embedCopy.WithFooter(string.Format(footerPrepend + " | ", ++counter, splitFields.Length) + embedCopy.Footer.Text);

                result.Add(new Page(localizedMessage, embedCopy));
            }

            return result;
        }

        /// <summary>
        /// Converts a collection of embed pages to a collection of page content.
        /// </summary>
        /// <param name="pages">The pages to be converted.</param>
        /// <returns>A collection of pages whose embed is <see langword="null"/>.</returns>
        private static IEnumerable<Page> ConvertToContentPages(IEnumerable<Page> pages)
        {
            foreach (var page in pages)
            {
                page.Content += ("\n\n" + GeneralService.DeconstructEmbed(page.Embed)).MaxLength(AkkoEntities.MessageMaxLength);
                page.Embed = null;
            }

            return pages;
        }
    }
}