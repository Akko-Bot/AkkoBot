using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YamlDotNet.Serialization;

namespace AkkoCore.Models.Serializable;

/// <summary>
/// Represents a serializable Discord message.
/// </summary>
/// <remarks>For serialization purposes, all unused properties are set to <see langword="null"/>.</remarks>
public class SerializableDiscordMessage
{
    private string? _content;

    /// <summary>
    /// The embeds contained in this message.
    /// </summary>
    /// <remarks>Only <see cref="AkkoConstants.MaxEmbedAmount"/> embeds will make it to the message.</remarks>
    public List<SerializableDiscordEmbed>? Embeds { get; private set; }

    /// <summary>
    /// Defines the rules for the mentionability of different mentions.
    /// </summary>
    [YamlIgnore, JsonIgnore]
    public IEnumerable<IMention> MentionRules { get; set; } = Mentions.All;

    /// <summary>
    /// Gets the first embed of this message, <see langword="null"/> if there isn't one.
    /// </summary>
    /// <remarks>This property is not mapped.</remarks>
    [YamlIgnore, JsonIgnore]
    public SerializableDiscordEmbed? Embed
        => (Embeds is not null && Embeds.Count is not 0) ? Embeds[0] : default;

    /// <summary>
    /// Represents the message content outside of the embed.
    /// </summary>
    public string? Content
    {
        get => _content;
        set => _content = value?.MaxLength(AkkoConstants.MaxMessageLength, AkkoConstants.EllipsisTerminator);
    }

    /// <summary>
    /// Initializes a serializable Discord message.
    /// </summary>
    public SerializableDiscordMessage()
    { }

    public SerializableDiscordMessage(DiscordMessageBuilder messageBuilder)
    {
        MentionRules = messageBuilder.Mentions;

        Content = messageBuilder.Content;
        AddEmbeds(messageBuilder.Embeds.Select(x => x.ToSerializableEmbed()));
    }

    /// <summary>
    /// Initializes a serializable Discord message.
    /// </summary>
    /// <param name="content">The content of the message.</param>
    /// <param name="embed">The message's first embed.</param>
    public SerializableDiscordMessage(string? content, SerializableDiscordEmbed? embed = default)
    {
        Content = content;

        if (embed is not null)
            AddEmbed(embed);
    }

    /// <summary>
    /// Initializes a serializable Discord message.
    /// </summary>
    /// <param name="content">The content of the message.</param>
    /// <param name="embeds">A collection of embeds to be included in the message.</param>
    public SerializableDiscordMessage(string? content, IEnumerable<SerializableDiscordEmbed> embeds)
    {
        Content = content;

        if (embeds is not null)
            AddEmbeds(embeds);
    }

    /// <summary>
    /// Initializes a serializable Discord message.
    /// </summary>
    /// <param name="embed">The message's first embed.</param>
    public SerializableDiscordMessage(SerializableDiscordEmbed embed)
    {
        if (embed is not null)
            AddEmbed(embed);
    }

    /// <summary>
    /// Initializes a serializable Discord message.
    /// </summary>
    /// <param name="embeds">A collection of embeds to be included in the message.</param>
    public SerializableDiscordMessage(IEnumerable<SerializableDiscordEmbed> embeds)
    {
        if (embeds is not null)
            AddEmbeds(embeds);
    }

    /// <summary>
    /// Sets the text outside the embed.
    /// </summary>
    /// <param name="content">The text to be displayed.</param>
    /// <returns>This message builder.</returns>
    public SerializableDiscordMessage WithContent(string content)
    {
        Content = content;
        return this;
    }

    /// <summary>
    /// Localizes the content of message.
    /// </summary>
    /// <param name="localizer">The cache of response strings.</param>
    /// <param name="locale">The locale to be used.</param>
    /// <param name="color">A hexadecimal color to set the embed if it doesn't have one.</param>
    /// <returns>This message builder.</returns>
    public SerializableDiscordMessage WithLocalization(ILocalizer localizer, string locale, string? color = default)
    {
        Content = localizer.GetResponseString(locale, Content);

        foreach (var embed in Embeds ?? Enumerable.Empty<SerializableDiscordEmbed>())
            embed.WithLocalization(localizer, locale, color);

        return this;
    }

    /// <summary>
    /// Adds the specified <paramref name="embed"/> to message builder.
    /// </summary>
    /// <param name="embed">The embed to be added.</param>
    /// <returns>This message builder.</returns>
    public SerializableDiscordMessage AddEmbed(SerializableDiscordEmbed embed)
    {
        Embeds ??= new();
        Embeds.Add(embed);

        return this;
    }

    /// <summary>
    /// Adds the specified <paramref name="embeds"/> to message builder.
    /// </summary>
    /// <param name="embeds">The embeds to be added.</param>
    /// <returns>This message builder.</returns>
    public SerializableDiscordMessage AddEmbeds(IEnumerable<SerializableDiscordEmbed> embeds)
    {
        Embeds ??= new(embeds.Count());

        foreach (var embed in embeds)
            Embeds.Add(embed);

        return this;
    }

    /// <summary>
    /// Sets the allowed mentions for the current message.
    /// </summary>
    /// <param name="mentions">The mentions to be set.</param>
    /// <returns>This message builder.</returns>
    public SerializableDiscordMessage WithMentions(IEnumerable<IMention> mentions)
    {
        MentionRules = mentions;
        return this;
    }

    /// <summary>
    /// Converts all text content from message builder into a string.
    /// </summary>
    /// <returns>A formatted string with the contents of message.</returns>
    public string Decompose()
    {
        var dEmbed = new StringBuilder(((Content is null) ? string.Empty : Content + "\n\n"));

        foreach (var embed in Embeds ?? Enumerable.Empty<SerializableDiscordEmbed>())
            dEmbed = embed.Decompose(dEmbed);

        return dEmbed.ToString();
    }

    /// <summary>
    /// Constructs the Discord message represented by this model.
    /// </summary>
    /// <returns>A <see cref="DiscordMessageBuilder"/> with the message content and the embed.</returns>
    /// <exception cref="ArgumentException">Occurs when one of the embeds' <see cref="SerializableDiscordEmbed.Color"/> is not a valid color.</exception>
    public DiscordMessageBuilder Build()
    {
        var message = new DiscordMessageBuilder() { Content = this.Content }
            .WithAllowedMentions(MentionRules);

        if (Embeds?.Count is not null and not 0)
            message.AddEmbeds(Embeds.Where(x => x.HasValidEmbed()).Select(x => x.Build()).Select(x => x!.Build()).Take(AkkoConstants.MaxEmbedAmount));

        return message;
    }

    /// <summary>
    /// Constructs the Discord webhook message represented by this model.
    /// </summary>
    /// <returns>A <see cref="DiscordWebhookBuilder"/> with the message content and the embed.</returns>
    /// <exception cref="ArgumentException">Occurs when one of the embeds' <see cref="SerializableDiscordEmbed.Color"/> is not a valid color.</exception>
    public DiscordWebhookBuilder BuildWebhookMessage()
    {
        var webhookMsg = new DiscordWebhookBuilder()
            .WithContent(Content)
            .AddMentions(MentionRules);

        if (Embeds?.Count is not null and not 0)
            webhookMsg.AddEmbeds(Embeds.Where(x => x.HasValidEmbed()).Select(x => x.Build()).Select(x => x!.Build()).Take(AkkoConstants.MaxEmbedAmount));

        return webhookMsg;
    }

    /// <summary>
    /// Constructs the Discord interactive response represented by this model.
    /// </summary>
    /// <returns>A <see cref="DiscordInteractionResponseBuilder"/> with the content and the embed.</returns>
    /// <exception cref="ArgumentException">Occurs when the embed <see cref="Color"/> is not a valid color.</exception>
    public DiscordInteractionResponseBuilder BuildInteractiveResponse()
    {
        var response = new DiscordInteractionResponseBuilder()
            .WithContent(Content)
            .AddMentions(MentionRules);

        if (Embeds?.Count is not null and not 0)
            response.AddEmbeds(Embeds.Where(x => x.HasValidEmbed()).Select(x => x.Build()!.Build()));

        return response;
    }

    /// <summary>
    /// Clears all data in embed.
    /// </summary>
    public void Clear()
    {
        Embeds?.Clear();

        _content = null;
        Embeds = null;
    }

    /* Operator Overloads */

    public static implicit operator DiscordMessageBuilder(SerializableDiscordMessage x) => x.Build();

    public static implicit operator DiscordWebhookBuilder(SerializableDiscordMessage x) => x.BuildWebhookMessage();

    public static implicit operator SerializableDiscordMessage(DiscordMessageBuilder x) => x.ToSerializableMessage();
}