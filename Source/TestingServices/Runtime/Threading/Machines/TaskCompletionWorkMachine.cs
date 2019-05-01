// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices.Runtime;
using Microsoft.PSharp.Threading;

namespace Microsoft.PSharp.TestingServices.Threading
{
    /// <summary>
    /// Implements a machine that can complete a task asynchronously.
    /// </summary>
    internal sealed class TaskCompletionWorkMachine : WorkMachine
    {
        /// <summary>
        /// Task that provides access to the completed work.
        /// </summary>
        internal readonly Task AwaiterTask;

        /// <summary>
        /// The id of the task that provides access to the completed work.
        /// </summary>
        internal override int AwaiterTaskId => this.AwaiterTask.Id;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCompletionWorkMachine"/> class.
        /// </summary>
        internal TaskCompletionWorkMachine(SystematicTestingRuntime runtime, Task task)
            : base(runtime)
        {
            this.AwaiterTask = task;
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        internal override Task ExecuteAsync()
        {
            Console.WriteLine($"Machine '{this.Id}' completed task '{this.AwaiterTask.Id}' on task '{MachineTask.CurrentId}'");
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Implements a machine that can complete a task asynchronously.
    /// </summary>
    internal sealed class TaskCompletionWorkMachine<TResult> : WorkMachine
    {
        /// <summary>
        /// Task that provides access to the completed work.
        /// </summary>
        internal readonly Task<TResult> AwaiterTask;

        /// <summary>
        /// The id of the task that provides access to the completed work.
        /// </summary>
        internal override int AwaiterTaskId => this.AwaiterTask.Id;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCompletionWorkMachine{TResult}"/> class.
        /// </summary>
        internal TaskCompletionWorkMachine(SystematicTestingRuntime runtime, Task<TResult> task)
            : base(runtime)
        {
            this.AwaiterTask = task;
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        internal override Task ExecuteAsync()
        {
            Console.WriteLine($"\n\nMachine '{this.Id}' completed task '{this.AwaiterTask.Id}' on task '{MachineTask.CurrentId}'\n\n");
            return Task.CompletedTask;
        }
    }
}
