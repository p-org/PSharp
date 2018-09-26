// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Timers
{
    /// <summary>
    /// Unique identifier for a timer 
    /// </summary>
    public class TimerId 
    {
        /// <summary>
        /// The timer machine id
        /// </summary>
        internal readonly MachineId mid;

        /// <summary>
        /// Payload
        /// </summary>
        public readonly object Payload;

        /// <summary>
        /// Initializes a timer id
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="Payload">Payload</param>
        internal TimerId(MachineId mid, object Payload)
        {
            this.mid = mid;
            this.Payload = Payload;
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

            var tid = obj as TimerId;
            if (tid == null)
            {
                return false;
            }

            return mid == tid.mid;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return mid.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current timer id.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return string.Format("Timer[{0},{1}]", mid, Payload != null ? Payload.ToString() : "null");
        }

    }
}
