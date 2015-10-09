//-----------------------------------------------------------------------
// <copyright file="Id.cs" company="Microsoft">
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
    public sealed class Id
    {
        #region fields

        /// <summary>
        /// Unique id value.
        /// </summary>
        [DataMember]
        internal readonly int Value;

        /// <summary>
        /// Type name.
        /// </summary>
        [DataMember]
        internal readonly string Type;

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
        static Id()
        {
            Id.IdCounter = 0;
            Id.TypeIdCounter = new Dictionary<Type, int>();
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="type">Type</param>
        internal Id(Type type)
        {
            lock (Id.TypeIdCounter)
            {
                if (!Id.TypeIdCounter.ContainsKey(type))
                {
                    Id.TypeIdCounter.Add(type, 0);
                }

                this.Value = Id.IdCounter++;
                this.Type = type.Name;
                this.MVal = Id.TypeIdCounter[type]++;
                this.IpAddress = "";
                this.Port = "";
            }
        }

        /// <summary>
        /// Resets the machine id counter.
        /// </summary>
        internal static void ResetMachineIDCounter()
        {
            Id.IdCounter = 0;
            Id.TypeIdCounter.Clear();
        }

        #endregion

        #region generic public and override methods
        
        /// <summary>
        /// Determines whether the specified System.Object is equal
        /// to the current System.Object.
        /// </summary>
        /// <param name="obj">Object</param>
        /// <returns>Boolean value</returns>
        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            Id mid = obj as Id;
            if (mid == null)
            {
                return false;
            }

            return this.Value == mid.Value;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current machine id.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            var text = "[" + this.Type + "," + this.MVal + "," +
                this.IpAddress + "," + this.Port + "]";
            return text;
        }

        #endregion
    }
}
