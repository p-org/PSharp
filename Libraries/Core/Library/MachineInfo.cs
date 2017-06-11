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

using Microsoft.PSharp.Scheduling;

namespace Microsoft.PSharp
{
    /// <summary>
    /// Stores machine-related information, which can used
    /// for scheduling and testing.
    /// </summary>
    internal sealed class MachineInfo : ISchedulable
    {
        #region fields

        /// <summary>
        /// Unique id of the machine.
        /// </summary>
        internal MachineId MachineId;

        /// <summary>
        /// Checks if the machine is executing an OnExit method.
        /// </summary>
        internal bool IsInsideOnExit;

        /// <summary>
        /// Checks if the current action called a transition statement.
        /// </summary>
        internal bool CurrentActionCalledTransitionStatement;

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

        /// <summary>
        /// Id of the task executing the event handler of the machine.
        /// </summary>
        public int TaskId { get; internal set; }

        /// <summary>
        /// Is machine enabled.
        /// </summary>
        public bool IsEnabled { get; internal set; }

        /// <summary>
        /// Is machine waiting to receive an event.
        /// </summary>
        public bool IsWaitingToReceive { get; internal set; }

        /// <summary>
        /// Is machine active.
        /// </summary>
        public bool IsActive { get; internal set; }

        /// <summary>
        /// Has the machine started.
        /// </summary>
        public bool HasStarted { get; internal set; }

        /// <summary>
        /// Is machine completed.
        /// </summary>
        public bool IsCompleted { get; internal set; }

        /// <summary>
        /// Type of the next operation of the machine.
        /// </summary>
        public OperationType NextOperationType { get; internal set; }

        /// <summary>
        /// Target id of the next operation of the machine.
        /// </summary>
        public int NextTargetId { get; internal set; }

        /// <summary>
        /// Monotonically increasing operation count.
        /// </summary>
        public int OperationCount { get; internal set; }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mid">MachineId</param>
        internal MachineInfo(MachineId mid)
        {
            MachineId = mid;

            IsEnabled = false;
            IsWaitingToReceive = false;
            IsActive = false;
            HasStarted = false;
            IsCompleted = false;

            IsInsideOnExit = false;
            CurrentActionCalledTransitionStatement = false;

            ProgramCounter = 0;
        }

        #endregion

        #region interface

        /// <summary>
        /// Notify that an event handler has been created and will
        /// run on the specified task id.
        /// </summary>
        /// <param name="taskId">TaskId</param>
        internal void NotifyEventHandlerCreated(int taskId)
        {
            TaskId = taskId;
            IsEnabled = true;
            IsWaitingToReceive = false;
            IsActive = false;
            HasStarted = false;
            IsCompleted = false;

            IsInsideOnExit = false;
            CurrentActionCalledTransitionStatement = false;

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

            return Id == mid.Id;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>int</returns>
        public override int GetHashCode()
        {
            return Id.GetHashCode();
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
