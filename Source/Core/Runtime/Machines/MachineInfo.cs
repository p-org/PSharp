// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Stores machine-related metadata.
    /// </summary>
    internal class MachineInfo
    {
        /// <summary>
        /// Unique id of the machine.
        /// </summary>
        protected MachineId MachineId;

        /// <summary>
        /// True if the event handler for the machine is running, else false.
        /// </summary>
        internal bool IsEventHandlerRunning;

        /// <summary>
        /// Is the machine halted.
        /// </summary>
        internal volatile bool IsHalted;

        /// <summary>
        /// Is the machine waiting to receive an event.
        /// </summary>
        internal bool IsWaitingToReceive;

        /// <summary>
        /// Checks if the machine is executing an OnExit method.
        /// </summary>
        internal bool IsInsideOnExit;

        /// <summary>
        /// Checks if the current action called a transition statement.
        /// </summary>
        internal bool CurrentActionCalledTransitionStatement;

        /// <summary>
        /// Unique id of the group of operations that the
        /// machine is currently executing.
        /// </summary>
        internal Guid OperationGroupId;

        /// <summary>
        /// Program counter used for state-caching. Distinguishes
        /// scheduling from non-deterministic choices.
        /// </summary>
        internal int ProgramCounter;

        /// <summary>
        /// Unique id of the machine.
        /// </summary>
        public ulong Id => this.MachineId.Value;

        /// <summary>
        /// Name of the machine.
        /// </summary>
        public string Name => this.MachineId.Name;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineInfo"/> class.
        /// </summary>
        internal MachineInfo(MachineId mid)
        {
            this.MachineId = mid;

            // The event handler starts in the running state, because when the machine
            // gets initialized it will always run one iteration of the handler.
            this.IsEventHandlerRunning = true;

            this.IsHalted = false;
            this.IsWaitingToReceive = false;
            this.IsInsideOnExit = false;
            this.CurrentActionCalledTransitionStatement = false;

            this.OperationGroupId = Guid.Empty;
            this.ProgramCounter = 0;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is MachineInfo mid)
            {
                return this.MachineId == mid.MachineId;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => this.MachineId.GetHashCode();

        /// <summary>
        /// Returns a string that represents this machine.
        /// </summary>
        public override string ToString() => this.MachineId.Name;
    }
}
