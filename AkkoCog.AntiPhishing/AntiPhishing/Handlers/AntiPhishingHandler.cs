using AkkoCog.AntiPhishing.AntiPhishing.Abstractions;
using AkkoCog.AntiPhishing.AntiPhishing.Services;
using AkkoCore.Commands.Attributes;
using AkkoCore.Commands.Modules.Administration.Services;
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

    private readonly EventId _eventLog = new(99, nameof(AntiPhishingHandler));

    private static readonly Regex _urlRegex = new(
        @"https?:\/\/\S{2,}",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

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

    public Task FilterPhishingLinksAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        if (eventArgs.Guild is null || !_service.IsAntiPhishingActive(eventArgs.Guild.Id)
            || !eventArgs.Guild.CurrentMember.PermissionsIn(eventArgs.Channel).HasPermission(Permissions.ManageMessages))
            return Task.CompletedTask;

        _ = DeleteIfMatchAsync(client, eventArgs);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes the Discord message if it contains a phishing link.
    /// </summary>
    /// <param name="client">The Discord client</param>
    /// <param name="eventArgs">The event arguments.</param>
    /// <returns><see langword="true"/> if the message was deleted, <see langword="false"/> otherwise.</returns>
    private async Task<bool> DeleteIfMatchAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        // Delete message, cache the scam URL, and stop handling this event
        Task DeleteMessageAsync(Match match, MessageCreateEventArgs eventArgs)
        {
            eventArgs.Handled = true;
            _positiveUrls.Add(match.Value);

            return eventArgs.Message.DeleteAsync();
        }

        var matches = _urlRegex.Matches(eventArgs.Message.Content);

        if (matches.Count is 0)
            return false;

        var httpClient = _httpClientFactory.CreateClient();

        foreach (Match match in matches)
        {
            if (_positiveUrls.Contains(match.Value))
            {
                _ = Task.WhenAll(DeleteMessageAsync(match, eventArgs), ApplyPunishmentAsync(client, eventArgs));
                return true;
            }

            var result = await httpClient.GetStringAsync(_apiUrl + match.Value);

            if (!result.Equals(@"{""match"":false}", StringComparison.Ordinal))
            {
                _ = Task.WhenAll(DeleteMessageAsync(match, eventArgs), ApplyPunishmentAsync(client, eventArgs));
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Applies the punishment to the user who shared a phishing link.
    /// </summary>
    /// <param name="client">The Discord client.</param>
    /// <param name="eventArgs">The event arguments.</param>
    private async Task ApplyPunishmentAsync(DiscordClient client, MessageCreateEventArgs eventArgs)
    {
        var punishmentType = _service.GetPunishment(eventArgs.Guild.Id);

        if (punishmentType is null)
            return;

        var user = (DiscordMember)eventArgs.Author;

        var fakeContext = client.GetCommandsNext().CreateFakeContext(
            eventArgs.Guild.CurrentMember,
            eventArgs.Channel,
            string.Empty,
            string.Empty,
            null
        );

        if (_roleService.CheckHierarchy(eventArgs.Guild.CurrentMember, user))
        {
            client.Logger.LogWarning(_eventLog, "Failed to apply an anti-phishing punishment because the bot doesn't have enough permission to act on user {User}.", user.GetFullname());
            return;
        }

        var reason = fakeContext.FormatLocalized("antiphishing_punish_reason");

        switch (punishmentType)
        {
            case PunishmentType.Mute when eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.ManageRoles)):
                var muteRole = await _roleService.FetchMuteRoleAsync(eventArgs.Guild);
                await _roleService.MuteUserAsync(fakeContext, muteRole, user, TimeSpan.Zero, reason);
                break;

            case PunishmentType.Kick when eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.KickMembers)):
                await _punishService.KickUserAsync(fakeContext, user, reason);
                break;

            case PunishmentType.Softban when eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.BanMembers)):
                await _punishService.SoftbanUserAsync(fakeContext, user, 7, reason);
                break;

            case PunishmentType.Ban when eventArgs.Guild.CurrentMember.Roles.Any(x => x.Permissions.HasPermission(Permissions.BanMembers)):
                await _punishService.BanUserAsync(fakeContext, user, 7, reason);
                break;

            default:
                client.Logger.LogWarning(_eventLog, "Failed applying punishment of type \"{PunishmentType}\". It's either unsupported or the bot has no permission to apply it.", punishmentType);
                break;
        }
    }
}