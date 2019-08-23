﻿using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Attribute for declaring what action to perform
    /// when entering a machine state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class OnEntryAttribute : Attribute
    {
        /// <summary>
        /// Action name.
        /// </summary>
        internal readonly string Action;

        /// <summary>
        /// Initializes a new instance of the <see cref="OnEntryAttribute"/> class.
        /// </summary>
        /// <param name="actionName">Action name</param>
        public OnEntryAttribute(string actionName)
        {
            this.Action = actionName;
        }
    }
}
