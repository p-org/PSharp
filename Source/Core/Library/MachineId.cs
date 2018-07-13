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
using System.Collections.Concurrent;
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
        /// Unique id value typically used for testing.
        /// </summary>
        [DataMember]
        internal readonly ulong Value;

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
        internal MachineId(Type type, string friendlyName, PSharpRuntime runtime) : this(type.FullName, friendlyName, runtime)
        {
        }

        /// <summary>
        /// Creates a new machine id.
        /// </summary>
        /// <param name="type">Machine type string</param>
        /// <param name="friendlyName">Friendly machine name</param>
        /// <param name="runtime">PSharpRuntime</param>
        internal MachineId(string type, string friendlyName, PSharpRuntime runtime)
        {
            Type = type;
            Runtime = runtime;

            if (this.Runtime.IsTest())
            {
                this.Value = runtime.GenerateTestId();
                // Checks for overflow.
                Runtime.Assert(Value != ulong.MaxValue, "Detected MachineId overflow.");
            }

            if (string.IsNullOrWhiteSpace(friendlyName))
            {
                friendlyName = Runtime.GetFriendlyName(Type);
            }

            FriendlyName = friendlyName;
            Endpoint = Runtime.NetworkProvider.GetLocalEndpoint();
            Name = $"({Type})-{FriendlyName}";
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

            if (Runtime.IsTest())
            {
                return Value == mid.Value;
            }
            else
            {
                return Name == mid.Name;
            }
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            int hash = 17;
            if (Runtime.IsTest())
            {
                hash = hash * 23 + Value.GetHashCode();
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
