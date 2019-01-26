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

using ProtoBuf;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Unique machine id.
    /// </summary>
    [DataContract]
    [ProtoContract]
    public sealed class MachineId : IEquatable<MachineId>, IComparable<MachineId>
    {
        /// <summary>
        /// Proxy to the runtime that executes the machine with this id.
        /// </summary>
        internal IMachineRuntimeProxy RuntimeProxy;

        /// <summary>
        /// Unique id value.
        /// </summary>
        [DataMember]
        [ProtoMember(1)]
        public ulong Value { get; private set; }

        /// <summary>
        /// Type of the machine.
        /// </summary>
        [DataMember]
        [ProtoMember(2)]
        public string Type { get; private set; }

        /// <summary>
        /// Name of the machine.
        /// </summary>
        [DataMember]
        [ProtoMember(3)]
        public string Name { get; private set; }

        /// <summary>
        /// Optional friendly name of the machine.
        /// </summary>
        [DataMember]
        [ProtoMember(4)]
        public string FriendlyName { get; private set; }

        /// <summary>
        /// The endpoint where the machine with this id is located.
        /// </summary>
        [DataMember]
        [ProtoMember(5)]
        public string Endpoint { get; private set; }

        /// <summary>
        /// The generation of the runtime this machine id was created.
        /// </summary>
        [DataMember]
        [ProtoMember(6)]
        public ulong RuntimeGeneration { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtimeProxy">Proxy to the machine runtime.</param>
        /// <param name="type">Machine type</param>
        /// <param name="value">The unique id value.</param>
        /// <param name="friendlyName">The friendly machine name.</param>
        /// <param name="endpoint">The machine endpoint.</param>
        /// <param name="runtimeGeneration">The generation of the runtime.</param>
        internal MachineId(IMachineRuntimeProxy runtimeProxy, string type, ulong value, string friendlyName,
            string endpoint, ulong runtimeGeneration)
        {
            this.RuntimeProxy = runtimeProxy;
            this.Type = type;
            this.Value = value;
            this.FriendlyName = friendlyName;
            this.Endpoint = string.Empty;
            this.RuntimeGeneration = runtimeGeneration;

            if (this.Endpoint != null && this.Endpoint.Length > 0 && this.FriendlyName != null && this.FriendlyName.Length > 0)
            {
                this.Name = string.Format("{0}/{1}({2}.{3})", this.Endpoint, this.FriendlyName, this.RuntimeGeneration, this.Value);
            }
            else if (this.Endpoint != null && this.Endpoint.Length > 0)
            {
                this.Name = string.Format("{0}/{1}({2}.{3})", this.Endpoint, type, this.RuntimeGeneration, this.Value);
            }
            else if (this.FriendlyName != null && this.FriendlyName.Length > 0)
            {
                this.Name = string.Format("{0}({1}.{2})", this.FriendlyName, this.RuntimeGeneration, this.Value);
            }
            else
            {
                this.Name = string.Format("{0}({1}.{2})", type, this.RuntimeGeneration, this.Value);
            }
        }

        /// <summary>
        /// Returns a proxy to the runtime that executes the machine with this id.
        /// </summary>
        /// <returns>The runtime proxy.</returns>
        public IMachineRuntimeProxy GetRuntimeProxy() => this.RuntimeProxy;

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal
        /// to the current <see cref="object"/>.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is MachineId mid)
            {
                return this.Value == mid.Value && this.RuntimeGeneration == mid.RuntimeGeneration && this.Endpoint == mid.Endpoint;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + this.Value.GetHashCode();
            hash = hash * 23 + this.RuntimeGeneration.GetHashCode();
            hash = hash * 23 + this.Endpoint.GetHashCode();
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
        public int CompareTo(MachineId other)
        {
            return string.Compare(this.Name, other?.Name);
        }

        bool IEquatable<MachineId>.Equals(MachineId other)
        {
            return this.Equals(other);
        }

        int IComparable<MachineId>.CompareTo(MachineId other)
        {
            return string.Compare(this.Name, other?.Name);
        }
    }
}
