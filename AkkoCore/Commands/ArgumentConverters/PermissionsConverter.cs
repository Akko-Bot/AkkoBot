using AkkoCore.Common;
using AkkoCore.Extensions;
using AkkoCore.Services.Localization.Abstractions;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AkkoCore.Commands.ArgumentConverters;

internal sealed class PermissionsConverter : IArgumentConverter<Permissions>
{
    /// <summary>
    /// Contains the permission response keys and the value they represent.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, Optional<Permissions>> _permissionsTable = new Dictionary<string, Optional<Permissions>>()
    {
        ["perm_access_channels"] = Optional.FromValue(Permissions.AccessChannels),
        ["perm_add_reactions"] = Optional.FromValue(Permissions.AddReactions),
        ["perm_administrator"] = Optional.FromValue(Permissions.Administrator),
        ["perm_all"] = Optional.FromValue(Permissions.All),
        ["perm_attach_files"] = Optional.FromValue(Permissions.AttachFiles),
        ["perm_ban_members"] = Optional.FromValue(Permissions.BanMembers),
        ["perm_change_nickname"] = Optional.FromValue(Permissions.ChangeNickname),
        ["perm_create_instant_invite"] = Optional.FromValue(Permissions.CreateInstantInvite),
        ["perm_deafen_members"] = Optional.FromValue(Permissions.DeafenMembers),
        ["perm_embed_links"] = Optional.FromValue(Permissions.EmbedLinks),
        ["perm_kick_members"] = Optional.FromValue(Permissions.KickMembers),
        ["perm_manage_channels"] = Optional.FromValue(Permissions.ManageChannels),
        ["perm_manage_emojis"] = Optional.FromValue(Permissions.ManageEmojis),
        ["perm_manage_guild"] = Optional.FromValue(Permissions.ManageGuild),
        ["perm_manage_messages"] = Optional.FromValue(Permissions.ManageMessages),
        ["perm_manage_nicknames"] = Optional.FromValue(Permissions.ManageNicknames),
        ["perm_manage_roles"] = Optional.FromValue(Permissions.ManageRoles),
        ["perm_manage_webhooks"] = Optional.FromValue(Permissions.ManageWebhooks),
        ["perm_mention_everyone"] = Optional.FromValue(Permissions.MentionEveryone),
        ["perm_move_members"] = Optional.FromValue(Permissions.MoveMembers),
        ["perm_mute_members"] = Optional.FromValue(Permissions.MuteMembers),
        ["perm_none"] = Optional.FromValue(Permissions.None),
        ["perm_priority_speaker"] = Optional.FromValue(Permissions.PrioritySpeaker),
        ["perm_read_message_history"] = Optional.FromValue(Permissions.ReadMessageHistory),
        ["perm_send_messages"] = Optional.FromValue(Permissions.SendMessages),
        ["perm_send_tts_messages"] = Optional.FromValue(Permissions.SendTtsMessages),
        ["perm_speak"] = Optional.FromValue(Permissions.Speak),
        ["perm_stream"] = Optional.FromValue(Permissions.Stream),
        ["perm_use_external_emojis"] = Optional.FromValue(Permissions.UseExternalEmojis),
        ["perm_use_voice"] = Optional.FromValue(Permissions.UseVoice),
        ["perm_use_voice_detection"] = Optional.FromValue(Permissions.UseVoiceDetection),
        ["perm_view_audit_log"] = Optional.FromValue(Permissions.ViewAuditLog),
        ["perm_manage_threads"] = Optional.FromValue(Permissions.ManageThreads),
        ["perm_use_external_stickers"] = Optional.FromValue(Permissions.UseExternalStickers),
        ["perm_start_embedded_activities"] = Optional.FromValue(Permissions.StartEmbeddedActivities),
        ["perm_send_messages_in_threads"] = Optional.FromValue(Permissions.SendMessagesInThreads),
        ["perm_create_private_threads"] = Optional.FromValue(Permissions.CreatePrivateThreads),
        ["perm_create_public_threads"] = Optional.FromValue(Permissions.CreatePublicThreads),
        ["perm_use_application_commands"] = Optional.FromValue(Permissions.UseApplicationCommands),
        ["perm_manage_events"] = Optional.FromValue(Permissions.ManageEvents),
        ["perm_moderate_members"] = Optional.FromValue(Permissions.ModerateMembers)
    };

    // Try fetching the value for DefaultLanguage first. If it fails, try the context language.
    public Task<Optional<Permissions>> ConvertAsync(string input, CommandContext ctx)
    {
        var localizer = ctx.Services.GetRequiredService<ILocalizer>();
        var settings = ctx.GetMessageSettings();

        var result = GetPermission(localizer, input, settings.Locale);

        return (result.HasValue)
            ? Task.FromResult(result)
            : Task.FromResult(GetPermission(localizer, input, AkkoConstants.DefaultLanguage));
    }

    /// <summary>
    /// Gets the permission from a localized input.
    /// </summary>
    /// <param name="localizer">The string localizer.</param>
    /// <param name="input">The user input.</param>
    /// <param name="locale">The locale of the input.</param>
    /// <returns>The permission enum value.</returns>
    private Optional<Permissions> GetPermission(ILocalizer localizer, string input, string locale)
    {
        var keyResponsePair = localizer.GetResponsePairsByPartialKey(locale, "perm_")
            .FirstOrDefault(x => x.Value.Equals(input, StringComparison.InvariantCultureIgnoreCase));

        return (_permissionsTable.TryGetValue(keyResponsePair.Key, out var result))
            ? result
            : Optional.FromNoValue<Permissions>();
    }
}