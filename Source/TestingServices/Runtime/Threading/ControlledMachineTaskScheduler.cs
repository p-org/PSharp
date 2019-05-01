// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.Threading;

using static System.Runtime.CompilerServices.YieldAwaitable;

namespace Microsoft.PSharp.TestingServices.Threading
{
    /// <summary>
    /// Implements a <see cref="MachineTaskScheduler"/> that is controlled by the runtime during testing.
    /// </summary>
    internal sealed class ControlledMachineTaskScheduler : MachineTaskScheduler
    {
        /// <summary>
        /// The testing runtime that is controlling this scheduler.
        /// </summary>
        internal SystematicTestingRuntime Runtime;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledMachineTaskScheduler"/> class.
        /// </summary>
        internal ControlledMachineTaskScheduler(SystematicTestingRuntime runtime)
            : base()
        {
            this.Runtime = runtime;
        }

        /// <summary>
        /// Schedules the specified work to execute asynchronously and returns a task handle for the work.
        /// </summary>
        /// <param name="action">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to be scheduled.</returns>
        internal override MachineTask RunAsync(Action action, CancellationToken cancellationToken) =>
            this.Runtime.CreateMachineTask(action, cancellationToken);

        /// <summary>
        /// Schedules the specified work to execute asynchronously and returns a task handle for the work.
        /// </summary>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to be scheduled.</returns>
        internal override MachineTask RunAsync(Func<Task> function, CancellationToken cancellationToken) =>
            this.Runtime.CreateMachineTask(function, cancellationToken);

        /// <summary>
        /// Schedules the specified work to execute asynchronously and returns a task handle for the work.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to be scheduled.</returns>
        internal override MachineTask<TResult> RunAsync<TResult>(Func<TResult> function, CancellationToken cancellationToken) =>
            this.Runtime.CreateMachineTask(function, cancellationToken);

        /// <summary>
        /// Schedules the specified work to execute asynchronously and returns a task handle for the work.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to be scheduled.</returns>
        internal override MachineTask<TResult> RunAsync<TResult>(Func<Task<TResult>> function,
            CancellationToken cancellationToken) =>
            this.Runtime.CreateMachineTask(function, cancellationToken);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that completes after a time delay.
        /// </summary>
        /// <param name="millisecondsDelay">
        /// The number of milliseconds to wait before completing the returned task, or -1 to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the delay.</param>
        /// <returns>Task that represents the time delay.</returns>
        internal override MachineTask DelayAsync(int millisecondsDelay, CancellationToken cancellationToken) =>
            this.Runtime.CreateMachineTask(millisecondsDelay, cancellationToken);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        internal override MachineTask WaitAllTasksAsync(params Task[] tasks) =>
            this.Runtime.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        internal override MachineTask WaitAllTasksAsync(IEnumerable<Task> tasks) =>
            this.Runtime.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        internal override MachineTask<TResult[]> WaitAllTasksAsync<TResult>(params Task<TResult>[] tasks) =>
            this.Runtime.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        internal override MachineTask<TResult[]> WaitAllTasksAsync<TResult>(IEnumerable<Task<TResult>> tasks) =>
            this.Runtime.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        internal override MachineTask<Task> WaitAnyTaskAsync(params MachineTask[] tasks) =>
            this.Runtime.WaitAnyTaskAsync(tasks.Select(t => t.AwaiterTask));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        internal override MachineTask<Task> WaitAnyTaskAsync(params Task[] tasks) =>
            this.Runtime.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal override MachineTask<Task> WaitAnyTaskAsync(IEnumerable<MachineTask> tasks) =>
            this.Runtime.WaitAnyTaskAsync(tasks.Select(t => t.AwaiterTask));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal override MachineTask<Task> WaitAnyTaskAsync(IEnumerable<Task> tasks) =>
            this.Runtime.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        internal override MachineTask<Task<TResult>> WaitAnyTaskAsync<TResult>(params MachineTask<TResult>[] tasks) =>
            this.Runtime.WaitAnyTaskAsync(tasks.Select(t => t.AwaiterTask));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        internal override MachineTask<Task<TResult>> WaitAnyTaskAsync<TResult>(params Task<TResult>[] tasks) =>
            this.Runtime.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal override MachineTask<Task<TResult>> WaitAnyTaskAsync<TResult>(IEnumerable<MachineTask<TResult>> tasks) =>
            this.Runtime.WaitAnyTaskAsync(tasks.Select(t => t.AwaiterTask));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        internal override MachineTask<Task<TResult>> WaitAnyTaskAsync<TResult>(IEnumerable<Task<TResult>> tasks) =>
            this.Runtime.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> associated with a completion source.
        /// </summary>
        internal override MachineTask CreateCompletionSourceMachineTask(Task task) =>
            this.Runtime.CreateCompletionMachineTask(task);

        /// <summary>
        /// Creates a <see cref="MachineTask{TResult}"/> associated with a completion source.
        /// </summary>
        internal override MachineTask<TResult> CreateCompletionSourceMachineTask<TResult>(Task<TResult> task) =>
            this.Runtime.CreateCompletionMachineTask(task);

        /// <summary>
        /// Creates a mutual exclusion lock that is compatible with <see cref="MachineTask"/> objects.
        /// </summary>
        internal override MachineLock CreateLock()
        {
            var id = (ulong)Interlocked.Increment(ref this.MachineLockIdCounter) - 1;
            return new ControlledMachineLock(this.Runtime, id);
        }

        /// <summary>
        /// Ends the wait for the completion of the yield operation.
        /// </summary>
        internal override void GetYieldResult(YieldAwaiter awaiter)
        {
            AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
            this.Runtime.Scheduler.ScheduleNextOperation(AsyncOperationType.Yield, AsyncOperationTarget.Task, caller.Id.Value);
            awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the yield operation completes.
        /// </summary>
        internal override void OnYieldCompleted(Action continuation, YieldAwaiter awaiter) =>
            this.DispatchYield(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the yield operation completes.
        /// </summary>
        internal override void UnsafeOnYieldCompleted(Action continuation, YieldAwaiter awaiter) =>
            this.DispatchYield(continuation);

        /// <summary>
        /// Dispatches the work.
        /// </summary>
        private void DispatchYield(Action continuation)
        {
            try
            {
                AsyncMachine caller = this.Runtime.GetExecutingMachine<AsyncMachine>();
                this.Runtime.Assert(caller != null,
                    "Task with id '{0}' that is not controlled by the P# runtime invoked yield operation.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");

                if (caller is Machine machine)
                {
                    this.Runtime.Assert((machine.StateManager as SerializedMachineStateManager).IsInsideMachineTaskHandler,
                        "Machine '{0}' is executing a yield operation inside a handler that does not return a 'MachineTask'.", caller.Id);
                }

                IO.Debug.WriteLine("<MachineTask> Machine '{0}' is executing a yield operation.", caller.Id);
                this.Runtime.DispatchWork(new ActionWorkMachine(this.Runtime, continuation), null);
                IO.Debug.WriteLine("<MachineTask> Machine '{0}' is executing a yield operation.", caller.Id);
            }
            catch (ExecutionCanceledException)
            {
                IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{Task.CurrentId}'.");
            }
        }
    }
}
