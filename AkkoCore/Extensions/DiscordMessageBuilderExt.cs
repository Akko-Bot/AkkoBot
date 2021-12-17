using AkkoCore.Common;
using AkkoCore.Config.Models;
using AkkoCore.Models.Serializable;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System.Linq;

namespace AkkoCore.Extensions;

public static class DiscordMessageBuilderExt
{
    /// <summary>
    /// Converts this builder to a serializable version of it.
    /// </summary>
    /// <param name="messageBuilder">This Discord message builder.</param>
    /// <returns>A serializable message builder.</returns>
    public static SerializableDiscordMessage ToSerializableMessage(this DiscordMessageBuilder messageBuilder)
    {
        return new SerializableDiscordMessage() { Content = messageBuilder.Content }
            .AddEmbeds(messageBuilder.Embeds.Select(x => x.ToSerializableEmbed()));
    }

    /// <summary>
    /// Appends an embed to the current message builder with the specified <paramref name="note"/>.
    /// </summary>
    /// <param name="message">This message builder.</param>
    /// <param name="channel">The channel the message is being sent to.</param>
    /// <param name="botConfig">The bot settings.</param>
    /// <param name="note">The text to be sent in the message.</param>
    /// <param name="color">The color of the embed.</param>
    /// <remarks>The note won't be appended if the message can't take more embeds.</remarks>
    /// <returns>This message with a note embed.</returns>
    public static DiscordMessageBuilder AppendDmSourceNote(this DiscordMessageBuilder message, DiscordChannel channel, BotConfig botConfig, string note, string? color = default)
    {
        if (!channel.IsPrivate || !botConfig.MarkDmsWithSource || message.Embeds.Count >= AkkoConstants.MaxEmbedAmount)
            return message;

        var dmSourceServerEmbed = new DiscordEmbedBuilder()
            .WithDescription(note);

        if (color is { Length: 6 or 7 })
            dmSourceServerEmbed.WithColor(new DiscordColor(color));

        return message.AddEmbed(dmSourceServerEmbed);
    }

    /// <summary>
    /// Appends an embed to the current message builder with the specified <paramref name="noteKey"/>.
    /// </summary>
    /// <param name="message">This message builder.</param>
    /// <param name="context">The command context.</param>
    /// <param name="channel">The channel the message is being sent to.</param>
    /// <param name="botConfig">The bot settings.</param>
    /// <param name="noteKey">The response string of the message to be sent.</param>
    /// <param name="noteArgs">The arguments of the response string, if any.</param>
    /// <remarks>The note won't be appended if the message can't take more embeds.</remarks>
    /// <returns>This message with a note embed.</returns>
    public static DiscordMessageBuilder AppendDmSourceNote(this DiscordMessageBuilder message, CommandContext context, DiscordChannel channel, BotConfig botConfig, string noteKey, params string[] noteArgs)
    {
        if (!channel.IsPrivate || !botConfig.MarkDmsWithSource || message.Embeds.Count >= AkkoConstants.MaxEmbedAmount)
            return message;

        var dmSourceServerEmbed = new DiscordEmbedBuilder()
            .WithColor(new DiscordColor(context.GetMessageSettings().OkColor))
            .WithDescription(context.FormatLocalized(noteKey, noteArgs));

        return message.AddEmbed(dmSourceServerEmbed);
    }
}