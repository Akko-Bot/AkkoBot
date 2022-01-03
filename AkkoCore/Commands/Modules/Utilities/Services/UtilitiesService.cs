using AkkoCore.Commands.Attributes;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Models.Serializable;
using AkkoCore.Services;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Utilities.Services;

/// <summary>
/// Groups utility methods for the Utilities command module.
/// </summary>
[CommandService(ServiceLifetime.Singleton)]
public sealed class UtilitiesService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DiscordShardedClient _shardedClient;

    public UtilitiesService(IServiceScopeFactory scopeFactory, IHttpClientFactory httpClientFactory, DiscordShardedClient shardedClient)
    {
        _scopeFactory = scopeFactory;
        _httpClientFactory = httpClientFactory;
        _shardedClient = shardedClient;
    }

    /// <summary>
    /// Deserializes user input in Yaml to a Discord message.
    /// </summary>
    /// <param name="input">The user's input.</param>
    /// <param name="result">The deserialized input.</param>
    /// <returns><see langword="true"/> if deserialization was successful, <see langword="false"/> otherwise.</returns>
    public bool DeserializeMessage(string input, [MaybeNullWhen(false)] out DiscordMessageBuilder result)
    {
        try
        {
            result = input.FromYaml<SerializableDiscordMessage>().Build();
            return result.Content is not null || result.Embed is not null;
        }
        catch
        {
            result = default;
            return false;
        }
    }

    /// <summary>
    /// Determines whether the specified emoji can be used by the bot in the context channel.
    /// </summary>
    /// <param name="server">The Discord guild.</param>
    /// <param name="channel">The Discord channel the emoji is going to be used.</param>
    /// <param name="emoji">The emoji to be used.</param>
    /// <returns><see langword="true"/> if the bot can use the emoji, <see langword="false"/> otherwise.</returns>
    public bool CanUseEmoji(DiscordGuild server, DiscordChannel channel, DiscordEmoji emoji)
    {
        if (emoji.Id is 0)
            return true;
        else if (!server.CurrentMember.PermissionsIn(channel).HasFlag(Permissions.UseExternalEmojis))
            return false;

        var servers = _shardedClient.ShardClients.Values
            .SelectMany(x => x.Guilds.Values);

        foreach (var guild in servers)
        {
            var canUse = guild.Emojis.Values
                .Where(x => (x.IsAvailable && x.Roles.Count is 0) || x.Roles.ContainsOne(guild.CurrentMember.Roles.Select(x => x.Id)))
                .Contains(emoji);

            if (canUse)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Creates a GET request to the specified URL and returns the result as a <see langword="string"/>.
    /// </summary>
    /// <param name="url">The URL to make the GET request.</param>
    /// <param name="cToken">The cancellation token.</param>
    /// <returns>A <see langword="string"/> of the requested URL, <see langword="null"/> if the request fails.</returns>
    public async Task<string?> GetOnlineStringAsync(string url, CancellationToken cToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient();

        try { return await httpClient.GetStringAsync(url, cToken); }
        catch { return default; }
    }

    /// <summary>
    /// Creates a GET request to the specified URL and returns the result as a <see cref="Stream"/>.
    /// </summary>
    /// <param name="url">The URL to make the GET request.</param>
    /// <param name="cToken">The cancellation token.</param>
    /// <returns>A <see cref="Stream"/> of the requested URL, <see langword="null"/> if the request fails.</returns>
    public async Task<Stream?> GetOnlineStreamAsync(string url, CancellationToken cToken = default)
    {
        var httpClient = _httpClientFactory.CreateClient();

        // Stream needs to be seekable.
        // HttpClient.GetStreamAsync() returns a non-seekable stream.
        try { return await (await httpClient.GetAsync(url, cToken)).Content.ReadAsStreamAsync(cToken); }
        catch { return default; }
    }

    /// <summary>
    /// Adds an emoji to the context guild.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="emoji">The emoji to be added.</param>
    /// <param name="name">The name of the emoji.</param>
    /// <remarks>If an emoji with the same <paramref name="name"/> is found, it will be replaced with the new emoji.</remarks>
    /// <returns><see langword="true"/> if the emoji got added, <see langword="false"/> otherwise.</returns>
    public async Task<bool> AddGuildEmojiAsync(CommandContext context, DiscordEmoji emoji, string? name = default)
    {
        name = name?.Trim(':') ?? emoji.Name.SanitizeEmojiName();
        var imageStream = await GetOnlineStreamAsync(emoji.Url);

        return imageStream is not null && await AddEmojiAsync(context, imageStream, name);
    }

    /// <summary>
    /// Adds an emoji to the context guild.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="url">Direct link to the image to be added as an emoji.</param>
    /// <param name="name">The name of the emoji. Defaults to the file name in the URL.</param>
    /// <remarks>If an emoji with the same <paramref name="name"/> is found, it will be replaced with the new emoji.</remarks>
    /// <returns><see langword="true"/> if the emoji got added, <see langword="false"/> otherwise.</returns>
    public async Task<bool> AddGuildEmojiAsync(CommandContext context, Uri url, string? name = default)
    {
        // Check if file extension is supported
        if (!url.AbsoluteUri.Contains('.') || !url.AbsoluteUri[(url.AbsoluteUri.LastIndexOf('.') + 1)..].StartsWith(AkkoStatics.SupportedEmojiFormats, StringComparison.InvariantCultureIgnoreCase))
            return false;

        var urlLeftPart = url.AbsoluteUri.RemoveExtension();
        var imageStream = await GetOnlineStreamAsync(url.AbsoluteUri);
        name = name?.Trim(':') ?? urlLeftPart[urlLeftPart.LastIndexOf('/')..].SanitizeEmojiName();

        return imageStream is not null && await AddEmojiAsync(context, imageStream, name);
    }

    /// <summary>
    /// Adds multiple emojis to the context guild.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="attachments">The attachments in a <see cref="DiscordMessage"/>.</param>
    /// <param name="additionalDelay">Time, in seconds, to add to the delay between processing each attachment.</param>
    /// <returns><see langword="true"/> if at least one emoji got added, <see langword="false"/> otherwise.</returns>
    public async Task<bool> AddGuildEmojisAsync(CommandContext context, IEnumerable<DiscordAttachment> attachments, double additionalDelay = 0.0)
    {
        var success = false;
        await context.TriggerTypingAsync();

        foreach (var attachment in attachments)
        {
            // Check if file extension is supported
            if (!attachment.FileName.Contains('.') || !attachment.FileName[(attachment.FileName.LastIndexOf('.') + 1)..].StartsWith(AkkoStatics.SupportedEmojiFormats, StringComparison.InvariantCultureIgnoreCase))
                continue;

            var name = attachment.FileName.RemoveExtension();
            var imageStream = await GetOnlineStreamAsync(attachment.Url);
            var added = imageStream is not null && await AddEmojiAsync(context, imageStream, name);

            success = success || added;

            if (added)
                await Task.Delay(AkkoStatics.SafetyDelay.Add(TimeSpan.FromSeconds(additionalDelay)));
        }

        return success;
    }

    /// <summary>
    /// Gets an embed with information about the specified guild.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="server">The Discord guild to get information from</param>
    /// <returns>An embed with information about the <paramref name="server"/>.</returns>
    public SerializableDiscordEmbed GetServerInfo(CommandContext context, DiscordGuild server)
    {
        var embed = new SerializableDiscordEmbed()
            .WithTitle(server.Name)
            .WithThumbnail(server.IconUrl)
            .AddField("id", server.Id.ToString(), true)
            .AddField("owner", server.Owner.GetFullname(), true)
            .AddField("members", server.MemberCount.ToString(), true)
            .AddField(
                context.FormatLocalized("{0} ({1})", "channels", server.Channels.Count),
                context.FormatLocalized(
                    "{0}: {1}\n" +
                    "{2}: {3}\n" +
                    "{4}: {5}",
                    "category", server.Channels.Values.Count(x => x.IsCategory),
                    "text", server.Channels.Values.Count(x => x.Type is not ChannelType.Voice and not ChannelType.Category),
                    "voice", server.Channels.Values.Count(x => x.Type is ChannelType.Voice)
                ),
                true
            )
            .AddField(
                "info",
                context.FormatLocalized(
                    "{0}: {1}\n" +
                    "{2}: {3}\n",
                    "verification_level", server.VerificationLevel.ToString().ToLowerInvariant(),
                    "created_on", server.CreationTimestamp.ToDiscordTimestamp(TimestampFormat.ShortDate)
                ),
                true
            )
            .AddField("roles", server.Roles.Count.ToString(), true)
            .AddField("Shard", $"{(server.Id >> 22) % (ulong)context.Client.ShardCount}/{context.Client.ShardCount}");  // Shards is not localized - this is intentional

        var modroles = server.Roles.Values
            .Where(x => x.Permissions.HasOneFlag(Permissions.Administrator | Permissions.KickMembers | Permissions.BanMembers))
            .Select(x => x.Name)
            .ToArray();

        if (modroles.Length is not 0)
            embed.AddField(context.FormatLocalized("{0} ({1})", "modroles", modroles.Length), string.Join(", ", modroles));

        if (!string.IsNullOrWhiteSpace(server.Description))
            embed.WithDescription(server.Description);

        if (server.Features.Count is not 0)
            embed.AddField("features", string.Join(", ", server.Features));

        if (!string.IsNullOrWhiteSpace(server.BannerUrl))
            embed.WithImageUrl(server.BannerUrl);

        if (!string.IsNullOrWhiteSpace(server.VanityUrlCode))
            embed.WithFooter(context.FormatLocalized("{0}: {1}", "vanity_url", server.VanityUrlCode));

        return embed;
    }

    /// <summary>
    /// Gets basic information about the specified Discord channel.
    /// </summary>
    /// <param name="embed">Embed to add the information to.</param>
    /// <param name="channel">Channel to get the information from.</param>
    /// <returns>An embed with information about the Discord <paramref name="channel"/>.</returns>
    public SerializableDiscordEmbed GetChannelInfo(SerializableDiscordEmbed embed, DiscordChannel channel)
    {
        embed.WithTitle(channel.Name)
            .AddField("type", channel.Type.ToString().ToLowerInvariant(), true)
            .AddField("position", channel.Position.ToString(), true);

        switch (channel.Type)
        {
            case ChannelType.Voice:
                embed.AddField("category", channel.Parent?.Name ?? "-", true)
                    .AddField("bitrate", $"{channel.Bitrate / 1000} kbps", true)
                    .AddField("user_limit", channel.UserLimit.ToString() ?? "-", true)
                    .AddField("connected_users", channel.Users.Count.ToString(), true);
                break;

            case ChannelType.Category:
                embed.AddField("contains", string.Join(", ", channel.Children.Select(x => x.Name)), false);
                break;

            default:
                embed.WithDescription(channel.Topic)
                    .AddField("category", channel.Parent?.Name ?? "-", true)
                    .AddField("nsfw", (channel.IsNSFW) ? AkkoStatics.SuccessEmoji.Name : AkkoStatics.FailureEmoji.Name, true)
                    .AddField("slowmode", channel.PerUserRateLimit?.ToString() ?? "-", true)
                    .AddField("visible_to", channel.Users.Count.ToString(), true);
                break;
        }

        return embed;
    }

    /// <summary>
    /// Adds an emoji to the context guild.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="imageStream">The image stream to be added as an emoji.</param>
    /// <param name="name">The name of the emoji.</param>
    /// <returns><see langword="true"/> if the emoji got added, <see langword="false"/> otherwise.</returns>
    private async Task<bool> AddEmojiAsync(CommandContext context, Stream imageStream, string name)
    {
        // Emojis must have less than 256Kb
        var isStreamValid = imageStream is not null && imageStream.Length < 256000;

        if (isStreamValid)
        {
            try { await context.Guild.CreateEmojiAsync(name.SanitizeEmojiName(), imageStream); }
            catch { isStreamValid = false; }
        }

        if (imageStream is not null)
            await imageStream.DisposeAsync();

        return isStreamValid;
    }
}