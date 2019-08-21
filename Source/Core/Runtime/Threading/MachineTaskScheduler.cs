// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using static System.Runtime.CompilerServices.YieldAwaitable;

namespace Microsoft.PSharp.Threading
{
    /// <summary>
    /// Schedules the execution of machine tasks on a <see cref="TaskScheduler"/>.
    /// </summary>
    internal class MachineTaskScheduler
    {
        /// <summary>
        /// The default <see cref="MachineTask"/> scheduler. It schedules the execution
        /// of tasks on <see cref="TaskScheduler.Default"/>.
        /// </summary>
        internal static MachineTaskScheduler Default { get; } = new MachineTaskScheduler();

        /// <summary>
        /// Map from task ids to <see cref="MachineTask"/> objects.
        /// </summary>
        protected readonly ConcurrentDictionary<int, MachineTask> TaskMap;

        /// <summary>
        /// Returns the id of the currently executing <see cref="MachineTask"/>.
        /// </summary>
        internal virtual int? CurrentTaskId => Task.CurrentId;

        /// <summary>
        /// Monotonically increasing machine lock id counter.
        /// </summary>
        internal long MachineLockIdCounter;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineTaskScheduler"/> class.
        /// </summary>
        internal MachineTaskScheduler()
        {
            this.TaskMap = new ConcurrentDictionary<int, MachineTask>();
            this.MachineLockIdCounter = 0;
        }

        /// <summary>
        /// Schedules the specified work to execute asynchronously and returns a task handle for the work.
        /// </summary>
        /// <param name="action">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to be scheduled.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask RunAsync(Action action, CancellationToken cancellationToken) =>
            new MachineTask(Task.Run(action, cancellationToken));

        /// <summary>
        /// Schedules the specified work to execute asynchronously and returns a task handle for the work.
        /// </summary>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to be scheduled.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask RunAsync(Func<Task> function, CancellationToken cancellationToken) =>
            new MachineTask(Task.Run(function, cancellationToken));

        /// <summary>
        /// Schedules the specified work to execute asynchronously and returns a task handle for the work.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to be scheduled.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<TResult> RunAsync<TResult>(Func<TResult> function, CancellationToken cancellationToken)
        {
            if (function is Func<MachineTask> taskFunc)
            {
                var unwrappedTask = Task.Run(async () =>
                {
                    var task = taskFunc();
                    await task;
                    if (task is TResult result)
                    {
                        return result;
                    }

                    return default;
                });

                return new MachineTask<TResult>(unwrappedTask);
            }
            else
            {
                return new MachineTask<TResult>(Task.Run(function, cancellationToken));
            }
        }

        /// <summary>
        /// Schedules the specified work to execute asynchronously and returns a task handle for the work.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to be scheduled.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<TResult> RunAsync<TResult>(Func<Task<TResult>> function,
            CancellationToken cancellationToken) =>
            new MachineTask<TResult>(Task.Run(function, cancellationToken));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that completes after a time delay.
        /// </summary>
        /// <param name="millisecondsDelay">
        /// The number of milliseconds to wait before completing the returned task, or -1 to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the delay.</param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask DelayAsync(int millisecondsDelay, CancellationToken cancellationToken) =>
            new MachineTask(Task.Delay(millisecondsDelay, cancellationToken));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that completes after a specified time interval.
        /// </summary>
        /// <param name="delay">
        /// The time span to wait before completing the returned task, or TimeSpan.FromMilliseconds(-1)
        /// to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the delay.</param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
            new MachineTask(Task.Delay(delay, cancellationToken));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask WaitAllTasksAsync(params MachineTask[] tasks) =>
            new MachineTask(Task.WhenAll(tasks.Select(t => t.AwaiterTask)));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask WaitAllTasksAsync(params Task[] tasks) =>
            new MachineTask(Task.WhenAll(tasks));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask WaitAllTasksAsync(IEnumerable<MachineTask> tasks) =>
            new MachineTask(Task.WhenAll(tasks.Select(t => t.AwaiterTask)));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask WaitAllTasksAsync(IEnumerable<Task> tasks) =>
            new MachineTask(Task.WhenAll(tasks));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<TResult[]> WaitAllTasksAsync<TResult>(params MachineTask<TResult>[] tasks) =>
            new MachineTask<TResult[]>(Task.WhenAll(tasks.Select(t => t.AwaiterTask)));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<TResult[]> WaitAllTasksAsync<TResult>(params Task<TResult>[] tasks) =>
            new MachineTask<TResult[]>(Task.WhenAll(tasks));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<TResult[]> WaitAllTasksAsync<TResult>(IEnumerable<MachineTask<TResult>> tasks) =>
            new MachineTask<TResult[]>(Task.WhenAll(tasks.Select(t => t.AwaiterTask)));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<TResult[]> WaitAllTasksAsync<TResult>(IEnumerable<Task<TResult>> tasks) =>
            new MachineTask<TResult[]>(Task.WhenAll(tasks));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<Task> WaitAnyTaskAsync(params MachineTask[] tasks) =>
            new MachineTask<Task>(Task.WhenAny(tasks.Select(t => t.AwaiterTask)));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<Task> WaitAnyTaskAsync(params Task[] tasks) =>
            new MachineTask<Task>(Task.WhenAny(tasks));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<Task> WaitAnyTaskAsync(IEnumerable<MachineTask> tasks) =>
            new MachineTask<Task>(Task.WhenAny(tasks.Select(t => t.AwaiterTask)));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<Task> WaitAnyTaskAsync(IEnumerable<Task> tasks) =>
            new MachineTask<Task>(Task.WhenAny(tasks));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<Task<TResult>> WaitAnyTaskAsync<TResult>(params MachineTask<TResult>[] tasks) =>
            new MachineTask<Task<TResult>>(Task.WhenAny(tasks.Select(t => t.AwaiterTask)));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<Task<TResult>> WaitAnyTaskAsync<TResult>(params Task<TResult>[] tasks) =>
            new MachineTask<Task<TResult>>(Task.WhenAny(tasks));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<Task<TResult>> WaitAnyTaskAsync<TResult>(IEnumerable<MachineTask<TResult>> tasks) =>
            new MachineTask<Task<TResult>>(Task.WhenAny(tasks.Select(t => t.AwaiterTask)));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<Task<TResult>> WaitAnyTaskAsync<TResult>(IEnumerable<Task<TResult>> tasks) =>
            new MachineTask<Task<TResult>>(Task.WhenAny(tasks));

        /// <summary>
        /// Waits for any of the provided <see cref="MachineTask"/> objects to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int WaitAnyTask(params MachineTask[] tasks) =>
            Task.WaitAny(tasks.Select(t => t.AwaiterTask).ToArray());

        /// <summary>
        /// Waits for any of the provided <see cref="MachineTask"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int WaitAnyTask(MachineTask[] tasks, int millisecondsTimeout) =>
            Task.WaitAny(tasks.Select(t => t.AwaiterTask).ToArray(), millisecondsTimeout);

        /// <summary>
        /// Waits for any of the provided <see cref="MachineTask"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int WaitAnyTask(MachineTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken) =>
            Task.WaitAny(tasks.Select(t => t.AwaiterTask).ToArray(), millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="MachineTask"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int WaitAnyTask(MachineTask[] tasks, CancellationToken cancellationToken) =>
            Task.WaitAny(tasks.Select(t => t.AwaiterTask).ToArray(), cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="MachineTask"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int WaitAnyTask(MachineTask[] tasks, TimeSpan timeout) =>
            Task.WaitAny(tasks.Select(t => t.AwaiterTask).ToArray(), timeout);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> associated with a completion source.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask CreateCompletionSourceMachineTask(Task task) => new MachineTask(task);

        /// <summary>
        /// Creates a <see cref="MachineTask{TResult}"/> associated with a completion source.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineTask<TResult> CreateCompletionSourceMachineTask<TResult>(Task<TResult> task) =>
            new MachineTask<TResult>(task);

        /// <summary>
        /// Creates a mutual exclusion lock that is compatible with <see cref="MachineTask"/> objects.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual MachineLock CreateLock()
        {
            var id = (ulong)Interlocked.Increment(ref this.MachineLockIdCounter) - 1;
            return new MachineLock(id);
        }

        /// <summary>
        /// Creates an awaiter that asynchronously yields back to the current context when awaited.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal YieldAwaitable.YieldAwaiter CreateYieldAwaiter() => new YieldAwaitable.YieldAwaiter(this);

        /// <summary>
        /// Ends the wait for the completion of the yield operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void GetYieldResult(YieldAwaiter awaiter) => awaiter.GetResult();

        /// <summary>
        /// Sets the action to perform when the yield operation completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void OnYieldCompleted(Action continuation, YieldAwaiter awaiter) => awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the yield operation completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void UnsafeOnYieldCompleted(Action continuation, YieldAwaiter awaiter) =>
            awaiter.UnsafeOnCompleted(continuation);
    }
}
