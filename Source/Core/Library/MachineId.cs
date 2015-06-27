//-----------------------------------------------------------------------
// <copyright file="MachineId.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Unique machine id.
    /// </summary>
    [DataContract]
    public sealed class MachineId
    {
        #region fields

        /// <summary>
        /// Id value.
        /// </summary>
        [DataMember]
        internal readonly int Value;

        /// <summary>
        /// Ip address.
        /// </summary>
        [DataMember]
        internal string IpAddress;

        /// <summary>
        /// Port.
        /// </summary>
        [DataMember]
        internal string Port;

        #endregion

        #region static fields

        /// <summary>
        /// Monotonically increasing machine id counter.
        /// </summary>
        private static int IdCounter = 0;

        #endregion

        #region internal API

        /// <summary>
        /// Constructor.
        /// </summary>
        internal MachineId()
        {
            this.Value = MachineId.IdCounter++;
            this.IpAddress = "";
            this.Port = "";
        }

        /// <summary>
        /// Resets the machine ID counter.
        /// </summary>
        internal static void ResetMachineIDCounter()
        {
            MachineId.IdCounter = 0;
        }

        #endregion

        #region public API

        /// <summary>
        /// Models the given machine.
        /// </summary>
        /// <param name="type">Type of the machine</param>
        /// <returns>Machine id</returns>
        public MachineId Models(Type type)
        {
            // Only used for rewriting purposes.
            return this;
        }

        #endregion
    }
}
