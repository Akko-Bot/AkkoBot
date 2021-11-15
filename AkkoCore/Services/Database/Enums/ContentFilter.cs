using System;

namespace AkkoCore.Services.Database.Enums;

/// <summary>
/// Defines the type of content that should be preserved.
/// </summary>
[Flags]
public enum ContentFilter
{
    /// <summary>
    /// Nothing.
    /// </summary>
    None = 0,

    /// <summary>
    /// Message attachments of any sort.
    /// </summary>
    Attachment = 1,

    /// <summary>
    /// Image links or attachments.
    /// </summary>
    Image = 1 << 1,

    /// <summary>
    /// Links of any sort.
    /// </summary>
    Url = 1 << 2,

    /// <summary>
    /// Guild invites.
    /// </summary>
    Invite = 1 << 3,

    /// <summary>
    /// Message stickers.
    /// </summary>
    Sticker = 1 << 4,

    /// <summary>
    /// Bot commands.
    /// </summary>
    Command = 1 << 5
}