// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;

namespace Microsoft.PSharp.Threading
{
    /// <summary>
    /// Provides the capability to execute work asynchronously on a <see cref="TaskScheduler"/>.
    /// During testing, a <see cref="MachineTask"/> executes in the scope of a <see cref="Machine"/>,
    /// which enables systematic exploration for finding bugs.
    /// </summary>
    [AsyncMethodBuilder(typeof(AsyncMachineTaskMethodBuilder))]
    public class MachineTask : IDisposable
    {
        /// <summary>
        /// Name of the task used for logging purposes.
        /// </summary>
        internal const string Name = "MachineTask";

        /// <summary>
        /// A <see cref="MachineTask"/> that has completed successfully.
        /// </summary>
        public static MachineTask CompletedTask { get; } = new MachineTask(Task.CompletedTask);

        /// <summary>
        /// Returns the id of the currently executing <see cref="MachineTask"/>.
        /// </summary>
        public static int? CurrentId => MachineRuntime.CurrentScheduler.CurrentTaskId;

        /// <summary>
        /// Internal task used to execute the work.
        /// </summary>
        private protected readonly Task InternalTask;

        /// <summary>
        /// The id of this task.
        /// </summary>
        public int Id => this.InternalTask.Id;

        /// <summary>
        /// Task that provides access to the completed work.
        /// </summary>
        internal Task AwaiterTask => this.InternalTask;

        /// <summary>
        /// Value that indicates whether the task has completed.
        /// </summary>
        public bool IsCompleted => this.InternalTask.IsCompleted;

        /// <summary>
        /// Value that indicates whether the task completed execution due to being canceled.
        /// </summary>
        public bool IsCanceled => this.InternalTask.IsCanceled;

        /// <summary>
        /// Value that indicates whether the task completed due to an unhandled exception.
        /// </summary>
        public bool IsFaulted => this.InternalTask.IsFaulted;

        /// <summary>
        /// Gets the <see cref="System.AggregateException"/> that caused the task
        /// to end prematurely. If the task completed successfully or has not yet
        /// thrown any exceptions, this will return null.
        /// </summary>
        public AggregateException Exception => this.InternalTask.Exception;

        /// <summary>
        /// The status of this task.
        /// </summary>
        public TaskStatus Status => this.InternalTask.Status;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineTask"/> class.
        /// </summary>
        internal MachineTask(Task task)
        {
            this.InternalTask = task ?? throw new ArgumentNullException(nameof(task));
        }

        /// <summary>
        /// Creates a <see cref="MachineTask{TResult}"/> that is completed successfully with the specified result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="result">The result to store into the completed task.</param>
        /// <returns>The successfully completed task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<TResult> FromResult<TResult>(TResult result) =>
            new MachineTask<TResult>(Task.FromResult(result));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that is completed due to
        /// cancellation with a specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token with which to complete the task.</param>
        /// <returns>The canceled task.</returns>
        public static MachineTask FromCanceled(CancellationToken cancellationToken) =>
            new MachineTask(Task.FromCanceled(cancellationToken));

        /// <summary>
        /// Creates a <see cref="MachineTask{TResult}"/> that is completed due to
        /// cancellation with a specified cancellation token.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="cancellationToken">The cancellation token with which to complete the task.</param>
        /// <returns>The canceled task.</returns>
        public static MachineTask<TResult> FromCanceled<TResult>(CancellationToken cancellationToken) =>
            new MachineTask<TResult>(Task.FromCanceled<TResult>(cancellationToken));

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that is completed with a specified exception.
        /// </summary>
        /// <param name="exception">The exception with which to complete the task.</param>
        /// <returns>The faulted task.</returns>
        public static MachineTask FromException(Exception exception) =>
            new MachineTask(Task.FromException(exception));

        /// <summary>
        /// Creates a <see cref="MachineTask{TResult}"/> that is completed with a specified exception.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="exception">The exception with which to complete the task.</param>
        /// <returns>The faulted task.</returns>
        public static MachineTask<TResult> FromException<TResult>(Exception exception) =>
            new MachineTask<TResult>(Task.FromException<TResult>(exception));

        /// <summary>
        /// Executes the specified work asynchronously on <see cref="TaskScheduler.Default"/>
        /// and returns the scheduled <see cref="MachineTask"/>.
        /// </summary>
        /// <param name="action">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask Run(Action action) => MachineRuntime.CurrentScheduler.RunAsync(action, default);

        /// <summary>
        /// Executes the specified work asynchronously on <see cref="TaskScheduler.Default"/>
        /// and returns the scheduled <see cref="MachineTask"/>.
        /// </summary>
        /// <param name="action">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask Run(Action action, CancellationToken cancellationToken) =>
            MachineRuntime.CurrentScheduler.RunAsync(action, cancellationToken);

        /// <summary>
        /// Executes the specified work asynchronously on <see cref="TaskScheduler.Default"/>
        /// and returns the scheduled <see cref="MachineTask"/>.
        /// </summary>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask Run(Func<Task> function) => MachineRuntime.CurrentScheduler.RunAsync(function, default);

        /// <summary>
        /// Executes the specified work asynchronously on <see cref="TaskScheduler.Default"/>
        /// and returns the scheduled <see cref="MachineTask"/>.
        /// </summary>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask Run(Func<Task> function, CancellationToken cancellationToken) =>
            MachineRuntime.CurrentScheduler.RunAsync(function, cancellationToken);

        /// <summary>
        /// Executes the specified work asynchronously on <see cref="TaskScheduler.Default"/>
        /// and returns the scheduled <see cref="MachineTask"/>.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<TResult> Run<TResult>(Func<TResult> function) =>
            MachineRuntime.CurrentScheduler.RunAsync(function, default);

        /// <summary>
        /// Executes the specified work asynchronously on <see cref="TaskScheduler.Default"/>
        /// and returns the scheduled <see cref="MachineTask"/>.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken) =>
            MachineRuntime.CurrentScheduler.RunAsync(function, cancellationToken);

        /// <summary>
        /// Executes the specified work asynchronously on <see cref="TaskScheduler.Default"/>
        /// and returns the scheduled <see cref="MachineTask"/>.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<TResult> Run<TResult>(Func<Task<TResult>> function) =>
            MachineRuntime.CurrentScheduler.RunAsync(function, default);

        /// <summary>
        /// Executes the specified work asynchronously on <see cref="TaskScheduler.Default"/>
        /// and returns the scheduled <see cref="MachineTask"/>.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken) =>
            MachineRuntime.CurrentScheduler.RunAsync(function, cancellationToken);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that completes after a time delay.
        /// </summary>
        /// <param name="millisecondsDelay">
        /// The number of milliseconds to wait before completing the returned task, or -1 to wait indefinitely.
        /// </param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask Delay(int millisecondsDelay) =>
            MachineRuntime.CurrentScheduler.DelayAsync(millisecondsDelay, default);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that completes after a time delay.
        /// </summary>
        /// <param name="millisecondsDelay">
        /// The number of milliseconds to wait before completing the returned task, or -1 to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the delay.</param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask Delay(int millisecondsDelay, CancellationToken cancellationToken) =>
            MachineRuntime.CurrentScheduler.DelayAsync(millisecondsDelay, cancellationToken);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that completes after a specified time interval.
        /// </summary>
        /// <param name="delay">
        /// The time span to wait before completing the returned task, or TimeSpan.FromMilliseconds(-1)
        /// to wait indefinitely.
        /// </param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask Delay(TimeSpan delay) =>
            MachineRuntime.CurrentScheduler.DelayAsync(delay, default);

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
        public static MachineTask Delay(TimeSpan delay, CancellationToken cancellationToken) =>
            MachineRuntime.CurrentScheduler.DelayAsync(delay, cancellationToken);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask WhenAll(params MachineTask[] tasks) =>
            MachineRuntime.CurrentScheduler.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask WhenAll(params Task[] tasks) =>
            MachineRuntime.CurrentScheduler.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask WhenAll(IEnumerable<MachineTask> tasks) =>
            MachineRuntime.CurrentScheduler.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask WhenAll(IEnumerable<Task> tasks) =>
            MachineRuntime.CurrentScheduler.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<TResult[]> WhenAll<TResult>(params MachineTask<TResult>[] tasks) =>
            MachineRuntime.CurrentScheduler.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks) =>
            MachineRuntime.CurrentScheduler.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<TResult[]> WhenAll<TResult>(IEnumerable<MachineTask<TResult>> tasks) =>
            MachineRuntime.CurrentScheduler.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks) =>
            MachineRuntime.CurrentScheduler.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<Task> WhenAny(params MachineTask[] tasks) =>
            MachineRuntime.CurrentScheduler.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<Task> WhenAny(params Task[] tasks) =>
            MachineRuntime.CurrentScheduler.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<Task> WhenAny(IEnumerable<MachineTask> tasks) =>
            MachineRuntime.CurrentScheduler.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<Task> WhenAny(IEnumerable<Task> tasks) =>
            MachineRuntime.CurrentScheduler.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<Task<TResult>> WhenAny<TResult>(params MachineTask<TResult>[] tasks) =>
            MachineRuntime.CurrentScheduler.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks) =>
            MachineRuntime.CurrentScheduler.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<Task<TResult>> WhenAny<TResult>(IEnumerable<MachineTask<TResult>> tasks) =>
            MachineRuntime.CurrentScheduler.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="MachineTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static MachineTask<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks) =>
            MachineRuntime.CurrentScheduler.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Waits for any of the provided <see cref="MachineTask"/> objects to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(params MachineTask[] tasks) =>
            MachineRuntime.CurrentScheduler.WaitAnyTask(tasks);

        /// <summary>
        /// Waits for any of the provided <see cref="MachineTask"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(MachineTask[] tasks, int millisecondsTimeout) =>
            MachineRuntime.CurrentScheduler.WaitAnyTask(tasks, millisecondsTimeout);

        /// <summary>
        /// Waits for any of the provided <see cref="MachineTask"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(MachineTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken) =>
            MachineRuntime.CurrentScheduler.WaitAnyTask(tasks, millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="MachineTask"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(MachineTask[] tasks, CancellationToken cancellationToken) =>
            MachineRuntime.CurrentScheduler.WaitAnyTask(tasks, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="MachineTask"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(MachineTask[] tasks, TimeSpan timeout) =>
            MachineRuntime.CurrentScheduler.WaitAnyTask(tasks, timeout);

        /// <summary>
        /// Creates an awaitable that asynchronously yields back to the current context when awaited.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static YieldAwaitable Yield() => new YieldAwaitable(MachineRuntime.CurrentScheduler);

        /// <summary>
        /// Converts the specified <see cref="MachineTask"/> into a <see cref="Task"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task ToTask() => this.InternalTask;

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual MachineTaskAwaiter GetAwaiter() => new MachineTaskAwaiter(this, this.InternalTask);

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void GetResult(TaskAwaiter awaiter) => awaiter.GetResult();

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void OnCompleted(Action continuation, TaskAwaiter awaiter) => awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void UnsafeOnCompleted(Action continuation, TaskAwaiter awaiter) =>
            awaiter.UnsafeOnCompleted(continuation);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        /// <param name="continueOnCapturedContext">
        /// True to attempt to marshal the continuation back to the original context captured; otherwise, false.
        /// </param>
        public virtual ConfiguredMachineTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) =>
            new ConfiguredMachineTaskAwaitable(this, this.InternalTask, continueOnCapturedContext);

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void GetResult(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter) => awaiter.GetResult();

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void OnCompleted(Action continuation, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter) =>
            awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void UnsafeOnCompleted(Action continuation, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter) =>
            awaiter.UnsafeOnCompleted(continuation);

        /// <summary>
        /// Disposes the <see cref="MachineTask"/>, releasing all of its unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Disposes the <see cref="MachineTask"/>, releasing all of its unmanaged resources.
        /// </summary>
        /// <remarks>
        /// Unlike most of the members of <see cref="MachineTask"/>, this method is not thread-safe.
        /// </remarks>
        public void Dispose()
        {
            this.InternalTask.Dispose();
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Provides the capability to execute work asynchronously on a <see cref="TaskScheduler"/> and produce
    /// a result at some time in the future. During testing, a <see cref="MachineTask"/> executes in the
    /// scope of a <see cref="Machine"/>, which enables systematic exploration for finding bugs.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    [AsyncMethodBuilder(typeof(AsyncMachineTaskMethodBuilder<>))]
    public class MachineTask<TResult> : MachineTask
    {
        /// <summary>
        /// Task that provides access to the completed work.
        /// </summary>
        internal new Task<TResult> AwaiterTask => this.InternalTask as Task<TResult>;

        /// <summary>
        /// Gets the result value of this task.
        /// </summary>
        public TResult Result => this.AwaiterTask.Result;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineTask{TResult}"/> class.
        /// </summary>
        internal MachineTask(Task<TResult> task)
            : base(task)
        {
        }

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new virtual MachineTaskAwaiter<TResult> GetAwaiter() =>
            new MachineTaskAwaiter<TResult>(this, this.AwaiterTask);

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual TResult GetResult(TaskAwaiter<TResult> awaiter) => awaiter.GetResult();

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void OnCompleted(Action continuation, TaskAwaiter<TResult> awaiter) =>
            awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void UnsafeOnCompleted(Action continuation, TaskAwaiter<TResult> awaiter) =>
            awaiter.UnsafeOnCompleted(continuation);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        /// <param name="continueOnCapturedContext">
        /// True to attempt to marshal the continuation back to the original context captured; otherwise, false.
        /// </param>
        public new virtual ConfiguredMachineTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) =>
            new ConfiguredMachineTaskAwaitable<TResult>(this, this.AwaiterTask, continueOnCapturedContext);

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual TResult GetResult(ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter) =>
            awaiter.GetResult();

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void OnCompleted(Action continuation, ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter) =>
            awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void UnsafeOnCompleted(Action continuation, ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter) =>
            awaiter.UnsafeOnCompleted(continuation);
    }
}
