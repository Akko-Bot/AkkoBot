using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Config.Abstractions;
using AkkoCore.Config.Models;
using AkkoCore.Models.Serializable;
using AkkoCore.Services;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AkkoCore.Extensions;

public static class InteractionContextExt
{
    /// <summary>
    /// Sends a localized interaction response to the user.
    /// </summary>
    /// <param name="context">This interaction context.</param>
    /// <param name="embed">The embed to be sent.</param>
    /// <param name="isEphemeral"><see langword="true"/> if only the user should be able to see the message, <see langword="false"/> otherwise.</param>
    /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
    public static Task RespondLocalizedAsync(this InteractionContext context, SerializableDiscordEmbed embed, bool isEphemeral = true, bool isError = false)
        => RespondLocalizedAsync(context, new SerializableDiscordMessage().AddEmbed(embed), isEphemeral, isError);

    /// <summary>
    /// Sends a localized interaction response to the user.
    /// </summary>
    /// <param name="context">This interaction context.</param>
    /// <param name="message">The message to be sent.</param>
    /// <param name="isEphemeral"><see langword="true"/> if only the user should be able to see the message, <see langword="false"/> otherwise.</param>
    /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
    public static Task RespondLocalizedAsync(this InteractionContext context, SerializableDiscordMessage message, bool isEphemeral = true, bool isError = false)
    {
        var localizedMessage = AkkoUtilities.GetLocalizedMessage(context.Services, message, context.Guild?.Id, isError);
        var response = new DiscordInteractionResponseBuilder(localizedMessage) { IsEphemeral = isEphemeral };

        return context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, response);
    }

    /// <summary>
    /// Sends a localized interaction response to the user.
    /// </summary>
    /// <param name="context">This interaction context.</param>
    /// <param name="response">The response to be sent.</param>
    /// <param name="isError"><see langword="true"/> if the embed should contain the guild ErrorColor, <see langword="false"/> for OkColor.</param>
    public static Task RespondLocalizedAsync(this InteractionContext context, DiscordInteractionResponseBuilder response, bool isError = false)
    {
        var localizedResponse = AkkoUtilities.GetLocalizedMessage(context.Services, response, context.Guild?.Id, isError);
        return context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, localizedResponse);
    }

    /// <summary>
    /// Sends a localized, paginated response to the context that triggered the command.
    /// </summary>
    /// <param name="context">This interaction context.</param>
    /// <param name="embed">The embed to be split into multiple pages.</param>
    /// <param name="maxFields">The maximum amount of fields each page is allowed to have.</param>
    /// <param name="isEphemeral">Whether the message should be ephemeral or not.</param>
    /// <param name="content">The content outside the embed.</param>
    public static async Task RespondPaginatedByFieldsAsync(this InteractionContext context, SerializableDiscordEmbed embed, int maxFields = 3, bool isEphemeral = true, string? content = default)
    {
        if (embed.Fields is null || embed.Fields.Count <= maxFields)
        {
            await context.RespondLocalizedAsync(embed, isEphemeral);
            return;
        }

        var settings = context.GetMessageSettings();
        var localizer = context.Services.GetRequiredService<ILocalizer>();
        var pages = (settings.UseEmbed)
            ? AkkoUtilities.GenerateLocalizedPagesByFields(settings, localizer, embed, embed.Fields, maxFields, content)
            : AkkoUtilities.ConvertToContentPages(AkkoUtilities.GenerateLocalizedPagesByFields(settings, localizer, embed, maxFields, content));

        await context.Client.GetInteractivity().SendPaginatedResponseAsync(
            context.Interaction,
            isEphemeral,
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
    /// <param name="context">This interaction context.</param>
    /// <param name="key">The key for the response string.</param>
    /// <param name="args">Variables to be included into the formatted response string.</param>
    /// <returns>A formatted and localized response string.</returns>
    public static string FormatLocalized(this InteractionContext context, string key, params object[] args)
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
    /// <param name="context">This interaction context.</param>
    /// <returns>The message settings.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IMessageSettings GetMessageSettings(this InteractionContext context)
    {
        return (context.Guild is null)
            ? context.Services.GetRequiredService<BotConfig>()
            : context.Services.GetRequiredService<GuildConfigService>().GetGuildSettings(context.Guild);
    }
}