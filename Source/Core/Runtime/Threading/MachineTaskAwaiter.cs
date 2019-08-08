// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.PSharp.Threading
{
    /// <summary>
    /// Implements a <see cref="MachineTask"/> awaiter.
    /// </summary>
    public readonly struct MachineTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic MachineTaskAwaiter<> as MachineTaskAwaiter.

        /// <summary>
        /// The machine task being awaited.
        /// </summary>
        private readonly MachineTask MachineTask;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly TaskAwaiter Awaiter;

        /// <summary>
        /// Gets a value that indicates whether the asynchronous task has completed.
        /// </summary>
        public bool IsCompleted => this.MachineTask.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineTaskAwaiter"/> struct.
        /// </summary>
        internal MachineTaskAwaiter(MachineTask task, Task awaiterTask)
        {
            this.MachineTask = task;
            this.Awaiter = awaiterTask.GetAwaiter();
        }

        /// <summary>
        /// Ends the wait for the completion of the asynchronous task.
        /// </summary>
        public void GetResult() => this.MachineTask.GetResult(this.Awaiter);

        /// <summary>
        /// Sets the action to perform when the asynchronous task completes.
        /// </summary>
        public void OnCompleted(Action continuation) =>
            this.MachineTask.OnCompleted(continuation, this.Awaiter);

        /// <summary>
        /// Schedules the continuation action that is invoked when the asynchronous task completes.
        /// </summary>
        public void UnsafeOnCompleted(Action continuation) =>
            this.MachineTask.UnsafeOnCompleted(continuation, this.Awaiter);
    }

    /// <summary>
    /// Implements a <see cref="MachineTask"/> awaiter.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    public readonly struct MachineTaskAwaiter<TResult> : ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic MachineTaskAwaiter<> as MachineTaskAwaiter.

        /// <summary>
        /// The machine task being awaited.
        /// </summary>
        private readonly MachineTask<TResult> MachineTask;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly TaskAwaiter<TResult> Awaiter;

        /// <summary>
        /// Gets a value that indicates whether the asynchronous task has completed.
        /// </summary>
        public bool IsCompleted => this.MachineTask.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineTaskAwaiter{TResult}"/> struct.
        /// </summary>
        internal MachineTaskAwaiter(MachineTask<TResult> task, Task<TResult> awaiterTask)
        {
            this.MachineTask = task;
            this.Awaiter = awaiterTask.GetAwaiter();
        }

        /// <summary>
        /// Ends the wait for the completion of the asynchronous task.
        /// </summary>
        public TResult GetResult() => this.MachineTask.GetResult(this.Awaiter);

        /// <summary>
        /// Sets the action to perform when the asynchronous task completes.
        /// </summary>
        public void OnCompleted(Action continuation) =>
            this.MachineTask.OnCompleted(continuation, this.Awaiter);

        /// <summary>
        /// Schedules the continuation action that is invoked when the asynchronous task completes.
        /// </summary>
        public void UnsafeOnCompleted(Action continuation) =>
            this.MachineTask.UnsafeOnCompleted(continuation, this.Awaiter);
    }
}
