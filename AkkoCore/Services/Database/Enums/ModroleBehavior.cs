using System;

namespace AkkoCore.Services.Database.Enums;

/// <summary>
/// Defines the possible behaviors for a modrole.
/// </summary>
[Flags]
public enum ModroleBehavior
{
    /// <summary>
    /// No action.
    /// </summary>
    None = 0,

    /// <summary>
    /// Enforces role hierarchy, so users cannot assign or remove target roles
    /// that are higher in the role hierarchy than themselves.
    /// </summary>
    EnforceHierarchy = 1 << 0,

    /// <summary>
    /// Forces all target roles to be exclusive.
    /// </summary>
    Exclusive = 1 << 1,

    /// <summary>
    /// All behaviors.
    /// </summary>
    All = EnforceHierarchy | Exclusive
}