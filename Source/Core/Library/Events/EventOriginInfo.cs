// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Class containing origin information
    /// regarding an event.
    /// </summary>
    [DataContract]
    internal class EventOriginInfo
    {
        /// <summary>
        /// The sender machine id.
        /// </summary>
        [DataMember]
        internal MachineId SenderMachineId { get; private set; }

        /// <summary>
        /// The sender machine name.
        /// </summary>
        [DataMember]
        internal string SenderMachineName { get; private set; }

        /// <summary>
        /// The sender machine state name.
        /// </summary>
        [DataMember]
        internal string SenderStateName { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="senderMachineId">Sender machine id</param>
        /// <param name="senderMachineName">Sender machine name</param>
        /// <param name="senderStateName">Sender state name</param>
        internal EventOriginInfo(MachineId senderMachineId, string senderMachineName,
            string senderStateName)
        {
            this.SenderMachineId = senderMachineId;
            this.SenderMachineName = senderMachineName;
            this.SenderStateName = senderStateName;
        }
    }
}
