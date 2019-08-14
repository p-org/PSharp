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
    /// Implements a machine that can execute a test entry point asynchronously.
    /// </summary>
    internal sealed class TestEntryPointWorkMachine : WorkMachine
    {
        /// <summary>
        /// Test to be executed asynchronously.
        /// </summary>
        private readonly Delegate Test;

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
        /// Initializes a new instance of the <see cref="TestEntryPointWorkMachine"/> class.
        /// </summary>
        internal TestEntryPointWorkMachine(SystematicTestingRuntime runtime, Delegate test)
            : base(runtime)
        {
            this.Test = test;
            this.Awaiter = new TaskCompletionSource<object>();
        }

        /// <summary>
        /// Executes the work asynchronously.
        /// </summary>
        internal override async Task ExecuteAsync()
        {
            IO.Debug.WriteLine($"Machine '{this.Id}' is executing test on task '{MachineTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");

            if (this.Test is Action<IMachineRuntime> actionWithRuntime)
            {
                actionWithRuntime(this.Runtime);
            }
            else if (this.Test is Action action)
            {
                action();
            }
            else if (this.Test is Func<IMachineRuntime, MachineTask> functionWithRuntime)
            {
                await functionWithRuntime(this.Runtime);
            }
            else if (this.Test is Func<MachineTask> function)
            {
                await function();
            }
            else
            {
                throw new InvalidOperationException($"Unsupported test delegate of type '{this.Test?.GetType()}'.");
            }

            IO.Debug.WriteLine($"Machine '{this.Id}' executed test on task '{MachineTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
            this.Awaiter.SetResult(default);
            IO.Debug.WriteLine($"Machine '{this.Id}' completed test on task '{MachineTask.CurrentId}' (tcs: {this.Awaiter.Task.Id})");
        }
    }
}
