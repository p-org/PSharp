//-----------------------------------------------------------------------
// <copyright file="MachineId.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
//
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.Serialization;

using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Unique machine id.
    /// </summary>
    [DataContract]
    public sealed class MachineId : IEquatable<MachineId>, IComparable<MachineId>
    {
        /// <summary>
        /// The runtime manager that executes the machine with this id.
        /// </summary>
        internal IRuntimeMachineManager RuntimeManager { get; private set; }

        /// <summary>
        /// Name of the machine.
        /// </summary>
        [DataMember]
        public readonly string Name;

        /// <summary>
        /// Optional friendly name of the machine.
        /// </summary>
        [DataMember]
        public readonly string FriendlyName;

        /// <summary>
        /// Type of the machine with this id.
        /// </summary>
        [DataMember]
        public readonly string Type;

        /// <summary>
        /// Unique id value.
        /// </summary>
        [DataMember]
        public readonly ulong Value;

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
        /// Creates a new machine id.
        /// </summary>
        /// <param name="runtimeManager">The runtime machine manager.</param>
        /// <param name="type">Machine type</param>
        /// <param name="value">Unique id value.</param>
        /// <param name="friendlyName">Friendly machine name</param>
        internal MachineId(IRuntimeMachineManager runtimeManager, Type type, ulong value, string friendlyName)
            : this(runtimeManager, type.FullName, friendlyName, value, runtimeManager.Configuration.RuntimeGeneration,
                  runtimeManager.NetworkProvider.LocalEndpoint)
        { }

        /// <summary>
        /// Create a fresh machine id borrowing information from the specified id.
        /// </summary>
        /// <param name="mid">MachineId</param>
        internal MachineId(MachineId mid)
            : this(mid.RuntimeManager, mid.Type, mid.FriendlyName, mid.Value, mid.Generation, mid.Endpoint)
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtimeManager">The runtime machine manager.</param>
        /// <param name="type">Machine type</param>
        /// <param name="friendlyName">Friendly machine name</param>
        /// <param name="value">Unique id value.</param>
        /// <param name="generation">Runtime generation</param>
        /// <param name="endpoint">Endpoint</param>
        private MachineId(IRuntimeMachineManager runtimeManager, string type, string friendlyName, ulong value, ulong generation, string endpoint)
        {
            this.RuntimeManager = runtimeManager;
            this.Type = type;
            this.FriendlyName = friendlyName;
            this.Value = value;
            this.Generation = generation;
            this.Endpoint = endpoint;

            // Checks for overflow.
            this.RuntimeManager.Assert(this.Value != ulong.MaxValue, "Detected MachineId overflow.");

            if (this.Endpoint != null && this.Endpoint.Length > 0 && this.FriendlyName != null && this.FriendlyName.Length > 0)
            {
                this.Name = string.Format("{0}.{1}({2})", this.Endpoint, this.FriendlyName, this.Value);
            }
            else if (this.Endpoint != null && this.Endpoint.Length > 0)
            {
                this.Name = string.Format("{0}({1})", this.Type, this.Value);
            }
            else if (this.FriendlyName != null && this.FriendlyName.Length > 0)
            {
                this.Name = string.Format("{0}({1})", this.FriendlyName, this.Value);
            }
            else
            {
                this.Name = string.Format("{0}({1})", this.Type, this.Value);
            }
        }

        /// <summary>
        /// Bind the machine id.
        /// </summary>
        /// <param name="runtimeManager">The runtime machine manager.</param>
        internal void Bind(IRuntimeMachineManager runtimeManager)
        {
            this.RuntimeManager = runtimeManager;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal
        /// to the current <see cref="object"/>.
        /// </summary>
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

            return this.Value == mid.Value && this.Generation == mid.Generation;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + this.Value.GetHashCode();
            hash = hash * 23 + this.Generation.GetHashCode();
            return hash;
        }

        /// <summary>
        /// Returns a string that represents the current machine id.
        /// </summary>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Indicates whether the specified <see cref="MachineId"/> is equal
        /// to the current <see cref="MachineId"/>.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>true if the current object is equal to the other parameter; otherwise, false.</returns>
        public bool Equals(MachineId other)
        {
            return this.Equals((object)other);
        }

        /// <summary>
        /// Compares the specified <see cref="MachineId"/> with the current
        /// <see cref="MachineId"/> for ordering or sorting purposes.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public int CompareTo(MachineId other)
        {
            return string.Compare(this.Name, other?.Name);
        }
    }
}
