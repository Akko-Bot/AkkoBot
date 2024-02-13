namespace AkkoBot.Common;

/// <summary>
/// Contains ANSI color codes.
/// </summary>
public static class AnsiColor
{
    /// <summary>
    /// Gray text.
    /// </summary>
    public const string Gray = "\x1b[90m";

    /// <summary>
    /// Green text.
    /// </summary>
    public const string Green = "\x1b[32m";

    /// <summary>
    /// Blue text.
    /// </summary>
    public const string Blue = "\x1b[34m";

    /// <summary>
    /// Purple text.
    /// </summary>
    public const string Purple = "\x1b[35m";

    /// <summary>
    /// Red text.
    /// </summary>
    public const string Red = "\x1b[31m";

    /// <summary>
    /// White text on a red background .
    /// </summary>
    public const string WhiteOnRed = "\x1b[1;37;41m";

    /// <summary>
    /// The default text color.
    /// </summary>
    public const string Default = "\x1b[0m";
}