//-----------------------------------------------------------------------
// <copyright file="SchedulableInfo.cs">
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

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Stores information for a schedulable entity that can be
    /// used during scheduling and testing.
    /// </summary>
    internal sealed class SchedulableInfo : MachineInfo, ISchedulable
    {
        #region properties

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
        /// The target type of the next operation of the entity.
        /// </summary>
        public OperationTargetType NextTargetType { get; internal set; }

        /// <summary>
        /// Target id of the next operation of the machine.
        /// </summary>
        public int NextTargetId { get; internal set; }

        /// <summary>
        /// If the next operation is <see cref="OperationType.Receive"/>
        /// then this gives the step index of the corresponding Send. 
        /// </summary>
        public int NextOperationMatchingSendIndex { get; internal set; }

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
        internal SchedulableInfo(MachineId mid)
            : base(mid)
        {
            IsEnabled = false;
            IsWaitingToReceive = false;
            IsActive = false;
            HasStarted = false;
            IsCompleted = false;
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
    }
}
