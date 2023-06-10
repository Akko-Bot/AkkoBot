using AkkoCog.AntiPhishing.AntiPhishing.Abstractions;
using AkkoCog.AntiPhishing.AntiPhishing.Models;
using AkkoCog.AntiPhishing.AntiPhishing.Services;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Services.Database.Enums;
using ConcurrentCollections;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AkkoCog.AntiPhishing.AntiPhishing.Handlers;

/// <summary>
/// Deletes scam links using the anti-fish.harmony.rocks API.
/// </summary>
[CommandService<IAntiPhishingHandler>(ServiceLifetime.Singleton)]
internal sealed class AntiPhishingHandler : IAntiPhishingHandler
{
    private const string _apiUrl = "https://anti-fish.harmony.rocks?url=";
    private const string _shamefulNick = "I'm a dirty scammer❗";

    private readonly EventId _eventLog = new(99, nameof(AntiPhishingHandler));
    private readonly ConcurrentHashSet<string> _positiveUrls = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AntiPhishingService _service;
    private readonly RoleService _roleService;
    private readonly UserPunishmentService _punishService;

    public AntiPhishingHandler(IHttpClientFactory httpClientFactory, AntiPhishingService service, RoleService roleService, UserPunishmentService punishService)
    {
        _httpClientFactory = httpClientFactory;
        _service = service;
        _roleService = roleService;
        _punishService = punishService;
    }

    public Task FilterPhishingMessagesAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || eventArgs.Author is not DiscordMember member || string.IsNullOrWhiteSpace(eventArgs.Message.Content)
            || !_service.TryGetAntiPhishingConfig(eventArgs.Guild.Id, out var config) || !IsValidContext(config, member, eventArgs.Channel)
            || !eventArgs.Guild.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.ManageMessages))
            return Task.CompletedTask;

        _ = CheckAndPunishAsync(client, eventArgs.Message.Content, member, eventArgs.Channel, eventArgs.Guild, x => eventArgs.Message.DeleteAsync());

        return Task.CompletedTask;
    }

    public Task FilterPhishingNicknamesAsync(DiscordClient client, GuildMemberUpdateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !_service.TryGetAntiPhishingConfig(eventArgs.Guild.Id, out var config) || !IsValidContext(config, eventArgs.Member))
            return Task.CompletedTask;

        _ = CheckAndPunishAsync(client, eventArgs.NicknameAfter ?? eventArgs.Member.DisplayName, eventArgs.Member, default, eventArgs.Guild, x =>
        {
            return (_roleService.CheckHierarchy(eventArgs.Guild.CurrentMember, eventArgs.Member) && eventArgs.Guild.CurrentMember.Permissions.HasFlag(Permissions.ManageNicknames))
                ? eventArgs.Member.ModifyAsync(x => x.Nickname = (string.IsNullOrWhiteSpace(eventArgs.NicknameAfter)) ? _shamefulNick : string.Empty)
                : Task.CompletedTask;
        });

        return Task.CompletedTask;
    }

    public Task FilterPhishingUserJoinAsync(DiscordClient client, GuildMemberAddEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !_service.TryGetAntiPhishingConfig(eventArgs.Guild.Id, out var config) || !IsValidContext(config, eventArgs.Member))
            return Task.CompletedTask;

        _ = CheckAndPunishAsync(client, eventArgs.Member.Username, eventArgs.Member, default, eventArgs.Guild, x =>
        {
            if (!_roleService.CheckHierarchy(eventArgs.Guild.CurrentMember, eventArgs.Member))
                return Task.CompletedTask;

            _service.TryGetAntiPhishingConfig(eventArgs.Guild.Id, out var config);
            var fakeContext = GetFakeContext(client.GetCommandsNext(), eventArgs.Guild.CurrentMember, eventArgs.Guild.GetDefaultChannel());

            return config?.PunishmentType switch
            {
                null when eventArgs.Guild.CurrentMember.Permissions.HasFlag(Permissions.KickMembers)
                    => _punishService.KickUserAsync(fakeContext, eventArgs.Member, "antiphishing_punish_reason"),

                PunishmentType.Mute when eventArgs.Guild.CurrentMember.Permissions.HasFlag(Permissions.ManageNicknames)
                    => eventArgs.Member.ModifyAsync(x => x.Nickname = _shamefulNick),

                _ => Task.CompletedTask
            };
        });

        // If Discord ever exposes the "About Me" section of a user, add it here.

        var customStatus = eventArgs.Member.Presence.Activities.FirstOrDefault(x => x.ActivityType is ActivityType.Custom)?.CustomStatus?.Name ?? string.Empty;

        _ = CheckAndPunishAsync(client, customStatus, eventArgs.Member, default, eventArgs.Guild, x =>
        {
            var fakeContext = GetFakeContext(client.GetCommandsNext(), eventArgs.Guild.CurrentMember, eventArgs.Guild.GetDefaultChannel());

            return (_roleService.CheckHierarchy(eventArgs.Guild.CurrentMember, eventArgs.Member) && eventArgs.Guild.CurrentMember.Permissions.HasFlag(Permissions.KickMembers))
                ? _punishService.KickUserAsync(fakeContext, eventArgs.Member, "antiphishing_punish_reason")
                : Task.CompletedTask;
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a fake command context with an empty prefix and content and no command.
    /// </summary>
    /// <param name="cmdHandler">The command handler.</param>
    /// <param name="user">The user to assign the context to.</param>
    /// <param name="channel">The text channel to assign the context to.</param>
    /// <returns>A fake command context.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private CommandContext GetFakeContext(CommandsNextExtension cmdHandler, DiscordUser user, DiscordChannel channel)
        => cmdHandler.CreateFakeContext(user, channel, string.Empty, string.Empty, null!);

    /// <summary>
    /// Checks if the current context is valid.
    /// </summary>
    /// <param name="config">The anti-phishing config.</param>
    /// <param name="member">The user that triggered the event.</param>
    /// <param name="channel">The channel where the event occurred, if any.</param>
    /// <returns><see langword="true"/> if the context is valid, <see langword="false"/> otherwise.</returns>
    private bool IsValidContext(AntiPhishingGuildConfig config, DiscordMember member, DiscordChannel? channel = default)
    {
        if (!config.IsActive || member.IsBot)
            return false;

        foreach (var id in config.IgnoredIds)
        {
            if (member.Id == id || member.Roles.Select(x => x.Id).Contains(id) || channel?.Id == id)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a phishing link was sent, executes an <paramref name="action"/> and applies the appropriate punishment.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="text">The text to be checked for phishing links.</param>
    /// <param name="user">The user to be checked.</param>
    /// <param name="channel">The text channel where the message was sent, <see langword="null"/> if no channel is involved.</param>
    /// <param name="server">The Discord guild the event originated from.</param>
    /// <param name="action">An action to be performed before the punishment is applied.</param>
    /// <returns><see langword="true"/> if a punishment was applied, false otherwise.</returns>
    private async Task<bool> CheckAndPunishAsync(DiscordClient client, string text, DiscordMember user, DiscordChannel? channel, DiscordGuild? server, Func<Match, Task> action)
    {
        async Task ExecuteAsync(Match match, Func<Match, Task> action)
        {
            _positiveUrls.Add(match.Value);
            await action(match);
            await ApplyPunishmentAsync(client, user, channel, server);
        }

        var matches = AkkoRegexes.Url.Matches(text);

        if (matches.Count is 0)
            return false;

        var httpClient = _httpClientFactory.CreateClient();

        foreach (Match match in matches)
        {
            if (_positiveUrls.Contains(match.Value))
            {
                await ExecuteAsync(match, action);
                return true;
            }

            var result = await httpClient.GetStringAsync(_apiUrl + match.Value);

            if (!result.Equals(@"{""match"":false}", StringComparison.Ordinal))
            {
                await ExecuteAsync(match, action);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Applies the punishment to the user who shared a phishing link.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="user">The user to be checked.</param>
    /// <param name="channel">The channel where the message was sent, <see langword="null"/> if no channel is involved.</param>
    /// <param name="server">The Discord guild the event originated from.</param>
    /// <returns>The type of punishment that was applied or <see langword="null"/> if no punishment was applied.</returns>
    private async Task<PunishmentType?> ApplyPunishmentAsync(DiscordClient client, DiscordMember user, DiscordChannel? channel, DiscordGuild? server)
    {
        _service.TryGetAntiPhishingConfig(server?.Id ?? default, out var config);

        if (config?.PunishmentType is null || server is null)
            return default;

        var fakeContext = GetFakeContext(client.GetCommandsNext(), server.CurrentMember, channel ?? server.GetDefaultChannel());

        if (_roleService.CheckHierarchy(server.CurrentMember, user))
        {
            client.Logger.LogWarning(_eventLog, "Failed to apply an anti-phishing punishment because the bot doesn't have enough permission to act on user {User}.", user.Username);
            return default;
        }

        var reason = fakeContext.FormatLocalized("antiphishing_punish_reason");

        switch (config.PunishmentType)
        {
            case PunishmentType.Mute when server.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.ManageRoles)):
                var muteRole = await _roleService.FetchMuteRoleAsync(server);
                await _roleService.MuteUserAsync(fakeContext, muteRole, user, TimeSpan.Zero, reason);
                break;

            case PunishmentType.Kick when server.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.KickMembers)):
                await _punishService.KickUserAsync(fakeContext, user, reason);
                break;

            case PunishmentType.Softban when server.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.BanMembers)):
                await _punishService.SoftbanUserAsync(fakeContext, user, 7, reason);
                break;

            case PunishmentType.Ban when server.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.BanMembers)):
                await _punishService.BanUserAsync(fakeContext, user, 7, reason);
                break;

            default:
                client.Logger.LogWarning(_eventLog, "Failed applying punishment of type \"{PunishmentType}\". It's either unsupported or the bot has no permission to apply it.", config.PunishmentType.ToString());
                break;
        }

        return config.PunishmentType;
    }
}