using System;

namespace AkkoCore.Services.Database.Enums;

/// <summary>
/// Defines the behavior of different features in a Discord guild.
/// </summary>
[Flags]
public enum GuildConfigBehavior
{
    /// <summary>
    /// No extra behavior.
    /// </summary>
    None = 0,

    /// <summary>
    /// Determines whether embeds should be used in responses or not.
    /// </summary>
    UseEmbed = 1 << 0,

    /// <summary>
    /// Determines whether role mentions should be sanitized by hierarchy (flag is present)
    /// or by EveryoneServer permission (flag is absent).
    /// </summary>
    PermissiveRoleMention = 1 << 1,

    /// <summary>
    /// Determines whether command messages should be automatically deleted.
    /// </summary>
    DeleteCmdOnMessage = 1 << 2,

    /// <summary>
    /// Determines whether all global tags should be ignored in this guild.
    /// </summary>
    IgnoreGlobalTags = 1 << 3
}