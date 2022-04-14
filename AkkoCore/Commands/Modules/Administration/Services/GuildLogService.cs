using AkkoCore.Commands.Attributes;
using AkkoCore.Config.Models;
using AkkoCore.Extensions;
using AkkoCore.Services.Caching.Abstractions;
using AkkoCore.Services.Database;
using AkkoCore.Services.Database.Entities;
using AkkoCore.Services.Database.Enums;
using AkkoCore.Services.Database.Queries;
using AkkoCore.Services.Localization.Abstractions;
using ConcurrentCollections;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using LinqToDB;
using Kotz.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AkkoCore.Commands.Modules.Administration.Services;

/// <summary>
/// Groups utility methods for manipulating <see cref="GuildLogEntity"/> objects.
/// </summary>
[CommandService(ServiceLifetime.Singleton)]
public sealed class GuildLogService
{
    private readonly string _headerSeparator = new('=', 30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDbCache _dbCache;
    private readonly ILocalizer _localizer;
    private readonly BotConfig _botConfig;
    private readonly DiscordWebhookClient _webhookClient;

    /// <summary>
    /// Contains the flags of the guild log types that are not supported for guild logging.
    /// </summary>
    public const GuildLogType ForbiddenTypes = GuildLogType.Unknown | GuildLogType.MessageCreated | GuildLogType.PresenceEvents;

    /// <summary>
    /// Contains the flags of all groups of guild logs.
    /// </summary>
    public static GuildLogType[] GuildLogTypeGroups { get; } = new[]
    {
        GuildLogType.None,
        GuildLogType.All,
        GuildLogType.ChannelEvents,
        GuildLogType.PunishmentEvents,
        GuildLogType.MemberEvents,
        GuildLogType.MessageEvents,
        GuildLogType.VoiceEvents,
        GuildLogType.RoleEvents,
        GuildLogType.InviteEvents,
        GuildLogType.EmojiEvents,
        GuildLogType.AltEvents,
        GuildLogType.PresenceEvents
    };

    public GuildLogService(IServiceScopeFactory scopeFactory, IDbCache dbCache, ILocalizer localizer, BotConfig botConfig, DiscordWebhookClient webhookClient)
    {
        _scopeFactory = scopeFactory;
        _dbCache = dbCache;
        _localizer = localizer;
        _botConfig = botConfig;
        _webhookClient = webhookClient;
    }

    /// <summary>
    /// Starts logging of a guild event.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="channel">The channel where logs are being output.</param>
    /// <param name="logType">The type of guild event to generate logs for.</param>
    /// <param name="name">The name of the webhook.</param>
    /// <param name="avatar">The image stream of the webhook's avatar.</param>
    /// <remarks>The method does not accept the types present in <see cref="ForbiddenTypes"/> and <see cref="GuildLogTypeGroups"/></remarks>
    /// <returns><see langword="true"/> if the guild log was created or updated, <see langword="false"/> otherwise.</returns>
    /// <exception cref="ArgumentException">Occurs when the channel type is invalid.</exception>
    public async Task<bool> StartLogAsync(CommandContext context, DiscordChannel channel, GuildLogType logType, string? name = default, Stream? avatar = default)
    {
        if (channel.Type is not ChannelType.Text and not ChannelType.News and not ChannelType.Store)
            throw new ArgumentException("Logs can only be output to text channels.", nameof(channel));
        else if (logType.HasOneFlag(ForbiddenTypes) || GuildLogTypeGroups.Contains(logType))
            return false;

        using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

        if (!_dbCache.GuildLogs.TryGetValue(context.Guild.Id, out var guildLogs))
            guildLogs ??= new();

        var anyChannelLog = guildLogs.FirstOrDefault(x => x.ChannelId == channel.Id);

        // These methods dispose the avatar stream because whoever wrote them is a 🐒
        var webhook = (anyChannelLog is null)
            ? await channel.CreateWebhookAsync(name ?? _botConfig.WebhookLogName, avatar?.GetCopy())
            : _webhookClient.GetRegisteredWebhook(anyChannelLog.WebhookId)
                ?? await context.Client.GetWebhookSafelyAsync(anyChannelLog.WebhookId)
                ?? await channel.CreateWebhookAsync(name ?? _botConfig.WebhookLogName, avatar?.GetCopy());

        var guildLog = guildLogs.FirstOrDefault(x => x.Type == logType)
            ?? new()
            {
                GuildIdFK = context.Guild.Id,
                ChannelId = channel.Id,
                WebhookId = webhook.Id,
                IsActive = true,
                Type = logType
            };

        // If webhook is not being used by any other log, modify it
        if (guildLog.ChannelId != channel.Id && guildLogs.Count(x => x.ChannelId == channel.Id) <= 1)
        {
            _webhookClient.TryRemove(guildLog.WebhookId);
            webhook = await webhook.ModifyAsync(name ?? webhook.Name, avatar?.GetCopy(), channel.Id);
        }

        // Update entry
        db.GuildLogs.Upsert(guildLog);
        guildLog.ChannelId = channel.Id;
        guildLog.WebhookId = webhook.Id;
        guildLog.IsActive = true;
        guildLog.Type = logType;

        await db.SaveChangesAsync();

        // Update cache
        guildLogs.Add(guildLog);
        _dbCache.GuildLogs.AddOrUpdate(context.Guild.Id, guildLogs, (_, _) => guildLogs);

        // Update webhook cache
        _webhookClient.TryAdd(webhook);

        return true;
    }

    /// <summary>
    /// Stops logging of a guild event.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="logType">The type of guild event to generate logs for.</param>
    /// <param name="deleteEntry"><see langword="true"/> to remove the entry from the database, <see langword="false"/> to update it.</param>
    /// <remarks>The method does not accept the types present in <see cref="ForbiddenTypes"/> and <see cref="GuildLogTypeGroups"/></remarks>
    /// <returns><see langword="true"/> if the guild log was successfully disabled, <see langword="false"/> otherwise or if it is already disabled.</returns>
    public async Task<bool> StopLogAsync(CommandContext context, GuildLogType logType, bool deleteEntry = false)
    {
        if (logType.HasOneFlag(ForbiddenTypes) || GuildLogTypeGroups.Contains(logType)
            || !_dbCache.GuildLogs.TryGetValue(context.Guild.Id, out var guildLogs))
            return false;

        var guildLog = guildLogs.FirstOrDefault(x => x.Type == logType);

        if (guildLog is null || (!guildLog.IsActive && !deleteEntry))
            return false;

        // Update the cache
        if (deleteEntry)
            guildLogs.TryRemove(guildLog);
        else
            guildLog.IsActive = false;

        // Update the database entry
        using var scope = _scopeFactory.GetRequiredScopedService<AkkoDbContext>(out var db);

        return (deleteEntry)
            ? await db.GuildLogs.DeleteAsync(x => x.GuildIdFK == context.Guild.Id && x.ChannelId == guildLog.ChannelId) is not 0
            : await db.GuildLogs.UpdateAsync(
                x => x.GuildIdFK == context.Guild.Id && x.ChannelId == guildLog.ChannelId,
                _ => new() { IsActive = false }
            ) is not 0;
    }

    /// <summary>
    /// Gets the cached guild logs of the specified Discord guild.
    /// </summary>
    /// <param name="server">The Discord guild.</param>
    /// <returns>The guild logs.</returns>
    public IReadOnlyCollection<GuildLogEntity> GetGuildLogs(DiscordGuild server)
    {
        _dbCache.GuildLogs.TryGetValue(server.Id, out var guildLogs);
        return guildLogs ?? new ConcurrentHashSet<GuildLogEntity>(1, 0);
    }

    /// <summary>
    /// Generates a message log for the specified Discord messages.
    /// </summary>
    /// <param name="messages">The messages to be logged.</param>
    /// <param name="channel">The Discord channel the messages are from.</param>
    /// <param name="locale">The locale to be used for the header.</param>
    /// <param name="extraInfo">Extra info to be appended to the header.</param>
    /// <returns>The message log, <see langword="null"/> if the message collection is empty.</returns>
    public string? GenerateMessageLog(IEnumerable<DiscordMessage> messages, DiscordChannel channel, string locale, string? extraInfo = default)
    {
        var amount = messages?.Count();

        if (amount is 0 or null || channel is null)
            return null;

        var msgLog = new StringBuilder(GenerateLogHeader(channel, amount.Value, locale, extraInfo));

        foreach (var message in messages!)
        {
            msgLog.AppendLine(
                $"{message.Author.GetFullname()} ({message.Author.Id}) [{message.CreationTimestamp.LocalDateTime}]" + Environment.NewLine +
                message.Content + ((string.IsNullOrWhiteSpace(message.Content)) ? string.Empty : Environment.NewLine) +
                ((message.Attachments.Count is 0) ? string.Empty : string.Join(Environment.NewLine, message.Attachments.Select(x => x.Url)) + Environment.NewLine)
            );
        }

        return msgLog.ToString();
    }

    /// <summary>
    /// Generates the header of a message log.
    /// </summary>
    /// <param name="channel">The Discord channel the messages are from.</param>
    /// <param name="messageAmount">The amount of messages being logged.</param>
    /// <param name="locale">The locale to be used for the header.</param>
    /// <param name="extraInfo">Extra info to be appended to the header.</param>
    /// <returns>The log header.</returns>
    private string GenerateLogHeader(DiscordChannel channel, int messageAmount, string locale, string? extraInfo = default)
    {
        return
            $"==> {_localizer.GetResponseString(locale, "log_channel_name")}: {channel.Name} | {_localizer.GetResponseString(locale, "id")}: {channel.Id}" + Environment.NewLine +
            $"==> {_localizer.GetResponseString(locale, "log_channel_topic")}: {channel.Topic}" + Environment.NewLine +
            $"==> {_localizer.GetResponseString(locale, "category")}: {channel.Parent.Name}" + Environment.NewLine +
            $"==> {_localizer.GetResponseString(locale, "log_messages_logged")}: {messageAmount}" + Environment.NewLine +
            extraInfo +
            $"{_headerSeparator}/{_localizer.GetResponseString(locale, "log_start")}/{_headerSeparator}" +
            Environment.NewLine + Environment.NewLine;
    }
}