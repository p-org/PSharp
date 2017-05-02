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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Class implementing machine related information for scheduling purposes.
    /// </summary>
    public sealed class MachineInfo
    {
        #region fields

        /// <summary>
        /// The corresponding machine.
        /// </summary>
        internal AbstractMachine Machine;

        /// <summary>
        /// Task id of the machine.
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// List of wrapped tasks that block the machine.
        /// </summary>
        internal List<MachineInfo> BlockingWrappedTasks;

        /// <summary>
        /// List of tasks that block the machine.
        /// </summary>
        internal List<Task> BlockingUnwrappedTasks;

        /// <summary>
        /// True if the machine should wait all blocking
        /// tasks to complete, before unblocking.
        /// </summary>
        internal bool WaitAll;

        /// <summary>
        /// Is machine enabled.
        /// </summary>
        public bool IsEnabled
        {
            get; internal set;
        }

        /// <summary>
        /// Is machine waiting to receive an event.
        /// </summary>
        public bool IsWaitingToReceive
        {
            get; internal set;
        }

        /// <summary>
        /// Is machine active.
        /// </summary>
        public bool IsActive
        {
            get; internal set;
        }

        /// <summary>
        /// Has the machine started.
        /// </summary>
        public bool HasStarted
        {
            get; internal set;
        }

        /// <summary>
        /// Is machine completed.
        /// </summary>
        public bool IsCompleted
        {
            get; internal set;
        }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">TaskId</param>
        /// <param name="machine">Machine</param>
        internal MachineInfo(int id, AbstractMachine machine)
        {
            this.Id = id;
            this.Machine = machine;
            this.IsEnabled = true;
            this.IsWaitingToReceive = false;
            this.IsActive = false;
            this.HasStarted = false;
            this.IsCompleted = false;

            this.BlockingWrappedTasks = new List<MachineInfo>();
            this.BlockingUnwrappedTasks = new List<Task>();
            this.WaitAll = false;
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

            return this.Id == mid.Id;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return this.Id.GetHashCode();
        }

        /// <summary>
        /// Returns a string that represents the current machine id.
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            var text = $"Task {this.Id} of machine {this.Machine.Id}::" +
                $"enabled[{this.IsEnabled}], waiting[{this.IsWaitingToReceive}], " +
                $"active[{this.IsActive}], started[{this.HasStarted}], " +
                $"completed[{this.IsCompleted}]";
            return text;
        }

        #endregion
    }
}
