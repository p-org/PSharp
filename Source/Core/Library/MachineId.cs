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
using System.Collections.Generic;
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
        /// Unique id value.
        /// </summary>
        [DataMember]
        internal readonly int Value;

        /// <summary>
        /// Machine-type-specific id value.
        /// </summary>
        [DataMember]
        internal readonly int MVal;

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
        private static int IdCounter;

        /// <summary>
        /// Id specific to a machine type.
        /// </summary>
        private static Dictionary<Type, int> TypeIdCounter;

        #endregion

        #region internal API

        /// <summary>
        /// Static constructor.
        /// </summary>
        static MachineId()
        {
            MachineId.IdCounter = 0;
            MachineId.TypeIdCounter = new Dictionary<Type, int>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">Type</param>
        internal MachineId(Type type)
        {
            if (!MachineId.TypeIdCounter.ContainsKey(type))
            {
                MachineId.TypeIdCounter.Add(type, 0);
            }

            this.Value = MachineId.IdCounter++;
            this.MVal = MachineId.TypeIdCounter[type]++;
            this.IpAddress = "";
            this.Port = "";
        }

        /// <summary>
        /// Resets the machine id counter.
        /// </summary>
        internal static void ResetMachineIDCounter()
        {
            MachineId.IdCounter = 0;
            MachineId.TypeIdCounter.Clear();
        }

        #endregion
    }
}
