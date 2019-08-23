﻿// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// The push state event.
    /// </summary>
    [DataContract]
    internal sealed class PushStateEvent : Event
    {
        /// <summary>
        /// Type of the state to transition to.
        /// </summary>
        public Type State;

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStateEvent"/> class.
        /// </summary>
        /// <param name="s">Type of the state.</param>
        public PushStateEvent(Type s)
            : base()
        {
            this.State = s;
        }
    }
}
