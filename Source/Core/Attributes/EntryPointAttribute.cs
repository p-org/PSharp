// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Attribute for declaring the entry point to a P# program.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class EntryPointAttribute : Attribute
    {
    }
}
