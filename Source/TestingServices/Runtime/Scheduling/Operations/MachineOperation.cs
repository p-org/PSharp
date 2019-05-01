// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Contains information about the asynchronous operation performed by a machine.
    /// </summary>
    internal sealed class MachineOperation : IAsyncOperation
    {
        /// <summary>
        /// Unique id of the machine that performs this operation.
        /// </summary>
        private readonly MachineId MachineId;

        /// <summary>
        /// The task that performs this operation.
        /// </summary>
        internal Task Task;

        /// <summary>
        /// The type of the operation.
        /// </summary>
        public AsyncOperationType Type { get; private set; }

        /// <summary>
        /// Unique id of the source of this operation.
        /// </summary>
        public ulong SourceId => this.MachineId.Value;

        /// <summary>
        /// Unique name of the source of this operation.
        /// </summary>
        public string SourceName => this.MachineId.Name;

        /// <summary>
        /// True if the task that performs this operation is enabled, else false.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// The target of the operation.
        /// </summary>
        public AsyncOperationTarget Target { get; private set; }

        /// <summary>
        /// Unique id of the target of the operation.
        /// </summary>
        public ulong TargetId { get; private set; }

        /// <summary>
        /// If the next operation is <see cref="AsyncOperationType.Receive"/>, then this value
        /// gives the step index of the corresponding <see cref="AsyncOperationType.Send"/>.
        /// </summary>
        public ulong MatchingSendIndex { get; internal set; }

        /// <summary>
        /// Is the machine active.
        /// </summary>
        internal bool IsActive;

        /// <summary>
        /// Is the inbox handler running.
        /// </summary>
        internal bool IsInboxHandlerRunning;

        /// <summary>
        /// Is the machine waiting to receive an event.
        /// </summary>
        internal bool IsWaitingToReceive;

        /// <summary>
        /// True if it should skip the next receive scheduling point,
        /// because it was already called in the end of the previous
        /// event handler.
        /// </summary>
        internal bool SkipNextReceiveSchedulingPoint;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineOperation"/> class.
        /// </summary>
        internal MachineOperation(MachineId id)
        {
            this.MachineId = id;
            this.IsEnabled = false;
            this.Type = AsyncOperationType.Start;
            this.Target = AsyncOperationTarget.Task;
            this.TargetId = id.Value;
            this.IsActive = false;
            this.IsWaitingToReceive = false;
            this.IsInboxHandlerRunning = false;
            this.SkipNextReceiveSchedulingPoint = false;
        }

        /// <summary>
        /// Sets the next operation to schedule.
        /// </summary>
        internal void SetNextOperation(AsyncOperationType operationType, AsyncOperationTarget target, ulong targetId)
        {
            this.Type = operationType;
            this.Target = target;
            this.TargetId = targetId;
        }

        /// <summary>
        /// Notify that an event handler has been created and will run on the specified task.
        /// </summary>
        /// <param name="task">The task that performs this operation.</param>
        /// <param name="sendIndex">The index of the send that caused the event handler to be restarted, or 0 if this does not apply.</param>
        internal void NotifyEventHandlerCreated(Task task, int sendIndex)
        {
            this.Task = task;
            this.IsEnabled = true;
            this.IsWaitingToReceive = false;
            this.IsActive = false;
            this.IsInboxHandlerRunning = false;
            this.MatchingSendIndex = (ulong)sendIndex;
        }

        /// <summary>
        /// Notify that the event handler has completed.
        /// </summary>
        internal void NotifyEventHandlerCompleted()
        {
            this.IsEnabled = false;
            this.IsInboxHandlerRunning = false;
            this.SkipNextReceiveSchedulingPoint = true;
            this.MatchingSendIndex = 0;
        }
    }
}
