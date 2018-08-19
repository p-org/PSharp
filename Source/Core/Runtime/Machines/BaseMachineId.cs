//-----------------------------------------------------------------------
// <copyright file="BaseMachineId.cs">
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

using System.Runtime.Serialization;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Unique machine id.
    /// </summary>
    [DataContract]
    public abstract class BaseMachineId : IMachineId
    {
        /// <summary>
        /// Name of the machine.
        /// </summary>
        [DataMember]
        public string Name { get; protected set; }

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
        /// Constructor.
        /// </summary>
        /// <param name="type">Machine type</param>
        /// <param name="friendlyName">Friendly machine name</param>
        /// <param name="value">Unique id value.</param>
        protected BaseMachineId(string type, string friendlyName, ulong value)
        {
            this.Type = type;
            this.FriendlyName = friendlyName;
            this.Value = value;
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal
        /// to the current <see cref="object"/>.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is BaseMachineId mid)
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
    }
}
