// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.Serialization;
using System.Threading;

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
        /// The P# runtime that executes the machine with this id.
        /// </summary>
        public PSharpRuntime Runtime { get; private set; }

        /// <summary>
        /// Name of the machine.
        /// </summary>
        [DataMember]
        public readonly string Name;

        /// <summary>
        /// Type of the machine with this id.
        /// </summary>
        [DataMember]
        public readonly string Type;

        /// <summary>
        /// Unique id value, when <see cref="StringValue"/> is null
        /// </summary>
        [DataMember]
        public readonly ulong Value;

        /// <summary>
        /// Unique name, when non-null.
        /// </summary>
        [DataMember]
        private readonly string StringValue;

        /// <summary>
        /// Generation of the runtime that created this machine id.
        /// </summary>
        [DataMember]
        public readonly ulong Generation;

        /// <summary>
        /// Endpoint.
        /// </summary>
        [DataMember]
        public readonly string Endpoint;

        #endregion

        #region constructors

        /// <summary>
        /// Creates a new machine id.
        /// </summary>
        /// <param name="type">Machine type</param>
        /// <param name="friendlyName">Friendly machine name</param>
        /// <param name="runtime">PSharpRuntime</param>
        /// <param name="useNameForHashing">Use friendly name as the id</param>
        internal MachineId(Type type, string friendlyName, PSharpRuntime runtime, bool useNameForHashing = false)
        {
            Runtime = runtime;
            Endpoint = Runtime.NetworkProvider.GetLocalEndpoint();

            if (useNameForHashing)
            {
                Value = 0;
                StringValue = friendlyName;
                Runtime.Assert(StringValue != null, "Input friendlyName cannot be null when used as Id");
            }
            else
            {
                // Atomically increments and safely wraps into an unsigned long.
                Value = (ulong)Interlocked.Increment(ref runtime.MachineIdCounter) - 1;
                StringValue = null;

                // Checks for overflow.
                Runtime.Assert(Value != ulong.MaxValue, "Detected MachineId overflow.");
            }

            Generation = runtime.Configuration.RuntimeGeneration;

            Type = type.FullName;
            if(StringValue != null)
            {
                Name = StringValue;
            }
            else 
            {
                Name = string.Format("{0}({1})", string.IsNullOrEmpty(friendlyName) ? Type : friendlyName, Value);
            }
        }

        /// <summary>
        /// Bind the machine id.
        /// </summary>
        /// <param name="runtime">PSharpRuntime</param>
        internal void Bind(PSharpRuntime runtime)
        {
            Runtime = runtime;
        }

        #endregion

        #region generic public and override methods
        
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

            MachineId mid = obj as MachineId;
            if (mid == null)
            {
                return false;
            }

            // Either both are null, or both are non-null
            if ((StringValue == null) != (mid.StringValue == null))
            {
                return false;
            }

            return StringValue == null ?
                Value == mid.Value && Generation == mid.Generation :
                StringValue.Equals(mid.StringValue) && Generation == mid.Generation;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (StringValue == null ? Value.GetHashCode() : StringValue.GetHashCode());
            hash = hash * 23 + Generation.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string that represents the current machine id.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}
