//-----------------------------------------------------------------------
// <copyright file="TaskAwareBugFindingScheduler.cs">
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
using System.Threading.Tasks;

using Microsoft.PSharp.TestingServices.Threading;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Class implementing a P# bug-finding scheduler that is
    /// aware of intra-machine concurrency.
    /// </summary>
    internal sealed class TaskAwareBugFindingScheduler : BugFindingScheduler
    {
        #region fields

        /// <summary>
        /// Collection of user tasks that cannot be directly scheduled.
        /// </summary>
        private ConcurrentBag<Task> UserTasks;

        /// <summary>
        /// Map from wrapped task ids to machine infos.
        /// </summary>
        private ConcurrentDictionary<int, MachineInfo> WrappedTaskMap;

        #endregion

        #region internal methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">PSharpBugFindingRuntime</param>
        /// <param name="strategy">SchedulingStrategy</param>
        internal TaskAwareBugFindingScheduler(PSharpBugFindingRuntime runtime, ISchedulingStrategy strategy)
            : base(runtime, strategy)
        {
            this.UserTasks = new ConcurrentBag<Task>();
            this.WrappedTaskMap = new ConcurrentDictionary<int, MachineInfo>();
        }

        /// <summary>
        /// Schedules the next machine to execute.
        /// </summary>
        internal override void Schedule()
        {
            int? id = Task.CurrentId;
            if (id == null || id == base.Runtime.RootTaskId)
            {
                return;
            }

            // Check if the scheduling steps bound has been reached.
            if (this.Strategy.HasReachedDepthBound())
            {
                IO.Debug("<ScheduleDebug> Scheduling steps bound of " +
                    $"{this.Strategy.GetDepthBound()} reached.");
                this.KillRemainingMachines();
                throw new OperationCanceledException();
            }

            foreach (var task in base.MachineInfos.Where(val => val.IsBlocked))
            {
                foreach (var userTask in this.UserTasks.Where(val => val.IsCompleted))
                {
                    if (task.BlockingUnwrappedTasks.Contains(userTask))
                    {
                        task.BlockingUnwrappedTasks.Remove(userTask);
                        if (!task.WaitAll)
                        {
                            task.BlockingWrappedTasks.Clear();
                            task.BlockingUnwrappedTasks.Clear();
                            task.IsBlocked = false;
                            break;
                        }
                    }
                }

                if (task.WaitAll && task.BlockingWrappedTasks.Count == 0 &&
                    task.BlockingUnwrappedTasks.Count == 0)
                {
                    task.WaitAll = false;
                    task.IsBlocked = false;
                }

                if (!task.IsBlocked)
                {
                    IO.Debug($"<ScheduleDebug> Unblocked task '{task.Id}' of machine " +
                        $"'{task.Machine.Id}'.");
                }
            }

            MachineInfo machineInfo = null;
            if (!(base.TaskMap.TryGetValue((int)id, out machineInfo) ||
                this.WrappedTaskMap.TryGetValue((int)id, out machineInfo)))
            {
                IO.Debug($"<ScheduleDebug> Unable to schedule task '{id}'.");
                this.KillRemainingMachines();
                throw new OperationCanceledException();
            }

            MachineInfo next = null;
            if (!this.Strategy.TryGetNext(out next, this.MachineInfos, machineInfo))
            {
                IO.Debug("<ScheduleDebug> Schedule explored.");
                this.KillRemainingMachines();
                throw new OperationCanceledException();
            }

            machineInfo.IsInsideTask = false;

            base.Runtime.ScheduleTrace.AddSchedulingChoice(next.Machine);
            if (base.Runtime.Configuration.CacheProgramState &&
                base.Runtime.Configuration.SafetyPrefixBound <= this.ExploredSteps)
            {
                base.Runtime.StateCache.CaptureState(base.Runtime.ScheduleTrace.Peek());
            }

            IO.Debug($"<ScheduleDebug> Schedule task '{next.Id}' of machine " +
                $"'{next.Machine.Id}'.");

            if (next.IsWaitingToReceive)
            {
                string message = IO.Format("Livelock detected. Machine " +
                    $"'{next.Machine.Id}' is waiting for an event, " +
                    "but no other machine is enabled.");
                base.Runtime.BugFinder.NotifyAssertionFailure(message, true);
            }

            if (machineInfo != next)
            {
                machineInfo.IsActive = false;
                lock (next)
                {
                    next.IsActive = true;
                    System.Threading.Monitor.PulseAll(next);
                }

                lock (machineInfo)
                {
                    if (machineInfo.IsCompleted)
                    {
                        return;
                    }

                    while (!machineInfo.IsActive)
                    {
                        IO.Debug($"<ScheduleDebug> Sleep task '{machineInfo.Id}' of machine " +
                            $"'{machineInfo.Machine.Id}'.");
                        System.Threading.Monitor.Wait(machineInfo);
                        IO.Debug($"<ScheduleDebug> Wake up task '{machineInfo.Id}' of machine " +
                            $"'{machineInfo.Machine.Id}'.");
                    }

                    if (!machineInfo.IsEnabled)
                    {
                        throw new OperationCanceledException();
                    }
                }
            }
        }

        /// <summary>
        /// Notify that a new task has been created for the given machine.
        /// </summary>
        /// <param name="id">TaskId</param>
        /// <param name="machine">Machine</param>
        internal override void NotifyNewTaskCreated(int id, AbstractMachine machine)
        {
            var machineInfo = new MachineInfo(id, machine);

            IO.Debug($"<ScheduleDebug> Created task '{machineInfo.Id}' for machine " +
                $"'{machineInfo.Machine.Id}'.");

            if (base.MachineInfos.Count == 0)
            {
                machineInfo.IsActive = true;
            }

            base.MachineInfos.Add(machineInfo);
            base.TaskMap.TryAdd(id, machineInfo);

            if (machine is TaskMachine)
            {
                this.WrappedTaskMap.TryAdd((machine as TaskMachine).WrappedTask.Id, machineInfo);
            }
        }

        /// <summary>
        /// Notify that the task has started.
        /// </summary>
        internal override void NotifyTaskStarted()
        {
            int? id = Task.CurrentId;
            if (id == null)
            {
                return;
            }

            MachineInfo machineInfo = null;
            if (!(base.TaskMap.TryGetValue((int)id, out machineInfo) ||
                this.WrappedTaskMap.TryGetValue((int)id, out machineInfo)))
            {
                IO.Debug($"<ScheduleDebug> Unable to start task '{id}'.");
                this.KillRemainingMachines();
                throw new OperationCanceledException();
            }

            IO.Debug($"<ScheduleDebug> Started task '{machineInfo.Id}' of machine " +
                $"'{machineInfo.Machine.Id}'.");

            lock (machineInfo)
            {
                machineInfo.HasStarted = true;
                System.Threading.Monitor.PulseAll(machineInfo);
                while (!machineInfo.IsActive)
                {
                    IO.Debug($"<ScheduleDebug> Sleep task '{machineInfo.Id}' of machine " +
                        $"'{machineInfo.Machine.Id}'.");
                    System.Threading.Monitor.Wait(machineInfo);
                    IO.Debug($"<ScheduleDebug> Wake up task '{machineInfo.Id}' of machine " +
                        $"'{machineInfo.Machine.Id}'.");
                }

                if (!machineInfo.IsEnabled)
                {
                    throw new OperationCanceledException();
                }
            }
        }

        /// <summary>
        /// Notify that the task has blocked.
        /// </summary>
        /// <param name="id">TaskId</param>
        /// <param name="blockingTasks"></param>
        /// <param name="waitAll"></param>
        internal void NotifyTaskBlocked(int? id, IEnumerable<Task> blockingTasks, bool waitAll)
        {
            if (id == null)
            {
                return;
            }

            var machineInfo = this.WrappedTaskMap[(int)id];

            IO.Debug($"<ScheduleDebug> Blocked task '{machineInfo.Id}' of machine " +
                $"'{machineInfo.Machine.Id}'.");

            var blockingWrappedTasks = new List<MachineInfo>();
            var blockingUnwrappedTasks = new List<Task>();
            foreach (var task in blockingTasks)
            {
                MachineInfo wrappedMachineInfo = null;
                if (this.WrappedTaskMap.TryGetValue(task.Id, out wrappedMachineInfo))
                {
                    blockingWrappedTasks.Add(wrappedMachineInfo);
                }
                else
                {
                    this.UserTasks.Add(task);
                    blockingUnwrappedTasks.Add(task);
                }
            }

            machineInfo.IsBlocked = true;
            machineInfo.BlockingWrappedTasks = blockingWrappedTasks;
            machineInfo.BlockingUnwrappedTasks = blockingUnwrappedTasks;
            machineInfo.WaitAll = waitAll;
        }

        /// <summary>
        /// Notify that the task has completed.
        /// </summary>
        internal override void NotifyTaskCompleted()
        {
            int? id = Task.CurrentId;
            if (id == null)
            {
                return;
            }

            var machineInfo = base.TaskMap[(int)id];

            IO.Debug($"<ScheduleDebug> Completed task '{machineInfo.Id}' of machine " +
                $"'{machineInfo.Machine.Id}'.");

            machineInfo.IsEnabled = false;
            machineInfo.IsInsideTask = false;
            machineInfo.IsCompleted = true;

            foreach (var mi in base.MachineInfos.Where(val => val.IsBlocked))
            {
                if (mi.BlockingWrappedTasks.Contains(machineInfo))
                {
                    mi.BlockingWrappedTasks.Remove(machineInfo);
                    if (!mi.WaitAll)
                    {
                        mi.BlockingWrappedTasks.Clear();
                        mi.BlockingUnwrappedTasks.Clear();
                        mi.IsBlocked = false;
                        IO.Debug($"<ScheduleDebug> Unblocked task '{mi.Id}' of machine " +
                            $"'{mi.Machine.Id}'.");
                    }
                }
            }

            this.Schedule();

            IO.Debug($"<ScheduleDebug> Exit task '{machineInfo.Id}' of machine " +
                $"'{machineInfo.Machine.Id}'.");
        }

        #endregion
    }
}
