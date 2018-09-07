// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Stores information for a schedulable machine that can be
    /// used during scheduling and testing.
    /// </summary>
    internal sealed class SchedulableInfo : MachineInfo, ISchedulable
    {
        /// <summary>
        /// Id of the task executing the event handler of the machine.
        /// </summary>
        public int TaskId { get; internal set; }

        /// <summary>
        /// Is machine enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Type of the next operation of the machine.
        /// </summary>
        public OperationType NextOperationType { get; private set; }

        /// <summary>
        /// The target type of the next operation of the machine.
        /// </summary>
        public OperationTargetType NextTargetType { get; private set; }

        /// <summary>
        /// Target id of the next operation of the machine.
        /// </summary>
        public ulong NextTargetId { get; private set; }

        /// <summary>
        /// If the next operation is <see cref="OperationType.Receive"/>
        /// then this gives the step index of the corresponding Send. 
        /// </summary>
        public ulong NextOperationMatchingSendIndex { get; internal set; }

        /// <summary>
        /// Monotonically increasing operation count.
        /// </summary>
        public ulong OperationCount { get; private set; }

        /// <summary>
        /// Unique id of the group of operations that
        /// contains the next operation.
        /// </summary>
        public Guid NextOperationGroupId => OperationGroupId;

        /// <summary>
        /// Monotonically increasing operation count for the current event handler.
        /// </summary>
        internal ulong EventHandlerOperationCount { get; private set; }

        /// <summary>
        /// Is the machine active.
        /// </summary>
        internal bool IsActive;

        /// <summary>
        /// Is the event handler running.
        /// </summary>
        internal bool IsEventHandlerRunning;

        /// <summary>
        /// True if it should skip the next receive scheduling point,
        /// because it was already called in the end of the previous
        /// event handler.
        /// </summary>
        internal bool SkipNextReceiveSchedulingPoint;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="mid">The machine id.</param>
        /// <param name="type">The machine type.</param>
        internal SchedulableInfo(IMachineId mid, Type type)
            : base(mid, type)
        {
            IsEnabled = false;
            IsActive = false;
            IsEventHandlerRunning = false;
            SkipNextReceiveSchedulingPoint = false;
            NextOperationType = OperationType.Start;
            NextTargetType = OperationTargetType.Schedulable;
            NextTargetId = mid.Value;
            OperationCount = 0;
            EventHandlerOperationCount = 0;
        }

        /// <summary>
        /// Sets the next operation to schedule.
        /// </summary>
        /// <param name="operationType">Type of the operation.</param>
        /// <param name="targetType">Type of the target of the operation.</param>
        /// <param name="targetId">Id of the target.</param>
        internal void SetNextOperation(OperationType operationType, OperationTargetType targetType, ulong targetId)
        {
            NextOperationType = operationType;
            NextTargetType = targetType;
            NextTargetId = targetId;
            OperationCount++;
            EventHandlerOperationCount++;
        }

        /// <summary>
        /// Notify that an event handler has been created and will
        /// run on the specified task id.
        /// </summary>
        /// <param name="taskId">TaskId</param>
        /// <param name="sendIndex">The index of the send that caused the event handler to be restarted, or 0 if this does not apply.</param>
        internal void NotifyEventHandlerCreated(int taskId, int sendIndex)
        {
            TaskId = taskId;
            IsEnabled = true;
            IsWaitingToReceive = false;
            IsActive = false;
            IsEventHandlerRunning = false;
            NextOperationMatchingSendIndex = (ulong)sendIndex;
            IsInsideOnExit = false;
            CurrentActionCalledTransitionStatement = false;
            ProgramCounter = 0;
            EventHandlerOperationCount = 0;
        }

        /// <summary>
        /// Notify that the event handler has completed.
        /// </summary>
        internal void NotifyEventHandlerCompleted()
        {
            IsEnabled = false;
            IsEventHandlerRunning = false;
            SkipNextReceiveSchedulingPoint = true;
            NextOperationMatchingSendIndex = 0;
        }
    }
}
