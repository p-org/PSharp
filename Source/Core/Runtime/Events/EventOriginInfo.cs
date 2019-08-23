﻿// ------------------------------------------------------------------------------------------------

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
        /// Initializes a new instance of the <see cref="EventOriginInfo"/> class.
        /// </summary>
        internal EventOriginInfo(MachineId senderMachineId, string senderMachineName, string senderStateName)
        {
            this.SenderMachineId = senderMachineId;
            this.SenderMachineName = senderMachineName;
            this.SenderStateName = senderStateName;
        }
    }
}
