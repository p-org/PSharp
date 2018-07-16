//-----------------------------------------------------------------------
// <copyright file="BaseMachine.cs">
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
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.PSharp.Runtime
{
    /// <summary>
    /// Base abstract class of a P# machine.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class BaseMachine
    {
        /// <summary>
        /// The unique machine id.
        /// </summary>
        protected internal MachineId Id { get; private set; }

        /// <summary>
        /// Stores machine-related information, which can used
        /// for scheduling and testing.
        /// </summary>
        internal MachineInfo Info { get; private set; }

        /// <summary>
        /// Initializes this machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        /// <param name="info">MachineInfo</param>
        internal void Initialize(MachineId mid, MachineInfo info)
        {
            this.Id = mid;
            this.Info = info;
        }

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

            BaseMachine m = obj as BaseMachine;
            if (m == null ||
                this.GetType() != m.GetType())
            {
                return false;
            }

            return this.Id.Equals(m.Id);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Id.Value.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current machine.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.Id.Name;
        }

        /// <summary>
        /// Returns the set of all states in the machine
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all states in the machine</returns>
        internal virtual HashSet<string> GetAllStates()
        {
            return new HashSet<string>();
        }

        /// <summary>
        /// Returns the set of all (states, registered event) pairs in the machine
        /// (for code coverage).
        /// </summary>
        /// <returns>Set of all (states, registered event) pairs in the machine</returns>
        internal virtual HashSet<Tuple<string, string>> GetAllStateEventPairs()
        {
            return new HashSet<Tuple<string, string>>();
        }
    }
}
