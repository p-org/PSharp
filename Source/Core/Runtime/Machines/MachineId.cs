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
        public string Name { get; internal set; }

        /// <summary>
        /// Optional friendly name of the machine.
        /// </summary>
        [DataMember]
        private readonly string FriendlyName;

        /// <summary>
        /// Type of the machine with this id.
        /// </summary>
        [DataMember]
        public readonly string Type;

        /// <summary>
        /// Unique id value.
        /// </summary>
        [DataMember]
        public ulong Value { get; internal set; }

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

        /// <summary>
        /// Endpoint.
        /// </summary>
        [DataMember]
        public readonly bool UseFriendlyNameForHashing;

        #endregion

        #region constructors

        /// <summary>
        /// Creates a new machine id.
        /// </summary>
        /// <param name="type">Machine type</param>
        /// <param name="friendlyName">Friendly machine name</param>
        /// <param name="runtime">PSharpRuntime</param>
        /// <param name="useFriendlyNameHash">If friendly name needs to be used for hashing</param>
        internal MachineId(Type type, string friendlyName, PSharpRuntime runtime, bool useFriendlyNameHash = false)
        {
            FriendlyName = friendlyName;
            Runtime = runtime;
            Endpoint = Runtime.NetworkProvider.GetLocalEndpoint();
            UseFriendlyNameForHashing = useFriendlyNameHash;
            Type = type.FullName;

            this.GenerateNameAndValue();
            
            // Checks for overflow.
            Runtime.Assert(Value != ulong.MaxValue, "Detected MachineId overflow.");
            Generation = runtime.Configuration.RuntimeGeneration;            
        }

        /// <summary>
        /// Given the set of parameters - generate a name and setup value for hashing
        /// </summary>
        private void GenerateNameAndValue()
        {
            if (!UseFriendlyNameForHashing)
            {
                // Atomically increments and safely wraps into an unsigned long.
                Value = (ulong)Interlocked.Increment(ref Runtime.MachineIdCounter) - 1;

                if (FriendlyName != null && FriendlyName.Length > 0)
                {
                    Name = string.Format("{0}({1})", FriendlyName, Value);
                }
                else
                {
                    Name = string.Format("{0}({1})", Type, Value);
                }
            }
            else
            {
                Name = $"{FriendlyName}({Type})";
                Value = (ulong) Name.GetHashCode();
            }
        }

        /// <summary>
        /// Create a fresh MachineId borrowing information from a given id.
        /// </summary>
        /// <param name="mid">MachineId</param>
        internal MachineId(MachineId mid)
        {
            Runtime = mid.Runtime;
            Endpoint = mid.Endpoint;
            FriendlyName = mid.FriendlyName;
            UseFriendlyNameForHashing = mid.UseFriendlyNameForHashing;
            Type = mid.Type;

            this.GenerateNameAndValue();

            // Checks for overflow.
            Runtime.Assert(Value != ulong.MaxValue, "Detected MachineId overflow.");
            Generation = mid.Generation;
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

            if (!UseFriendlyNameForHashing)
            {
                return Value == mid.Value && Generation == mid.Generation;
            }

            return Name == mid.Name;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            int hash = 17;

            if (!UseFriendlyNameForHashing)
            {
                hash = hash * 23 + Value.GetHashCode();
                hash = hash * 23 + Generation.GetHashCode();
            }
            else
            {
                hash = hash * 23 + Name.GetHashCode();
            }

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
