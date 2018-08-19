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
    public class MachineId : /*IMachineId,*/ IEquatable<MachineId>, IComparable<MachineId>
    {
        /// <summary>
        /// The runtime that executes the machine with this id.
        /// </summary>
        public IPSharpRuntime Runtime { get; private set; }

        /// <summary>
        /// Name of the machine.
        /// </summary>
        [DataMember]
        public string Name { get; }

        /// <summary>
        /// Optional friendly name of the machine.
        /// </summary>
        [DataMember]
        public string FriendlyName { get; }

        /// <summary>
        /// Type of the machine.
        /// </summary>
        [DataMember]
        public string Type { get; }

        /// <summary>
        /// Unique id value.
        /// </summary>
        [DataMember]
        public ulong Value { get; }

        /// <summary>
        /// Creates a new machine id.
        /// </summary>
        /// <param name="runtime">The P# runtime.</param>
        /// <param name="type">Machine type</param>
        /// <param name="value">Unique id value.</param>
        /// <param name="friendlyName">Friendly machine name</param>
        internal MachineId(IPSharpRuntime runtime, Type type, ulong value, string friendlyName)
            : this(runtime, type.FullName, friendlyName, value)
        { }

        /// <summary>
        /// Create a fresh machine id borrowing information from the specified id.
        /// </summary>
        /// <param name="mid">MachineId</param>
        internal MachineId(MachineId mid)
            : this(mid.Runtime, mid.Type, mid.FriendlyName, mid.Value)
        { }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">The P# runtime.</param>
        /// <param name="type">Machine type</param>
        /// <param name="friendlyName">Friendly machine name</param>
        /// <param name="value">Unique id value.</param>
        protected MachineId(IPSharpRuntime runtime, string type, string friendlyName, ulong value)
        {
            this.Runtime = runtime;
            this.Type = type;
            this.FriendlyName = friendlyName;
            this.Value = value;

            // Checks for overflow.
            this.Runtime.Assert(this.Value != ulong.MaxValue, "Detected MachineId overflow.");

            if (this.FriendlyName != null && this.FriendlyName.Length > 0)
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
        /// <param name="runtime">The P# runtime.</param>
        internal void Bind(IPSharpRuntime runtime)
        {
            this.Runtime = runtime;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal
        /// to the current <see cref="object"/>.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is MachineId mid)
            {
                return this.Value == mid.Value;
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

        //bool IEquatable<IMachineId>.Equals(IMachineId other)
        //{
        //    return this.Equals(other);
        //}

        //int IComparable<IMachineId>.CompareTo(IMachineId other)
        //{
        //    return string.Compare(this.Name, other?.Name);
        //}
    }
}
