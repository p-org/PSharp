﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.Serialization;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Contains the origin information of an <see cref="Event"/>. 
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
        internal EventOriginInfo(MachineId senderMachineId, string senderMachineName, string senderStateName)
        {
            this.SenderMachineId = senderMachineId;
            this.SenderMachineName = senderMachineName;
            this.SenderStateName = senderStateName;
        }
    }
}
