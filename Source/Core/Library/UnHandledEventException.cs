// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Signals that a machine received an unhandled event
    /// </summary>
    public sealed class UnhandledEventException : RuntimeException
    {
        /// <summary>
        /// The machine that threw the exception
        /// </summary>
        public MachineId mid;

        /// <summary>
        /// Name of the current state of the machine
        /// </summary>
        public string CurrentStateName;

        /// <summary>
        ///  The event
        /// </summary>
        public Event UnhandledEvent;

        /// <summary>
        /// Initializes a new instance of the exception.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="CurrentStateName">Current state name</param>
        /// <param name="UnhandledEvent">The event that was unhandled</param>
        /// <param name="message">Message</param>
        internal UnhandledEventException(MachineId mid, string CurrentStateName, Event UnhandledEvent, string message)
            : base(message)
        {
            this.mid = mid;
            this.CurrentStateName = CurrentStateName;
            this.UnhandledEvent = UnhandledEvent;
        }

    }
}

