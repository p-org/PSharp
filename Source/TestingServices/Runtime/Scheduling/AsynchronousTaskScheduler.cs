﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Runtime;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// The P# asynchronous task scheduler.
    /// </summary>
    internal sealed class AsynchronousTaskScheduler : TaskScheduler
    {
        /// <summary>
        /// The P# testing runtime.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// Map from task ids to machines.
        /// </summary>
        private readonly ConcurrentDictionary<int, AsyncMachine> TaskMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsynchronousTaskScheduler"/> class.
        /// </summary>
        internal AsynchronousTaskScheduler(SystematicTestingRuntime runtime, ConcurrentDictionary<int, AsyncMachine> taskMap)
        {
            this.Runtime = runtime;
            this.TaskMap = taskMap;
        }

        /// <summary>
        /// Enqueues the given task. If the task does not correspond to a P# machine,
        /// then it wraps it in a task machine and schedules it.
        /// </summary>
        protected override void QueueTask(Task task)
        {
            if (this.TaskMap.ContainsKey(task.Id))
            {
                // If the task was already registered with the P# runtime, then
                // execute it on the thread pool.
                this.Execute(task);
            }
            else
            {
                // Else, the task was spawned by user-code (e.g. due to async/await). In
                // this case, get the currently scheduled machine (this was the machine
                // that spawned this task).
                int prevTaskId = this.Runtime.Scheduler.ScheduledOperation.Task.Id;
                this.TaskMap.TryRemove(prevTaskId, out AsyncMachine machine);
                this.TaskMap.TryAdd(task.Id, machine);

                // Change the task previously associated with the currently executing operation to the new task.
                AsyncOperation op = this.Runtime.GetAsynchronousOperation(machine.Id.Value);
                op.Task = task;

                IO.Debug.WriteLine($"<ScheduleDebug> '{machine.Id}' changed task '{prevTaskId}' to '{task.Id}'.");

                // Execute the new task.
                this.Execute(task);
            }
        }

        /// <summary>
        /// Tries to execute the task inline.
        /// </summary>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        /// <summary>
        /// Returns the wrapped in a machine scheduled tasks.
        /// </summary>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            throw new InvalidOperationException("The BugFindingTaskScheduler does not provide access to the scheduled tasks.");
        }

        /// <summary>
        /// Executes the given scheduled task on the thread pool.
        /// </summary>
        private void Execute(Task task)
        {
            ThreadPool.UnsafeQueueUserWorkItem(
                _ =>
                {
                    this.TryExecuteTask(task);
                }, null);
        }
    }
}
