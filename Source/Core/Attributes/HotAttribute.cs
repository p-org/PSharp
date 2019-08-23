using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Attribute for checking liveness properties in monitors.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class HotAttribute : Attribute
    {
    }
}
