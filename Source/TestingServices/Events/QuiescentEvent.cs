// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Signals that a machine has reached Quiescence
    /// </summary>
    [DataContract]
    internal sealed class QuiescentEvent : Event
    {
        /// <summary>
        /// MachineId
        /// </summary>
        public MachineId mid;

        /// <summary>
        /// Constructor.
        /// </summary>
        public QuiescentEvent(MachineId mid)
            : base()
        {
            this.mid = mid;
        }
    }
}
