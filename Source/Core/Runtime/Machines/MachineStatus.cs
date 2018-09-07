// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// The status returned by the machine as the result of a runtime operation,
    /// such as enqueuing an event.
    /// </summary>
    internal enum MachineStatus
    {
        /// <summary>
        /// The machine notifies the runtime that an
        /// event handler is already running.
        /// </summary>
        EventHandlerRunning = 0,
        /// <summary>
        /// The machine notifies the runtime that an
        /// event handler is not running.
        /// </summary>
        EventHandlerNotRunning,
        /// <summary>
        /// The machine notifies the runtime that there is no
        /// next event available to dequeue and handle.
        /// </summary>
        NextEventUnavailable,
        /// <summary>
        /// The machine notifies the runtime that the machine is halted.
        /// </summary>
        IsHalted
    }
}
