﻿using System;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Defines a push state transition.
    /// </summary>
    internal sealed class PushStateTransition
    {
        /// <summary>
        /// The target state.
        /// </summary>
        public Type TargetState;

        /// <summary>
        /// Initializes a new instance of the <see cref="PushStateTransition"/> class.
        /// </summary>
        /// <param name="targetState">The target state.</param>
        public PushStateTransition(Type targetState)
        {
            this.TargetState = targetState;
        }
    }
}
