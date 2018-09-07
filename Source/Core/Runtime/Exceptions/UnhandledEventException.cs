// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Signals that a machine received an unhandled event.
    /// </summary>
    public sealed class UnhandledEventException : RuntimeException
    {
        /// <summary>
        /// Name of the current state of the machine.
        /// </summary>
        public string CurrentStateName;

        /// <summary>
        ///  The event.
        /// </summary>
        public Event UnhandledEvent;

        /// <summary>
        /// Initializes a new instance of the exception.
        /// </summary>
        /// <param name="CurrentStateName">Current state name.</param>
        /// <param name="UnhandledEvent">The event that was unhandled.</param>
        /// <param name="message">The exception message.</param>
        internal UnhandledEventException(string CurrentStateName, Event UnhandledEvent, string message)
            : base(message)
        {
            this.CurrentStateName = CurrentStateName;
            this.UnhandledEvent = UnhandledEvent;
        }
    }
}

