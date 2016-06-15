//-----------------------------------------------------------------------
// <copyright file="TaskWrapperScheduler.cs">
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.PSharp.TestingServices.Threading
{
    /// <summary>
    /// Class implementing the P# task wrapper scheduler.
    /// </summary>
    internal sealed class TaskWrapperScheduler : TaskScheduler
    {
        #region fields

        /// <summary>
        /// The P# runtime.
        /// </summary>
        private PSharpRuntime Runtime;

        /// <summary>
        /// The machine tasks.
        /// </summary>
        private ConcurrentBag<Task> MachineTasks;

        /// <summary>
        /// The wrapped in a machine user tasks.
        /// </summary>
        private ConcurrentBag<Task> WrappedTasks;

        #endregion

        #region internal methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">PSharpRuntime</param>
        /// <param name="machineTasks">Machine tasks</param>
        internal TaskWrapperScheduler(PSharpRuntime runtime, ConcurrentBag<Task> machineTasks)
        {
            this.Runtime = runtime;
            this.MachineTasks = machineTasks;
            this.WrappedTasks = new ConcurrentBag<Task>();
        }

        /// <summary>
        /// Executes the given scheduled task.
        /// </summary>
        /// <param name="task">Task</param>
        internal void Execute(Task task)
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                base.TryExecuteTask(task);
            }, null);
        }

        #endregion

        #region override methods

        /// <summary>
        /// Enqueues the given task. If the task does not correspond to a P# machine,
        /// then it wraps it in a task machine and schedules it.
        /// </summary>
        /// <param name="task">Task</param>
        protected override void QueueTask(Task task)
        {
            if (this.MachineTasks.Contains(task))
            {
                this.Execute(task);
            }
            else
            {
                this.Runtime.Log("<ScheduleDebug> Wrapping task {0} in a machine.", task.Id);
                this.WrappedTasks.Add(task);
                this.Runtime.TryCreateTaskMachine(task);
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
            return this.WrappedTasks;
        }

        #endregion
    }
}
