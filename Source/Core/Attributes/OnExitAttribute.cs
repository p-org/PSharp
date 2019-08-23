using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Attribute for declaring what action to perform
    /// when exiting a machine state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OnExitAttribute : Attribute
    {
        /// <summary>
        /// Action name.
        /// </summary>
        internal string Action;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnExitAttribute"/> class.
        /// </summary>
        /// <param name="actionName">Action name</param>
        public OnExitAttribute(string actionName)
        {
            this.Action = actionName;
        }
    }
}
