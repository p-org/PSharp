// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.Threading;

namespace Microsoft.PSharp.TestingServices.Threading
{
    /// <summary>
    /// A <see cref="MachineTask"/> that is controlled by the runtime scheduler.
    /// </summary>
    internal sealed class ControlledMachineTask : MachineTask
    {
        /// <summary>
        /// The testing runtime executing this task.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// The type of the task.
        /// </summary>
        private readonly MachineTaskType Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledMachineTask"/> class.
        /// </summary>
        internal ControlledMachineTask(SystematicTestingRuntime runtime, Task task, MachineTaskType taskType)
            : base(task)
        {
            IO.Debug.WriteLine("<MachineTask> Creating task '{0}' from task '{1}' (option: {2}).",
                task.Id, Task.CurrentId, taskType);
            this.Runtime = runtime;
            this.Type = taskType;
        }

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        public override MachineTaskAwaiter GetAwaiter()
        {
            IO.Debug.WriteLine("<MachineTask> Awaiting task '{0}' from task '{1}'.", this.AwaiterTask.Id, Task.CurrentId);
            return new MachineTaskAwaiter(this, this.AwaiterTask);
        }

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        internal override void GetResult(TaskAwaiter awaiter)
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            IO.Debug.WriteLine("<MachineTask> Machine '{0}' is waiting task '{1}' to complete from task '{2}'.",
                caller.Id, this.Id, Task.CurrentId);
            MachineOperation callerOp = this.Runtime.GetAsynchronousOperation(caller.Id.Value);
            callerOp.OnWaitTask(this.AwaiterTask);
            this.Runtime.Scheduler.ScheduleNextOperation(AsyncOperationType.Join, AsyncOperationTarget.Task, caller.Id.Value);
            awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        internal override void OnCompleted(Action continuation, TaskAwaiter awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        internal override void UnsafeOnCompleted(Action continuation, TaskAwaiter awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        /// <param name="continueOnCapturedContext">
        /// True to attempt to marshal the continuation back to the original context captured; otherwise, false.
        /// </param>
        public override ConfiguredMachineTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
        {
            IO.Debug.WriteLine("<MachineTask> Awaiting task '{0}' from task '{1}'.", this.AwaiterTask.Id, Task.CurrentId);
            return new ConfiguredMachineTaskAwaitable(this, this.AwaiterTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        internal override void GetResult(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter)
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            IO.Debug.WriteLine("<MachineTask> Machine '{0}' is waiting task '{1}' to complete from task '{2}'.",
                caller.Id, this.Id, Task.CurrentId);
            MachineOperation callerOp = this.Runtime.GetAsynchronousOperation(caller.Id.Value);
            callerOp.OnWaitTask(this.AwaiterTask);
            this.Runtime.Scheduler.ScheduleNextOperation(AsyncOperationType.Join, AsyncOperationTarget.Task, caller.Id.Value);
            awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        internal override void OnCompleted(Action continuation, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        internal override void UnsafeOnCompleted(Action continuation, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Dispatches the work.
        /// </summary>
        private void DispatchWork(Action continuation)
        {
            try
            {
                AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
                this.Runtime.Assert(caller != null,
                    "Task with id '{0}' that is not controlled by the P# runtime is executing machine task '{1}'.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>", this.Id);

                if (caller is Machine machine)
                {
                    this.Runtime.Assert((machine.StateManager as SerializedMachineStateManager).IsInsideMachineTaskHandler,
                        "Machine '{0}' is executing machine task '{1}' inside a handler that does not return a 'MachineTask'.",
                        caller.Id, this.Id);
                }

                if (this.Type is MachineTaskType.CompletionSourceTask)
                {
                    IO.Debug.WriteLine("<MachineTask> Machine '{0}' is executing continuation of task '{1}' on task '{2}'.",
                        caller.Id, this.Id, Task.CurrentId);
                    continuation();
                    IO.Debug.WriteLine("<MachineTask> Machine '{0}' resumed after continuation of task '{1}' on task '{2}'.",
                        caller.Id, this.Id, Task.CurrentId);
                }
                else if (this.Type is MachineTaskType.ExplicitTask)
                {
                    IO.Debug.WriteLine("<MachineTask> Machine '{0}' is dispatching continuation of task '{1}'.", caller.Id, this.Id);
                    this.Runtime.DispatchWork(new ActionWorkMachine(this.Runtime, continuation), this.AwaiterTask);
                    IO.Debug.WriteLine("<MachineTask> Machine '{0}' dispatched continuation of task '{1}'.", caller.Id, this.Id);
                }
            }
            catch (ExecutionCanceledException)
            {
                IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{Task.CurrentId}'.");
            }
        }
    }

    /// <summary>
    /// A <see cref="MachineTask{TResult}"/> that is controlled by the runtime scheduler.
    /// </summary>
    internal sealed class ControlledMachineTask<TResult> : MachineTask<TResult>
    {
        /// <summary>
        /// The testing runtime executing this task.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// The type of the task.
        /// </summary>
        private readonly MachineTaskType Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledMachineTask{TResult}"/> class.
        /// </summary>
        internal ControlledMachineTask(SystematicTestingRuntime runtime, Task<TResult> task, MachineTaskType taskType)
            : base(task)
        {
            IO.Debug.WriteLine("<MachineTask> Creating task '{0}' with result type '{1}' from task '{2}' (option: {3}).",
                task.Id, typeof(TResult), Task.CurrentId, taskType);
            this.Runtime = runtime;
            this.Type = taskType;
        }

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        public override MachineTaskAwaiter<TResult> GetAwaiter()
        {
            IO.Debug.WriteLine("<MachineTask> Awaiting task '{0}' with result type '{1}' and type '{2}' from task '{3}'.",
                this.AwaiterTask.Id, typeof(TResult), this.Type, Task.CurrentId);
            return new MachineTaskAwaiter<TResult>(this, this.AwaiterTask);
        }

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        internal override TResult GetResult(TaskAwaiter<TResult> awaiter)
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            IO.Debug.WriteLine("<MachineTask> Machine '{0}' is waiting task '{1}' with result type '{2}' to complete from task '{3}'.",
                caller.Id, this.Id, typeof(TResult), Task.CurrentId);
            MachineOperation callerOp = this.Runtime.GetAsynchronousOperation(caller.Id.Value);
            callerOp.OnWaitTask(this.AwaiterTask);
            this.Runtime.Scheduler.ScheduleNextOperation(AsyncOperationType.Join, AsyncOperationTarget.Task, caller.Id.Value);
            return awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        internal override void OnCompleted(Action continuation, TaskAwaiter<TResult> awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        internal override void UnsafeOnCompleted(Action continuation, TaskAwaiter<TResult> awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        /// <param name="continueOnCapturedContext">
        /// True to attempt to marshal the continuation back to the original context captured; otherwise, false.
        /// </param>
        public override ConfiguredMachineTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext)
        {
            IO.Debug.WriteLine("<MachineTask> Awaiting task '{0}' with result type '{1}' and type '{2}' from task '{3}'.",
                this.AwaiterTask.Id, typeof(TResult), this.Type, Task.CurrentId);
            return new ConfiguredMachineTaskAwaitable<TResult>(this, this.AwaiterTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override TResult GetResult(ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter)
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            IO.Debug.WriteLine("<MachineTask> Machine '{0}' is waiting task '{1}' with result type '{2}' to complete from task '{3}'.",
                caller.Id, this.Id, typeof(TResult), Task.CurrentId);
            MachineOperation callerOp = this.Runtime.GetAsynchronousOperation(caller.Id.Value);
            callerOp.OnWaitTask(this.AwaiterTask);
            this.Runtime.Scheduler.ScheduleNextOperation(AsyncOperationType.Join, AsyncOperationTarget.Task, caller.Id.Value);
            return awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void OnCompleted(Action continuation, ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal override void UnsafeOnCompleted(Action continuation, ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter) =>
            this.DispatchWork(continuation);

        /// <summary>
        /// Dispatches the work.
        /// </summary>
        private void DispatchWork(Action continuation)
        {
            try
            {
                AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
                this.Runtime.Assert(caller != null,
                    "Task with id '{0}' that is not controlled by the P# runtime is executing machine task '{1}'.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>", this.Id);

                if (caller is Machine machine)
                {
                    this.Runtime.Assert((machine.StateManager as SerializedMachineStateManager).IsInsideMachineTaskHandler,
                        "Machine '{0}' is executing machine task '{1}' inside a handler that does not return a 'MachineTask'.",
                        caller.Id, this.Id);
                }

                if (this.Type is MachineTaskType.CompletionSourceTask)
                {
                    IO.Debug.WriteLine("<MachineTask> Machine '{0}' is executing continuation of task '{1}' with result type '{2}' on task '{3}'.",
                        caller.Id, this.Id, typeof(TResult), Task.CurrentId);
                    continuation();
                    IO.Debug.WriteLine("<MachineTask> Machine '{0}' resumed after continuation of task '{1}' with result type '{2}' on task '{3}'.",
                        caller.Id, this.Id, typeof(TResult), Task.CurrentId);
                }
                else if (this.Type is MachineTaskType.ExplicitTask)
                {
                    IO.Debug.WriteLine("<MachineTask> Machine '{0}' is dispatching continuation of task '{1}' with result type '{2}'.",
                        caller.Id, this.Id, typeof(TResult));
                    this.Runtime.DispatchWork(new ActionWorkMachine(this.Runtime, continuation), this.AwaiterTask);
                    IO.Debug.WriteLine("<MachineTask> Machine '{0}' dispatched continuation of task '{1}' with result type '{2}'.",
                        caller.Id, this.Id, typeof(TResult));
                }
            }
            catch (ExecutionCanceledException)
            {
                IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{Task.CurrentId}'.");
            }
        }
    }
}
