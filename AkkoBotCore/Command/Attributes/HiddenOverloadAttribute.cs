using System;

namespace AkkoBot.Command.Attributes
{
    /// <summary>
    /// Marks this command overload as hidden.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public sealed class HiddenOverloadAttribute : Attribute { }
}