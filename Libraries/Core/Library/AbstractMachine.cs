//-----------------------------------------------------------------------
// <copyright file="AbstractMachine.cs">
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

using System.ComponentModel;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Abstract class representing a P# machine.
    /// </summary>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class AbstractMachine
    {
        #region fields

        /// <summary>
        /// The P# runtime that executes this machine.
        /// </summary>
        internal PSharpRuntime Runtime { get; private set; }

        /// <summary>
        /// The unique machine id.
        /// </summary>
        protected internal MachineId Id { get; private set; }

        /// <summary>
        /// The operation id.
        /// </summary>
        internal int OperationId { get; private set; }

        /// <summary>
        /// (Optional) Friendly name for the machine
        /// </summary>
        protected internal string _friendlyName;

        /// <summary>
        /// Return the unique friendly name for the machine
        /// </summary>
        public string UniqueFriendlyName
        {
            get
            {
                if (_friendlyName != null)
                    return string.Format("{0}({1})", _friendlyName, Id.Value);
                else
                    return string.Format("{0}({1})", this.GetType().Name, Id.MVal);
            }
        }

        #endregion

        #region generic public and override methods

        /// <summary>
        /// Constructor.
        /// </summary>
        public AbstractMachine()
        {
            this.OperationId = 0;
            this._friendlyName = null;
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

            AbstractMachine m = obj as AbstractMachine;
            if (m == null ||
                this.GetType() != m.GetType())
            {
                return false;
            }

            return this.Id.Value == m.Id.Value;
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
            if (UniqueFriendlyName != null) return UniqueFriendlyName;
            else return this.GetType().Name;
        }

        #endregion

        #region internal methods
        
        /// <summary>
        /// Sets the id of this machine.
        /// </summary>
        /// <param name="mid">MachineId</param>
        internal void SetMachineId(MachineId mid)
        {
            this.Id = mid;
            this.Runtime = mid.Runtime;
        }

        /// <summary>
        /// Sets the operation id of this machine.
        /// </summary>
        /// <param name="opid">OperationId</param>
        internal void SetOperationId(int opid)
        {
            this.OperationId = opid;
        }

        /// <summary>
        /// Sets the friendly name of the machine
        /// </summary>
        /// <param name="friendlyName">Friendly name</param>
        internal void SetFriendlyName(string friendlyName)
        {
            this._friendlyName = friendlyName;
        }

        /// <summary>
        /// Returns true if the given operation id is pending
        /// execution by the machine.
        /// </summary>
        /// <param name="opid">OperationId</param>
        /// <returns>Boolean</returns>
        internal virtual bool IsOperationPending(int opid)
        {
            return false;
        }

        #endregion
    }
}
