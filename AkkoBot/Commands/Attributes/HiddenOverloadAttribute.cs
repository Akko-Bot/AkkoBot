using System;

namespace AkkoBot.Commands.Attributes
{
    /// <summary>
    /// Hides this command overload from the help command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class HiddenOverloadAttribute : Attribute { }
}