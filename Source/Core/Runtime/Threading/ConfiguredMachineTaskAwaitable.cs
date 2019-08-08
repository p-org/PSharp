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
    /// Provides an awaitable object that enables configured awaits on a <see cref="MachineTask"/>.
    /// </summary>
    public struct ConfiguredMachineTaskAwaitable
    {
        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly ConfiguredMachineTaskAwaiter Awaiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredMachineTaskAwaitable"/> struct.
        /// </summary>
        internal ConfiguredMachineTaskAwaitable(MachineTask task, Task awaiterTask, bool continueOnCapturedContext)
        {
            this.Awaiter = new ConfiguredMachineTaskAwaiter(task, awaiterTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Returns an awaiter for this awaitable object.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredMachineTaskAwaiter GetAwaiter() => this.Awaiter;

        /// <summary>
        /// Provides an awaiter for an awaitable object.
        /// </summary>
        public struct ConfiguredMachineTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// The machine task being awaited.
            /// </summary>
            private readonly MachineTask MachineTask;

            /// <summary>
            /// The task awaiter.
            /// </summary>
            private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether the asynchronous task has completed.
            /// </summary>
            public bool IsCompleted => this.MachineTask.IsCompleted;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfiguredMachineTaskAwaiter"/> struct.
            /// </summary>
            internal ConfiguredMachineTaskAwaiter(MachineTask task, Task awaiterTask, bool continueOnCapturedContext)
            {
                this.MachineTask = task;
                this.Awaiter = awaiterTask.ConfigureAwait(continueOnCapturedContext).GetAwaiter();
            }

            /// <summary>
            /// Ends the await on the completed task.
            /// </summary>
            public void GetResult() => this.MachineTask.GetResult(this.Awaiter);

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void OnCompleted(Action continuation) =>
                this.MachineTask.OnCompleted(continuation, this.Awaiter);

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void UnsafeOnCompleted(Action continuation) =>
                this.MachineTask.UnsafeOnCompleted(continuation, this.Awaiter);
        }
    }

    /// <summary>
    /// Provides an awaitable object that enables configured awaits on a <see cref="MachineTask{TResult}"/>.
    /// </summary>
    public struct ConfiguredMachineTaskAwaitable<TResult>
    {
        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly ConfiguredMachineTaskAwaiter Awaiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredMachineTaskAwaitable{TResult}"/> struct.
        /// </summary>
        internal ConfiguredMachineTaskAwaitable(MachineTask<TResult> task, Task<TResult> awaiterTask,
            bool continueOnCapturedContext)
        {
            this.Awaiter = new ConfiguredMachineTaskAwaiter(task, awaiterTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Returns an awaiter for this awaitable object.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredMachineTaskAwaiter GetAwaiter() => this.Awaiter;

        /// <summary>
        /// Provides an awaiter for an awaitable object.
        /// </summary>
        public struct ConfiguredMachineTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// The machine task being awaited.
            /// </summary>
            private readonly MachineTask<TResult> MachineTask;

            /// <summary>
            /// The task awaiter.
            /// </summary>
            private readonly ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether the asynchronous task has completed.
            /// </summary>
            public bool IsCompleted => this.MachineTask.IsCompleted;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfiguredMachineTaskAwaiter"/> struct.
            /// </summary>
            internal ConfiguredMachineTaskAwaiter(MachineTask<TResult> task, Task<TResult> awaiterTask,
                bool continueOnCapturedContext)
            {
                this.MachineTask = task;
                this.Awaiter = awaiterTask.ConfigureAwait(continueOnCapturedContext).GetAwaiter();
            }

            /// <summary>
            /// Ends the await on the completed task.
            /// </summary>
            public TResult GetResult() => this.MachineTask.GetResult(this.Awaiter);

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void OnCompleted(Action continuation) =>
                this.MachineTask.OnCompleted(continuation, this.Awaiter);

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void UnsafeOnCompleted(Action continuation) =>
                this.MachineTask.UnsafeOnCompleted(continuation, this.Awaiter);
        }
    }
}
