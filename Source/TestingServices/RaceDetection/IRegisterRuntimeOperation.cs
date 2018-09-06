// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices
{
    /// <summary>
    /// Interface to register interesting runtime operations.
    /// For race detection, the interesting operations are:
    /// 1. Reads and writes to the (shared) heap
    /// 2. Enqueues (posts) and dequeues (action begins)
    /// 3. Creation of a new machine
    /// In addition, this interface also allows clients to query
    /// the runtime for the currently running machine, and whether
    /// the runtime is in an action.
    /// </summary>
    public interface IRegisterRuntimeOperation
    {
        /// <summary>
        /// InAction[machineId.Value] = true iff the runtime executing an action
        /// in machine with Id machineId
        /// Reads and writes are instrumented only provided we're in an action.
        /// </summary>
        Dictionary<ulong, bool> InAction { get; set; }

        /// <summary>
        /// InMonitor = -1 iff the runtime is not inside a monitor
        /// and the monitor id otherwise
        /// </summary>
        long InMonitor { get; set; }

        /// <summary>
        /// Process a read to a heap location.
        /// <param name="source">The machine performing the read</param>
        /// <param name="sourceInformation"> Line number of this read</param>
        /// <param name="location">The base address for the heap location read</param>
        /// <param name="objHandle">The object handle</param>
        /// <param name="offset">The offset</param>
        /// <param name="isVolatile">Was the location declared volatile?</param>
        /// </summary>
        void RegisterRead(ulong source, string sourceInformation, UIntPtr location, UIntPtr objHandle, UIntPtr offset, bool isVolatile);

        /// <summary>
        /// Process a write to a heap location.
        /// <param name="source">The machine performing the write</param>
        /// <param name="sourceInformation"> Line number of this write</param>
        /// <param name="location">The base address for the heap location written</param>
        /// <param name="objHandle">The object handle</param>
        /// <param name="offset">The offset</param>
        /// <param name="isVolatile">Was the location declared volatile?</param>
        /// </summary>
        void RegisterWrite(ulong source, string sourceInformation, UIntPtr location, UIntPtr objHandle, UIntPtr offset, bool isVolatile);

        /// <summary>
        /// Process the enqueue of an event by a machine.
        /// <param name="source">The id of the machine that is the origin of the enqueue/post</param>
        /// <param name="target">The id of the machine receiving the event</param>
        /// <param name="e">The event sent</param>
        /// <param name="sequenceNumber">Is n if this is the n'th enqueue</param>
        /// </summary>
        void RegisterEnqueue(MachineId source, MachineId target, Event e, ulong sequenceNumber);

        /// <summary>
        /// Process the deq and begin of an action by a machine.
        /// <param name="source">The id of the machine that originally posted the event</param>
        /// <param name="target">The id of the machine processing the event</param>
        /// <param name="e">The event being processed</param>
        /// <param name="sequenceNumber">Is n if this is the n'th enqueue</param>
        /// </summary>
        void RegisterDequeue(MachineId source, MachineId target, Event e, ulong sequenceNumber);

        /// <summary>
        /// Update the internal data structures and vector clocks.
        /// when a machine creates another
        /// <param name="source">The id of the machine that is the creator</param>
        /// <param name="target">The id of the machine that is freshly created</param>
        /// </summary>
        void RegisterCreateMachine(MachineId source, MachineId target);

        /// <summary>
        /// Set the runtime an implementer should forward TryGetCurrentMachineId calls to.
        /// </summary>
        /// <param name="runtime"></param>
        void SetRuntime(PSharpRuntime runtime);

        /// <summary>
        /// Return true if the runtime is currently executing a machine's action.
        /// If it is, write its Id to the out parameter as a ulong.
        /// </summary>
        /// <param name="machineId"></param>
        /// <returns></returns>
        bool TryGetCurrentMachineId(out ulong machineId);

        /// <summary>
        /// Clear the internal state the reporter maintains.
        /// </summary>
        void ClearAll();
    }
}
