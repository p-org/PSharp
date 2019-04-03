// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Stores machine-related information, which is used for various
    /// internal purposes, including scheduling and testing.
    /// </summary>
    internal class MachineInfo
    {
        /// <summary>
        /// Unique id of the machine.
        /// </summary>
        protected MachineId MachineId;

        /// <summary>
        /// Is the machine halted.
        /// </summary>
        internal bool IsHalted;

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
        /// <param name="mid">The machine id.</param>
        internal MachineInfo(MachineId mid)
        {
            this.MachineId = mid;
            this.IsHalted = false;
            this.IsWaitingToReceive = false;
            this.IsInsideOnExit = false;
            this.CurrentActionCalledTransitionStatement = false;
            this.OperationGroupId = Guid.Empty;
            this.ProgramCounter = 0;
        }

        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            MachineInfo mid = obj as MachineInfo;
            if (mid == null)
            {
                return false;
            }

            return this.MachineId == mid.MachineId;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.MachineId.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents this machine.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.MachineId.Name;
        }
    }
}
