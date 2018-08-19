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
    public sealed class MachineId : BaseMachineId, IEquatable<MachineId>, IComparable<MachineId>
    {
        /// <summary>
        /// The runtime that executes the machine with this id.
        /// </summary>
        public IPSharpRuntime Runtime { get; private set; }

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
        private MachineId(IPSharpRuntime runtime, string type, string friendlyName, ulong value)
            : base(type, friendlyName, value)
        {
            this.Runtime = runtime;

            if (friendlyName != null && friendlyName.Length > 0)
            {
                this.Name = string.Format("{0}({1})", friendlyName, value);
            }
            else
            {
                this.Name = string.Format("{0}({1})", type, value);
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
