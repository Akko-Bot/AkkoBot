using System;
using System.Linq;
using System.Threading.Tasks;
using AkkoBot.Services;
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
            using var scope = context.CommandsNext.Services.GetScopedService<IUnitOfWork>(out var db);
            var localizer = context.CommandsNext.Services.GetService<ILocalizer>();

            // Get the message settings (guild or dm)
            IMessageSettings settings = (context.Guild is null)
                ? db.BotConfig.GetAllSync().FirstOrDefault()
                : db.GuildConfig.GetGuild(context.Guild.Id);

            var responseString = GeneralService.GetLocalizedResponse(localizer, settings.Locale, message); // Localize the content message, if there is one
            var localizedEmbed = GeneralService.LocalizeEmbed(localizer, settings, embed, isError);    // Localize the embed message

            if (isMarked && !string.IsNullOrWhiteSpace(embed?.Description))   // Marks the message with the full name of the user who ran the command
                localizedEmbed.Description = localizedEmbed.Description.Insert(0, Formatter.Bold($"{context.User.GetFullname()} "));

            return settings.UseEmbed
                ? await context.RespondAsync(responseString, false, localizedEmbed)
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
                    ? await user.SendMessageAsync(responseString, false, localizedEmbed)
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
                : db.GuildConfig.GetSync(context.Guild.Id).Locale;

            for (int index = 0; index < args.Length; index++)
            {
                if (args[index] is string)
                    args[index] = GeneralService.GetLocalizedResponse(localizer, locale, args[index] as string);
            }

            key = GeneralService.GetLocalizedResponse(localizer, locale, key);

            return string.Format(key, args);
        }

        
    }
}