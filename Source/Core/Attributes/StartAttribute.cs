using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Attribute for declaring that a state of a machine
    /// is the start one.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class StartAttribute : Attribute
    {
    }
}
