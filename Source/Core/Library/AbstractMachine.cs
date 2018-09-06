// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a P# machine.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class AbstractMachine
    {
        /// <summary>
        /// The runtime that executes this machine.
        /// </summary>
        internal PSharpRuntime Runtime { get; private set; }

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
        /// <param name="runtime">PSharpRuntime</param>
        /// <param name="mid">MachineId</param>
        /// <param name="info">MachineInfo</param>
        internal void Initialize(PSharpRuntime runtime, MachineId mid, MachineInfo info)
        {
            this.Runtime = mid.Runtime;
            this.Id = mid;
            this.Info = info;
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

            AbstractMachine m = obj as AbstractMachine;
            if (m == null ||
                this.GetType() != m.GetType())
            {
                return false;
            }

            return this.Id.Value == m.Id.Value;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Id.Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current machine.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.Id.Name;
        }

        /// <summary>
        /// Returns the set of all states in the machine
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all states in the machine</returns>
        internal virtual HashSet<string> GetAllStates()
        {
            return new HashSet<string>();
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the machine
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all (states, registered event) pairs in the machine</returns>
        internal virtual HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            return new HashSet<Tuple<string, string>>();
        }
    }
}
