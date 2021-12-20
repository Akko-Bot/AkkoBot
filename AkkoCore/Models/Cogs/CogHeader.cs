using AkkoCore.Config.Abstractions;

namespace AkkoCore.Models.Cogs;

/// <summary>
/// Contains metadata about a cog.
/// </summary>
public sealed record CogHeader
{
    /// <summary>
    /// The name of the cog.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The version of this cog.
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// The name of the author.
    /// </summary>
    public string Author { get; }

    /// <summary>
    /// Description of what the cog does.
    /// </summary>
    public string Description { get; }

    public CogHeader(ICogSetup cogSetup)
    {
        Name = cogSetup.Name;
        Version = cogSetup.Version;
        Author = cogSetup.Author;
        Description = cogSetup.Description;
    }
}