//-----------------------------------------------------------------------
// <copyright file="MachineInfo.cs">
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

namespace Microsoft.PSharp
{
    /// <summary>
    /// Stores machine-related information, which can used
    /// for scheduling and testing.
    /// </summary>
    internal class MachineInfo
    {
        #region fields

        /// <summary>
        /// Unique id of the machine.
        /// </summary>
        protected MachineId MachineId;

        /// <summary>
        /// Is the machine halted.
        /// </summary>
        internal bool IsHalted;

        /// <summary>
        /// Is the machine waiting to receive an event.
        /// </summary>
        internal bool IsWaitingToReceive;

        /// <summary>
        /// Checks if the machine is executing an OnExit method.
        /// </summary>
        internal bool IsInsideOnExit;

        /// <summary>
        /// Checks if the current action called a transition statement.
        /// </summary>
        internal bool CurrentActionCalledTransitionStatement;

        /// <summary>
        /// Unique id of the group of operations that the
        /// machine is currently executing.
        /// </summary>
        internal Guid OperationGroupId;

        /// <summary>
        /// Program counter used for state-caching. Distinguishes
        /// scheduling from non-deterministic choices.
        /// </summary>
        internal int ProgramCounter;

        #endregion

        #region properties

        /// <summary>
        /// Unique id of the machine.
        /// </summary>
        public ulong Id => MachineId.Value;

        /// <summary>
        /// Name of the machine.
        /// </summary>
        public string Name => MachineId.Name;

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mid">MachineId</param>
        internal MachineInfo(MachineId mid)
        {
            MachineId = mid;
            IsHalted = false;
            IsWaitingToReceive = false;
            IsInsideOnExit = false;
            CurrentActionCalledTransitionStatement = false;
            OperationGroupId = Guid.Empty;
            ProgramCounter = 0;
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

            MachineInfo mid = obj as MachineInfo;
            if (mid == null)
            {
                return false;
            }

            return MachineId == mid.MachineId;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return MachineId.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents this machine.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return MachineId.Name;
        }

        #endregion
    }
}
