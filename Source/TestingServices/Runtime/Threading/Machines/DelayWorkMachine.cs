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
    /// Implements a machine that can execute a delay asynchronously.
    /// </summary>
    internal sealed class DelayWorkMachine : WorkMachine
    {
        /// <summary>
        /// Provides the capability to await for work completion.
        /// </summary>
        private readonly TaskCompletionSource<object> Awaiter;

        /// <summary>
        /// Task that provides access to the completed work.
        /// </summary>
        internal Task AwaiterTask => this.Awaiter.Task;

        /// <summary>
        /// The id of the task that provides access to the completed work.
        /// </summary>
        internal override int AwaiterTaskId => this.AwaiterTask.Id;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayWorkMachine"/> class.
        /// </summary>
        internal DelayWorkMachine(SystematicTestingRuntime runtime)
            : base(runtime)
        {
            this.Awaiter = new TaskCompletionSource<object>();
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        internal override Task ExecuteAsync()
        {
            Console.WriteLine($"Machine '{this.Id}' is performing a delay on task '{MachineTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(default);
            Console.WriteLine($"Machine '{this.Id}' completed a delay on task '{MachineTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            return Task.CompletedTask;
        }
    }
}
