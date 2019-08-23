﻿// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Attribute for declaring what events should be deferred in
    /// a machine state.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DeferEventsAttribute : Attribute
    {
        /// <summary>
        /// Event types.
        /// </summary>
        internal Type[] Events;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeferEventsAttribute"/> class.
        /// </summary>
        /// <param name="eventTypes">Event types</param>
        public DeferEventsAttribute(params Type[] eventTypes)
        {
            this.Events = eventTypes;
        }
    }
}
