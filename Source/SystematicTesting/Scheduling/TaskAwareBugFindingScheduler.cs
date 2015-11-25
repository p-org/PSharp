//-----------------------------------------------------------------------
// <copyright file="TaskAwareBugFindingScheduler.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation. All rights reserved.
// 
//      THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, 
//      EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES 
//      OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR PURPOSE.
// ----------------------------------------------------------------------------------
//      The example companies, organizations, products, domain names,
//      e-mail addresses, logos, people, places, and events depicted
//      herein are fictitious.  No association with any real company,
//      organization, product, domain name, email address, logo, person,
//      places, or events is intended or should be inferred.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.PSharp.Threading;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.SystematicTesting.Scheduling
{
    /// <summary>
    /// Class implementing a P# bug-finding scheduler that is
    /// aware of intra-machine concurrency.
    /// </summary>
    internal sealed class TaskAwareBugFindingScheduler : BugFindingScheduler
    {
        #region fields
        
        /// <summary>
        /// List of user tasks that cannot be directly scheduled.
        /// </summary>
        private List<Task> UserTasks;

        /// <summary>
        /// Map from wrapped task ids to machine infos.
        /// </summary>
        private Dictionary<int, MachineInfo> WrappedTaskMap;

        #endregion

        #region internal methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">PSharpBugFindingRuntime</param>
        /// <param name="strategy">SchedulingStrategy</param>
        internal TaskAwareBugFindingScheduler(PSharpBugFindingRuntime runtime, ISchedulingStrategy strategy)
            : base (runtime, strategy)
        {
            this.UserTasks = new List<Task>();
            this.WrappedTaskMap = new Dictionary<int, MachineInfo>();
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

            if (this.Strategy.HasReachedDepthBound())
            {
                IO.Debug("<ScheduleDebug> Depth bound of {0} reached.",
                    this.Strategy.GetDepthBound());
                this.KillRemainingMachines();
                throw new TaskCanceledException();
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
                    IO.Debug("<ScheduleDebug> Unblocked task {0} of " +
                        "machine {1}({2}).", task.Id, task.Machine.GetType(), task.Machine.Id.MVal);
                }
            }

            MachineInfo machineInfo = null;
            if (base.TaskMap.ContainsKey((int)id))
            {
                machineInfo = base.TaskMap[(int)id];
            }
            else if (this.WrappedTaskMap.ContainsKey((int)id))
            {
                machineInfo = this.WrappedTaskMap[(int)id];
            }
            else
            {
                IO.Debug("<ScheduleDebug> Unable to schedule task {0}.", id);
                this.KillRemainingMachines();
                throw new TaskCanceledException();
            }

            var machineInfos = this.MachineInfos;
            if (this.Runtime.Configuration.BoundOperations)
            {
                machineInfos = this.Runtime.OperationScheduler.GetPrioritizedMachines(machineInfos, machineInfo);
            }

            MachineInfo next = null;
            if (!this.Strategy.TryGetNext(out next, machineInfos, machineInfo))
            {
                IO.Debug("<ScheduleDebug> Schedule explored.");
                this.KillRemainingMachines();
                throw new TaskCanceledException();
            }

            base.Runtime.ProgramTrace.AddSchedulingChoice(next.Machine);
            if (base.Runtime.Configuration.CheckLiveness &&
                base.Runtime.Configuration.CacheProgramState &&
                base.Runtime.Configuration.SafetyPrefixBound <= this.ExploredSteps)
            {
                base.Runtime.StateCache.CaptureState(base.Runtime.ProgramTrace.Peek());
            }

            IO.Debug("<ScheduleDebug> Schedule task {0} of machine {1}({2}).",
                next.Id, next.Machine.GetType(), next.Machine.Id.MVal);

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
                        IO.Debug("<ScheduleDebug> Sleep task {0} of machine {1}({2}).",
                            machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);
                        System.Threading.Monitor.Wait(machineInfo);
                        IO.Debug("<ScheduleDebug> Wake up task {0} of machine {1}({2}).",
                            machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);
                    }

                    if (!machineInfo.IsEnabled)
                    {
                        throw new TaskCanceledException();
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

            IO.Debug("<ScheduleDebug> Created task {0} for machine {1}({2}).",
                machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);

            if (base.MachineInfos.Count == 0)
            {
                machineInfo.IsActive = true;
            }

            base.MachineInfos.Add(machineInfo);
            base.TaskMap.Add(id, machineInfo);
            
            if (machine is TaskMachine)
            {
                this.WrappedTaskMap.Add((machine as TaskMachine).WrappedTask.Id, machineInfo);
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
            if (base.TaskMap.ContainsKey((int)id))
            {
                machineInfo = base.TaskMap[(int)id];
            }
            else if (this.WrappedTaskMap.ContainsKey((int)id))
            {
                machineInfo = this.WrappedTaskMap[(int)id];
            }

            IO.Debug("<ScheduleDebug> Started task {0} of machine {1}({2}).",
                machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);

            lock (machineInfo)
            {
                machineInfo.HasStarted = true;
                System.Threading.Monitor.PulseAll(machineInfo);
                while (!machineInfo.IsActive)
                {
                    IO.Debug("<ScheduleDebug> Sleep task {0} of machine {1}({2}).",
                        machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);
                    System.Threading.Monitor.Wait(machineInfo);
                    IO.Debug("<ScheduleDebug> Wake up task {0} of machine {1}({2}).",
                        machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);
                }

                if (!machineInfo.IsEnabled)
                {
                    throw new TaskCanceledException();
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

            IO.Debug("<ScheduleDebug> Blocked task {0} of machine {1}({2}).",
                machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);

            var blockingWrappedTasks = new List<MachineInfo>();
            var blockingUnwrappedTasks = new List<Task>();
            foreach (var task in blockingTasks)
            {
                if (this.WrappedTaskMap.ContainsKey(task.Id))
                {
                    blockingWrappedTasks.Add(this.WrappedTaskMap[task.Id]);
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

            IO.Debug("<ScheduleDebug> Completed task {0} of machine {1}({2}).",
                machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);

            machineInfo.IsEnabled = false;
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
                        IO.Debug("<ScheduleDebug> Unblocked task {0} of " +
                            "machine {1}({2}).", mi.Id, mi.Machine.GetType(), mi.Machine.Id.MVal);
                    }
                }
            }

            this.Schedule();

            IO.Debug("<ScheduleDebug> Exit task {0} of machine {1}({2}).",
                machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);
        }

        #endregion
    }
}
