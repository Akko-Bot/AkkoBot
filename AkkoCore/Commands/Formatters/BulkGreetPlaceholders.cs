using AkkoCore.Commands.Attributes;
using AkkoCore.Extensions;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace AkkoCore.Commands.Formatters;

/// <summary>
/// Defines the formatter for command placeholders in bulk greetings.
/// </summary>
[CommandService(ServiceLifetime.Singleton)]
public sealed class BulkGreetPlaceholders : CommandPlaceholders
{
    private IEnumerable<DiscordMember> _users = Enumerable.Empty<DiscordMember>();

    /// <summary>
    /// Stores the users that should be greeted in bulk.
    /// </summary>
    public IEnumerable<DiscordMember> Users
    {
        get => _users;
        set => _users = value ?? Enumerable.Empty<DiscordMember>();
    }

    public BulkGreetPlaceholders()
    {
        base.placeholderActions["user.id"] = _ => string.Join(", ", Users.Select(x => x.Id));
        base.placeholderActions["user.name"] = _ => string.Join(", ", Users.Select(x => x.Username));
        base.placeholderActions["user.discrim"] = _ => null;
        base.placeholderActions["user.fullname"] = _ => string.Join(", ", Users.Select(x => x.Username));
        base.placeholderActions["user.nickname"] = _ => string.Join(", ", Users.Select(x => x.DisplayName));
        base.placeholderActions["user.mention"] = _ => string.Join(", ", Users.Select(x => x.Mention));
        //base.placeholderActions["user.avatar"] = _ => null; // Let it fetch the image of the context user, so deserialization doesn't fail
        base.placeholderActions["user.creationdate"] = _ => null;
        base.placeholderActions["user.joindate"] = _ => Users.Min(x => x.JoinedAt);
        base.placeholderActions["user.joindifference"] = _ => null;
        base.placeholderActions["user.flags"] = _ => null;
        base.placeholderActions["user.locale"] = _ => null;
        base.placeholderActions["user.2fa"] = _ => null;
        base.placeholderActions["user.hierarchy"] = _ => Users.Min(x => x.Hierarchy);
        base.placeholderActions["user.color"] = _ => Users.Min(x => x.Color);
        base.placeholderActions["user.nitrodate"] = _ => null;
        base.placeholderActions["user.nitrotype"] = _ => null;
        base.placeholderActions["user.roles"] = _ => string.Join(", ", Users.SelectMany(x => x.Roles).Select(x => x.Name));
        base.placeholderActions["user.voicechat"] = _ => null;
    }
}