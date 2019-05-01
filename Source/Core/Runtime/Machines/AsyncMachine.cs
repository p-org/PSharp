// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.ComponentModel;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Implements a machine that can execute asynchronously.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class AsyncMachine
    {
        /// <summary>
        /// The runtime that executes this machine.
        /// </summary>
        internal MachineRuntime Runtime { get; private set; }

        /// <summary>
        /// The unique machine id.
        /// </summary>
        protected internal MachineId Id { get; private set; }

        /// <summary>
        /// Unique id of the group of operations that is
        /// associated with the next operation.
        /// </summary>
        protected internal Guid OperationGroupId { get; set; }

        /// <summary>
        /// Initializes this machine.
        /// </summary>
        internal void Initialize(MachineRuntime runtime, MachineId mid)
        {
            this.Runtime = runtime;
            this.Id = mid;
            this.OperationGroupId = Guid.Empty;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is AsyncMachine m &&
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
    }
}
