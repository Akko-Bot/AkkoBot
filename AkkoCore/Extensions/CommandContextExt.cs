using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Config.Abstractions;
using AkkoCore.Config.Models;
using AkkoCore.Models.Serializable;
using AkkoCore.Models.Serializable.EmbedParts;
using AkkoCore.Services;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Kotz.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AkkoCore.Extensions;

public static class CommandContextExt
{
    /// <summary>
    /// Sends a localized interactive message to the context that triggered the command and executes a follow-up task.
    /// </summary>
    /// <param name="context">This command context.</param>
    /// <param name="embed">The embed to be sent.</param>
    /// <param name="expectedResponseKey">The response key that is expected from the user. It gets localized internally.</param>
    /// <param name="action">The operations to be performed if the user comfirms the interaction.</param>
    /// <param name="isMarked"><see langword="true"/> if the message should be marked with the full name of the user who ran the command, <see langword="false"/> otherwise.</param>
    /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
    /// <param name="content">The content outside the embed.</param>
    /// <remarks>The question message gets deleted, regardless of user input.</remarks>
    /// <returns>The interaction between the user and the message.</returns>
    public static async Task<InteractivityResult<DiscordMessage>> RespondInteractiveAsync(this CommandContext context, SerializableDiscordEmbed embed, string expectedResponseKey, Func<Task> action, bool isMarked = true, bool isError = false, string? content = default)
        => await RespondInteractiveAsync(context, new SerializableDiscordMessage(content, embed), expectedResponseKey, action, isMarked, isError);

    /// <summary>
    /// Sends a localized interactive message to the context that triggered the command and executes a follow-up task.
    /// </summary>
    /// <param name="context">This command context.</param>
    /// <param name="message">The message to be sent.</param>
    /// <param name="expectedResponseKey">The response key that is expected from the user. It gets localized internally.</param>
    /// <param name="action">The operations to be performed if the user comfirms the interaction.</param>
    /// <param name="isMarked"><see langword="true"/> if the message should be marked with the full name of the user who ran the command, <see langword="false"/> otherwise.</param>
    /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
    /// <remarks>The question message gets deleted, regardless of user input.</remarks>
    /// <returns>The interaction between the user and the message.</returns>
    public static async Task<InteractivityResult<DiscordMessage>> RespondInteractiveAsync(this CommandContext context, SerializableDiscordMessage message, string expectedResponseKey, Func<Task> action, bool isMarked = true, bool isError = false)
    {
        var dbCache = context.Services.GetRequiredService<IDbCache>();
        var settings = context.GetMessageSettings();

        // Send the question
        var question = await context.RespondLocalizedAsync(message, isMarked, isError);

        // Await interaction, proceed after any message no matter its content
        var result = await context.Message.GetNextMessageAsync(x => true, settings.InteractiveTimeout);

        // Delete the confirmation message
        await context.Channel.DeleteMessageAsync(question);

        // Localize the response expected from the user
        var response = context.FormatLocalized(expectedResponseKey);

        // If user replied with the expected response, execute the action
        if (!result.TimedOut && result.Result.Content.EqualsOrStartsWith(response, StringComparison.OrdinalIgnoreCase))
            await action();

        return result;
    }

    /// <summary>
    /// Sends a localized Discord message to the context that triggered the command.
    /// </summary>
    /// <param name="context">This command context.</param>
    /// <param name="embed">The embed to be sent.</param>
    /// <param name="isMarked"><see langword="true"/> if the message should be marked with the full name of the user who ran the command, <see langword="false"/> otherwise.</param>
    /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
    /// <param name="content">The content outside the embed.</param>
    /// <returns>The <see cref="DiscordMessage"/> that has been sent.</returns>
    public static Task<DiscordMessage> RespondLocalizedAsync(this CommandContext context, SerializableDiscordEmbed embed, bool isMarked = true, bool isError = false, string? content = default)
        => RespondLocalizedAsync(context, new SerializableDiscordMessage(content, embed), isMarked, isError);

    /// <summary>
    /// Sends a localized Discord message to the context that triggered the command.
    /// </summary>
    /// <param name="context">This command context.</param>
    /// <param name="message">The message to be sent.</param>
    /// <param name="isMarked"><see langword="true"/> if the message should be marked with the full name of the user who ran the command, <see langword="false"/> otherwise.</param>
    /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
    /// <returns>The <see cref="DiscordMessage"/> that has been sent.</returns>
    public static Task<DiscordMessage> RespondLocalizedAsync(this CommandContext context, SerializableDiscordMessage message, bool isMarked = true, bool isError = false)
    {
        var localizedMsg = AkkoUtilities.GetLocalizedMessage(context.Services, message, context.Guild?.Id, isError);

        if (isMarked && !string.IsNullOrWhiteSpace(localizedMsg.Embed?.Body?.Description))   // Marks the message with the full name of the user who ran the command
            localizedMsg.Embed.Body.Description = localizedMsg.Embed.Body.Description.Insert(0, Formatter.Bold($"{context.User.Username} "));

        return context.Channel.SendMessageAsync(localizedMsg.Build());
    }

    /// <summary>
    /// Sends a localized direct message to the specified user.
    /// </summary>
    /// <param name="context">This command context.</param>
    /// <param name="userId">Discord ID of the user.</param>
    /// <param name="embed">The embed to be sent.</param>
    /// <param name="content">The content outside the embed.</param>
    /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
    /// <returns>The <see cref="DiscordMessage"/> that has been sent, <see langword="null"/> if it failed to send the message.</returns>
    public static async Task<DiscordMessage?> SendLocalizedDmAsync(this CommandContext context, ulong userId, SerializableDiscordEmbed embed, bool isError = false, string? content = default)
        => await SendLocalizedDmAsync(context, await context.Guild.GetMemberAsync(userId), new SerializableDiscordMessage(content, embed), isError);

    /// <summary>
    /// Sends a localized direct message to the specified user.
    /// </summary>
    /// <param name="context">This command context.</param>
    /// <param name="userId">Discord ID of the user.</param>
    /// <param name="message">The message to be sent.</param>
    /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
    /// <returns>The <see cref="DiscordMessage"/> that has been sent, <see langword="null"/> if it failed to send the message.</returns>
    public static async Task<DiscordMessage?> SendLocalizedDmAsync(this CommandContext context, ulong userId, SerializableDiscordMessage message, bool isError = false)
        => await SendLocalizedDmAsync(context, await context.Guild.GetMemberAsync(userId), message, isError);

    /// <summary>
    /// Sends a localized direct message to the specified user.
    /// </summary>
    /// <param name="context">This command context.</param>
    /// <param name="user">The user to receive the direct message.</param>
    /// <param name="message">The message to be sent.</param>
    /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
    /// <returns>The <see cref="DiscordMessage"/> that has been sent, <see langword="null"/> if it failed to send the message.</returns>
    public static Task<DiscordMessage?> SendLocalizedDmAsync(this CommandContext context, DiscordMember user, SerializableDiscordMessage message, bool isError = false)
    {
        var localizedEmbed = AkkoUtilities.GetLocalizedMessage(context.Services, message, context.Guild?.Id, isError);
        return user.SendMessageSafelyAsync(localizedEmbed.Build());
    }

    /// <summary>
    /// Sends a localized, paginated message to the context that triggered the command.
    /// </summary>
    /// <param name="context">This command context.</param>
    /// <param name="input">The string to be split across the description of multiple embeds.</param>
    /// <param name="embed">The embed to be used as a template.</param>
    /// <param name="maxLength">Maximum amount of characters in each embed description.</param>
    /// <param name="content">The content outside the embed.</param>
    /// <remarks>
    /// If you want to paginate the embed fields, use
    /// <see cref="RespondPaginatedByFieldsAsync(CommandContext, SerializableDiscordEmbed, IEnumerable{SerializableEmbedField}, int, string)"/>
    /// instead.
    /// </remarks>
    public static async Task RespondPaginatedAsync(this CommandContext context, string input, SerializableDiscordEmbed embed, int maxLength = 500, string? content = default)
    {
        if (input.Length <= maxLength)
        {
            embed.WithDescription(input);
            await context.RespondLocalizedAsync(embed, false);

            return;
        }

        var settings = context.GetMessageSettings();
        var localizer = context.Services.GetRequiredService<ILocalizer>();
        var pages = (settings.UseEmbed)
            ? AkkoUtilities.GenerateLocalizedPages(settings, localizer, input, embed, maxLength, content)
            : AkkoUtilities.ConvertToContentPages(AkkoUtilities.GenerateLocalizedPages(settings, localizer, input, embed, maxLength, content));

        await context.Channel.SendPaginatedMessageAsync(
            context.User,
            pages,
            token: new CancellationTokenSource(
                settings.InteractiveTimeout
                ?? context.Services.GetRequiredService<BotConfig>().InteractiveTimeout
                ?? TimeSpan.FromSeconds(60)
            ).Token
        );
    }

    /// <summary>
    /// Sends a localized, paginated message to the context that triggered the command.
    /// </summary>
    /// <param name="context">This command context.</param>
    /// <param name="embed">The embed to be split into multiple pages.</param>
    /// <param name="maxFields">The maximum amount of fields each page is allowed to have.</param>
    /// <param name="content">The content outside the embed.</param>
    /// <remarks>
    /// If you want to paginate a large string in the embed description, use
    /// <see cref="RespondPaginatedAsync(CommandContext, string, SerializableDiscordEmbed, int, string)"/>
    /// instead.
    /// </remarks>
    public static async Task RespondPaginatedByFieldsAsync(this CommandContext context, SerializableDiscordEmbed embed, int maxFields = 3, string? content = default)
    {
        if (embed.Fields is null || embed.Fields.Count <= maxFields)
        {
            await context.RespondLocalizedAsync(embed, false);
            return;
        }

        var settings = context.GetMessageSettings();
        var localizer = context.Services.GetRequiredService<ILocalizer>();
        var pages = (settings.UseEmbed)
            ? AkkoUtilities.GenerateLocalizedPagesByFields(settings, localizer, embed, embed.Fields, maxFields, content)
            : AkkoUtilities.ConvertToContentPages(AkkoUtilities.GenerateLocalizedPagesByFields(settings, localizer, embed, maxFields, content));

        await context.Channel.SendPaginatedMessageAsync(
            context.User,
            pages,
            token: new CancellationTokenSource(
                settings.InteractiveTimeout
                ?? context.Services.GetRequiredService<BotConfig>().InteractiveTimeout
                ?? TimeSpan.FromSeconds(60)
            ).Token
        );
    }

    /// <summary>
    /// Sends a localized, paginated message to the context that triggered the command.
    /// </summary>
    /// <param name="context">This command context.</param>
    /// <param name="embed">The embed to be split into multiple pages.</param>
    /// <param name="fields">The fields to be added to a page message.</param>
    /// <param name="maxFields">The maximum amount of fields each page is allowed to have.</param>
    /// <param name="content">The content outside the embed.</param>
    /// <remarks>
    /// If you want to paginate a large string in the embed description, use
    /// <see cref="RespondPaginatedAsync(CommandContext, string, SerializableDiscordEmbed, int, string)"/>
    /// instead.
    /// </remarks>
    public static async Task RespondPaginatedByFieldsAsync(this CommandContext context, SerializableDiscordEmbed embed, IEnumerable<SerializableEmbedField> fields, int maxFields = 3, string? content = default)
    {
        if (fields.Count() <= maxFields)
        {
            foreach (var field in fields)
                embed.AddField(field.Title, field.Text, field.Inline);

            await context.RespondLocalizedAsync(embed, false);
            return;
        }
        else if (maxFields > 25)
            throw new ArgumentException("Embeds cannot have more than 25 fields.", nameof(maxFields));

        var settings = context.GetMessageSettings();
        var localizer = context.Services.GetRequiredService<ILocalizer>();
        var pages = (settings.UseEmbed)
            ? AkkoUtilities.GenerateLocalizedPagesByFields(settings, localizer, embed, fields, maxFields, content)
            : AkkoUtilities.ConvertToContentPages(AkkoUtilities.GenerateLocalizedPagesByFields(settings, localizer, embed, fields, maxFields, content));

        await context.Channel.SendPaginatedMessageAsync(
            context.User,
            pages,
            token: new CancellationTokenSource(
                settings.InteractiveTimeout
                ?? context.Services.GetRequiredService<BotConfig>().InteractiveTimeout
                ?? TimeSpan.FromSeconds(60)
            ).Token
        );
    }

    /// <summary>
    /// Localizes a response string that contains string formatters.
    /// </summary>
    /// <param name="context">This command context.</param>
    /// <param name="key">The key for the response string.</param>
    /// <param name="args">Variables to be included into the formatted response string.</param>
    /// <returns>A formatted and localized response string.</returns>
    public static string FormatLocalized(this CommandContext context, string key, params object?[] args)
    {
        var dbCache = context.Services.GetRequiredService<IDbCache>();
        var localizer = context.Services.GetRequiredService<ILocalizer>();

        var locale = (dbCache.Guilds.TryGetValue(context.Guild?.Id ?? default, out var dbGuild))
            ? dbGuild.Locale
            : context.Services.GetRequiredService<BotConfig>().Locale;

        return localizer.FormatLocalized(locale, key, args);
    }

    /// <summary>
    /// Gets the message settings for this context.
    /// </summary>
    /// <param name="context">This command context.</param>
    /// <returns>The message settings.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IMessageSettings GetMessageSettings(this CommandContext context)
    {
        return (context.Guild is null)
            ? context.Services.GetRequiredService<BotConfig>()
            : context.Services.GetRequiredService<GuildConfigService>().GetGuildSettings(context.Guild);
    }

    /// <summary>
    /// Gets the timezone of the current context.
    /// </summary>
    /// <param name="context">This command context.</param>
    /// <returns>The <see cref="TimeZoneInfo"/> associated with this context or <see cref="TimeZoneInfo.Local"/> if there isn't one.</returns>
    public static TimeZoneInfo GetTimeZone(this CommandContext context)
    {
        if (context.Guild is null)
            return TimeZoneInfo.Local;

        var timezone = context.Services.GetRequiredService<IDbCache>().Guilds[context.Guild.Id].Timezone;

        return (timezone is null)
            ? TimeZoneInfo.Local
            : AkkoUtilities.GetTimeZone(timezone, true)!;
    }
}