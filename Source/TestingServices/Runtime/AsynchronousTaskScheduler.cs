//-----------------------------------------------------------------------
// <copyright file="AsynchronousTaskScheduler.cs">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
//      EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//      MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//      IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//      CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//      TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//      SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.PSharp.Runtime;
using Microsoft.PSharp.TestingServices.Scheduling;

namespace Microsoft.PSharp.TestingServices.Runtime
{
    /// <summary>
    /// The P# asynchronous task scheduler.
    /// </summary>
    internal sealed class AsynchronousTaskScheduler : TaskScheduler
    {
        /// <summary>
        /// The P# testing runtime.
        /// </summary>
        private BaseTestingRuntime Runtime;

        /// <summary>
        /// Map from task ids to machine data.
        /// </summary>
        private readonly ConcurrentDictionary<int, (IMachineId mid, SchedulableInfo info)> TaskMap;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">The P# testing runtime.</param>
        /// <param name="taskMap">Map from task ids to machine data.</param>
        internal AsynchronousTaskScheduler(BaseTestingRuntime runtime, ConcurrentDictionary<int, (IMachineId, SchedulableInfo)> taskMap)
        {
            this.Runtime = runtime;
            this.TaskMap = taskMap;
        }

        /// <summary>
        /// Enqueues the given task. If the task does not correspond to a P# machine,
        /// then it wraps it in a task machine and schedules it.
        /// </summary>
        /// <param name="task">Task</param>
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
                int prevTaskId = Runtime.Scheduler.ScheduledMachine.TaskId;

                this.TaskMap.TryRemove(prevTaskId, out (IMachineId id, SchedulableInfo info) machineData);
                this.TaskMap.TryAdd(task.Id, machineData);

                // Change the task previously associated with the machine to the new task.
                (machineData.info as SchedulableInfo).TaskId = task.Id;
                IO.Debug.WriteLine($"<ScheduleDebug> '{machineData.id}' changed task '{prevTaskId}' to '{task.Id}'.");

                // Execute the new task.
                this.Execute(task);
            }
        }

        /// <summary>
        /// Tries to execute the task inline.
        /// </summary>
        /// <param name="task">Task</param>
        /// <param name="taskWasPreviouslyQueued">Boolean</param>
        /// <returns>Boolean</returns>
        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        /// <summary>
        /// Returns the wrapped in a machine scheduled tasks.
        /// </summary>
        /// <returns>Scheduled tasks</returns>
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            throw new InvalidOperationException("The BugFindingTaskScheduler does not provide access to the scheduled tasks.");
        }

        /// <summary>
        /// Executes the given scheduled task on the
        /// thread pool.
        /// </summary>
        /// <param name="task">Task</param>
        private void Execute(Task task)
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                base.TryExecuteTask(task);
            }, null);
        }
    }
}
