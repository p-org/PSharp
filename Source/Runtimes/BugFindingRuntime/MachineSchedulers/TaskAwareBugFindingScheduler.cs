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
using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Scheduling
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
        /// Map from wrapped task ids to task infos.
        /// </summary>
        private Dictionary<int, TaskInfo> WrappedTaskMap;

        #endregion

        #region internal methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="strategy">SchedulingStrategy</param>
        internal TaskAwareBugFindingScheduler(ISchedulingStrategy strategy)
            : base (strategy)
        {
            this.UserTasks = new List<Task>();
            this.WrappedTaskMap = new Dictionary<int, TaskInfo>();
        }

        /// <summary>
        /// Schedules the next machine to execute.
        /// </summary>
        internal override void Schedule()
        {
            int? id = Task.CurrentId;
            if (id == null || id == PSharpRuntime.RootTaskId)
            {
                return;
            }

            if (this.Strategy.HasReachedDepthBound())
            {
                Output.Debug("<ScheduleDebug> Depth bound of {0} reached.",
                    this.Strategy.GetDepthBound());
                this.KillRemainingTasks();
                throw new TaskCanceledException();
            }

            foreach (var task in this.Tasks.Where(val => val.IsBlocked))
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
                    Output.Debug("<ScheduleDebug> Unblocked task {0} of " +
                        "machine {1}({2}).", task.Id, task.Machine.GetType(), task.Machine.Id.MVal);
                }
            }

            TaskInfo taskInfo = null;
            if (this.TaskMap.ContainsKey((int)id))
            {
                taskInfo = this.TaskMap[(int)id];
            }
            else if (this.WrappedTaskMap.ContainsKey((int)id))
            {
                taskInfo = this.WrappedTaskMap[(int)id];
            }
            else
            {
                Output.Debug("<ScheduleDebug> Unable to schedule task {0}.", id);
                this.KillRemainingTasks();
                throw new TaskCanceledException();
            }

            TaskInfo next = null;
            if (!this.Strategy.TryGetNext(out next, this.Tasks, taskInfo))
            {
                Output.Debug("<ScheduleDebug> Schedule explored.");
                this.KillRemainingTasks();
                throw new TaskCanceledException();
            }

           PSharpRuntime.ProgramTrace.AddSchedulingChoice(next.Machine);
            if (PSharpRuntime.Configuration.CheckLiveness &&
                PSharpRuntime.Configuration.CacheProgramState &&
                PSharpRuntime.Configuration.SafetyPrefixBound <= this.SchedulingPoints)
            {
                PSharpRuntime.StateCache.CaptureState(PSharpRuntime.ProgramTrace.Peek());
            }

            Output.Debug("<ScheduleDebug> Schedule task {0} of machine {1}({2}).",
                next.Id, next.Machine.GetType(), next.Machine.Id.MVal);

            if (taskInfo != next)
            {
                taskInfo.IsActive = false;
                lock (next)
                {
                    next.IsActive = true;
                    System.Threading.Monitor.PulseAll(next);
                }

                lock (taskInfo)
                {
                    if (taskInfo.IsCompleted)
                    {
                        return;
                    }
                    
                    while (!taskInfo.IsActive)
                    {
                        Output.Debug("<ScheduleDebug> Sleep task {0} of machine {1}({2}).",
                            taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);
                        System.Threading.Monitor.Wait(taskInfo);
                        Output.Debug("<ScheduleDebug> Wake up task {0} of machine {1}({2}).",
                            taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);
                    }

                    if (!taskInfo.IsEnabled)
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
        internal override void NotifyNewTaskCreated(int id, BaseMachine machine)
        {
            var taskInfo = new TaskInfo(id, machine);

            Output.Debug("<ScheduleDebug> Created task {0} for machine {1}({2}).",
                taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);

            if (this.Tasks.Count == 0)
            {
                taskInfo.IsActive = true;
            }

            this.Tasks.Add(taskInfo);
            this.TaskMap.Add(id, taskInfo);
            
            if (machine is TaskMachine)
            {
                this.WrappedTaskMap.Add((machine as TaskMachine).WrappedTask.Id, taskInfo);
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

            TaskInfo taskInfo = null;
            if (this.TaskMap.ContainsKey((int)id))
            {
                taskInfo = this.TaskMap[(int)id];
            }
            else if (this.WrappedTaskMap.ContainsKey((int)id))
            {
                taskInfo = this.WrappedTaskMap[(int)id];
            }

            Output.Debug("<ScheduleDebug> Started task {0} of machine {1}({2}).",
                taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);

            lock (taskInfo)
            {
                taskInfo.HasStarted = true;
                System.Threading.Monitor.PulseAll(taskInfo);
                while (!taskInfo.IsActive)
                {
                    Output.Debug("<ScheduleDebug> Sleep task {0} of machine {1}({2}).",
                        taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);
                    System.Threading.Monitor.Wait(taskInfo);
                    Output.Debug("<ScheduleDebug> Wake up task {0} of machine {1}({2}).",
                        taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);
                }

                if (!taskInfo.IsEnabled)
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
            
            var taskInfo = this.WrappedTaskMap[(int)id];

            Output.Debug("<ScheduleDebug> Blocked task {0} of machine {1}({2}).",
                taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);

            var blockingWrappedTasks = new List<TaskInfo>();
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

            taskInfo.IsBlocked = true;
            taskInfo.BlockingWrappedTasks = blockingWrappedTasks;
            taskInfo.BlockingUnwrappedTasks = blockingUnwrappedTasks;
            taskInfo.WaitAll = waitAll;
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
            
            var taskInfo = this.TaskMap[(int)id];

            Output.Debug("<ScheduleDebug> Completed task {0} of machine {1}({2}).",
                taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);

            taskInfo.IsEnabled = false;
            taskInfo.IsCompleted = true;

            foreach (var task in this.Tasks.Where(val => val.IsBlocked))
            {
                if (task.BlockingWrappedTasks.Contains(taskInfo))
                {
                    task.BlockingWrappedTasks.Remove(taskInfo);
                    if (!task.WaitAll)
                    {
                        task.BlockingWrappedTasks.Clear();
                        task.BlockingUnwrappedTasks.Clear();
                        task.IsBlocked = false;
                        Output.Debug("<ScheduleDebug> Unblocked task {0} of " +
                            "machine {1}({2}).", task.Id, task.Machine.GetType(), task.Machine.Id.MVal);
                    }
                }
            }

            this.Schedule();

            Output.Debug("<ScheduleDebug> Exit task {0} of machine {1}({2}).",
                taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);
        }

        #endregion
    }
}
