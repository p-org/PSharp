// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Abstract class representing a P# machine.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class BaseMachine
    {
        /// <summary>
        /// The runtime that executes this machine.
        /// </summary>
        internal BaseRuntime Runtime { get; private set; }

        /// <summary>
        /// The unique machine id.
        /// </summary>
        protected internal MachineId Id { get; private set; }

        /// <summary>
        /// Stores machine-related information, which can used
        /// for scheduling and testing.
        /// </summary>
        internal MachineInfo Info { get; private set; }

        /// <summary>
        /// Initializes this machine.
        /// </summary>
        internal void Initialize(BaseRuntime runtime, MachineId mid, MachineInfo info)
        {
            this.Runtime = runtime;
            this.Id = mid;
            this.Info = info;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is BaseMachine m &&
                this.GetType() == m.GetType())
            {
                return this.Id.Value == m.Id.Value;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            return this.Id.Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current machine.
        /// </summary>
        public override string ToString()
        {
            return this.Id.Name;
        }

        /// <summary>
        /// Returns the set of all states in the machine
        /// (for code coverage).
        /// </summary>
        internal virtual HashSet<string> GetAllStates()
        {
            return new HashSet<string>();
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the machine
        /// (for code coverage).
        /// </summary>
        internal virtual HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            return new HashSet<Tuple<string, string>>();
        }
    }
}
