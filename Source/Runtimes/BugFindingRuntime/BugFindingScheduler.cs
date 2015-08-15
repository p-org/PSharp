//-----------------------------------------------------------------------
// <copyright file="BugFindingScheduler.cs" company="Microsoft">
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
    /// Class implementing the P# bug-finding scheduler.
    /// </summary>
    internal sealed class BugFindingScheduler
    {
        #region fields

        /// <summary>
        /// The scheduling strategy to be used for bug-finding.
        /// </summary>
        private ISchedulingStrategy Strategy;

        /// <summary>
        /// List of tasks to schedule.
        /// </summary>
        private List<TaskInfo> Tasks;

        /// <summary>
        /// List of user tasks that cannot be directly scheduled.
        /// </summary>
        private List<Task> UserTasks;

        /// <summary>
        /// Map from task ids to task infos.
        /// </summary>
        private Dictionary<int, TaskInfo> TaskMap;

        /// <summary>
        /// Map from wrapped task ids to task infos.
        /// </summary>
        private Dictionary<int, TaskInfo> WrappedTaskMap;

        /// <summary>
        /// True if a bug was found.
        /// </summary>
        internal bool BugFound
        {
            get; private set;
        }

        /// <summary>
        /// Number of scheduling points.
        /// </summary>
        internal int SchedulingPoints
        {
            get { return this.Strategy.GetSchedulingSteps(); }
        }

        /// <summary>
        /// Bug report.
        /// </summary>
        internal string BugReport
        {
            get; private set;
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="strategy">SchedulingStrategy</param>
        internal BugFindingScheduler(ISchedulingStrategy strategy)
        {
            this.Strategy = strategy;
            this.Tasks = new List<TaskInfo>();
            this.UserTasks = new List<Task>();
            this.TaskMap = new Dictionary<int, TaskInfo>();
            this.WrappedTaskMap = new Dictionary<int, TaskInfo>();
            this.BugFound = false;
        }

        /// <summary>
        /// Schedules the next machine to execute.
        /// </summary>
        /// <param name="id">TaskId</param>
        internal void Schedule(int? id)
        {
            if (id == null || id == PSharpRuntime.RootTaskId)
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
            else
            {
                Output.Debug(DebugType.Testing, "<ScheduleDebug> Unable to" +
                    " schedule task {0}.", id);
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
                    Output.Debug(DebugType.Testing, "<ScheduleDebug> Unblocked task {0} of " +
                        "machine {1}({2}).", task.Id, task.Machine.GetType(), task.Machine.Id.MVal);
                }
            }

            TaskInfo next = null;
            if (this.Strategy.HasReachedDepthBound())
            {
                Output.Debug(DebugType.Testing, "<ScheduleDebug> Depth bound of {0} reached.",
                    this.Strategy.GetDepthBound());
                this.KillRemainingTasks();
                throw new TaskCanceledException();
            }
            else if (!this.Strategy.TryGetNext(out next, this.Tasks, taskInfo))
            {
                Output.Debug(DebugType.Testing, "<ScheduleDebug> Schedule explored.");
                this.KillRemainingTasks();
                throw new TaskCanceledException();
            }

           PSharpRuntime.ProgramTrace.AddSchedulingChoice(next.Machine);
            if (Configuration.CheckLiveness && Configuration.CacheProgramState &&
                Configuration.SafetyPrefixBound <= this.SchedulingPoints)
            {
                PSharpRuntime.StateCache.CaptureState(PSharpRuntime.ProgramTrace.Peek());
            }

            Output.Debug(DebugType.Testing, "<ScheduleDebug> Schedule task {0} of machine {1}({2}).",
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
                        Output.Debug(DebugType.Testing, "<ScheduleDebug> Sleep task {0} of machine {1}({2}).",
                            taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);
                        System.Threading.Monitor.Wait(taskInfo);
                        Output.Debug(DebugType.Testing, "<ScheduleDebug> Wake up task {0} of machine {1}({2}).",
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
        /// Returns the next nondeterministic choice.
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean value</returns>
        internal bool GetNextNondeterministicChoice(string uniqueId = null)
        {
            var choice = false;
            if (!this.Strategy.GetNextChoice(out choice))
            {
                Output.Debug(DebugType.Testing, "<ScheduleDebug> Schedule explored.");
                this.KillRemainingTasks();
                throw new TaskCanceledException();
            }

            if (uniqueId == null)
            {
                PSharpRuntime.ProgramTrace.AddNondeterministicChoice(choice);
            }
            else
            {
                PSharpRuntime.ProgramTrace.AddFairNondeterministicChoice(uniqueId, choice);
            }
            
            if (Configuration.CheckLiveness && Configuration.CacheProgramState &&
                Configuration.SafetyPrefixBound <= this.SchedulingPoints)
            {
                PSharpRuntime.StateCache.CaptureState(PSharpRuntime.ProgramTrace.Peek());
            }

            return choice;
        }

        /// <summary>
        /// Returns the enabled machines.
        /// </summary>
        /// <returns>Enabled machines</returns>
        internal HashSet<BaseMachine> GetEnabledMachines()
        {
            var enabledMachines = new HashSet<BaseMachine>();
            foreach (var taskInfo in this.Tasks)
            {
                if (taskInfo.IsEnabled)
                {
                    enabledMachines.Add(taskInfo.Machine);
                }
            }

            return enabledMachines;
        }

        /// <summary>
        /// Notify that a new task has been created for the given machine.
        /// </summary>
        /// <param name="id">TaskId</param>
        /// <param name="machine">Machine</param>
        internal void NotifyNewTaskCreated(int id, BaseMachine machine)
        {
            var taskInfo = new TaskInfo(id, machine);

            Output.Debug(DebugType.Testing, "<ScheduleDebug> Created task {0} for machine {1}({2}).",
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
        /// <param name="id">TaskId</param>
        internal void NotifyTaskStarted(int? id)
        {
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

            Output.Debug(DebugType.Testing, "<ScheduleDebug> Started task {0} of machine {1}({2}).",
                taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);

            lock (taskInfo)
            {
                taskInfo.HasStarted = true;
                System.Threading.Monitor.PulseAll(taskInfo);
                while (!taskInfo.IsActive)
                {
                    Output.Debug(DebugType.Testing, "<ScheduleDebug> Sleep task {0} of machine {1}({2}).",
                        taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);
                    System.Threading.Monitor.Wait(taskInfo);
                    Output.Debug(DebugType.Testing, "<ScheduleDebug> Wake up task {0} of machine {1}({2}).",
                        taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);
                }

                if (!taskInfo.IsEnabled)
                {
                    throw new TaskCanceledException();
                }
            }
        }

        /// <summary>
        /// Notify that the task is waiting to receive an event.
        /// </summary>
        /// <param name="id">TaskId</param>
        internal void NotifyTaskBlockedOnEvent(int? id)
        {
            var taskInfo = this.TaskMap[(int)id];

            Output.Debug(DebugType.Testing, "<ScheduleDebug> Task {0} of machine {1}({2}) " +
                "is waiting to receive an event.", taskInfo.Id, taskInfo.Machine.GetType(),
                taskInfo.Machine.Id.MVal);

            taskInfo.IsWaiting = true;
        }

        /// <summary>
        /// Notify that the task received an event that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal void NotifyTaskReceivedEvent(BaseMachine machine)
        {
            var taskInfo = this.GetTaskFromMachine(machine);

            Output.Debug(DebugType.Testing, "<ScheduleDebug> Task {0} of machine {1}({2}) " +
                "received an event and unblocked.", taskInfo.Id, taskInfo.Machine.GetType(),
                taskInfo.Machine.Id.MVal);

            taskInfo.IsWaiting = false;
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

            Output.Debug(DebugType.Testing, "<ScheduleDebug> Blocked task {0} of machine {1}({2}).",
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
        /// <param name="id">TaskId</param>
        internal void NotifyTaskCompleted(int? id)
        {
            if (id == null)
            {
                return;
            }
            
            var taskInfo = this.TaskMap[(int)id];

            Output.Debug(DebugType.Testing, "<ScheduleDebug> Completed task {0} of machine {1}({2}).",
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
                        Output.Debug(DebugType.Testing, "<ScheduleDebug> Unblocked task {0} of " +
                            "machine {1}({2}).", task.Id, task.Machine.GetType(), task.Machine.Id.MVal);
                    }
                }
            }

            this.Schedule(taskInfo.Id);

            Output.Debug(DebugType.Testing, "<ScheduleDebug> Exit task {0} of machine {1}({2}).",
                taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);
        }

        /// <summary>
        /// Wait for the task to start.
        /// </summary>
        /// <param name="id">TaskId</param>
        internal void WaitForTaskToStart(int id)
        {
            var taskInfo = this.TaskMap[id];
            lock (taskInfo)
            {
                while (!taskInfo.HasStarted)
                {
                    System.Threading.Monitor.Wait(taskInfo);
                }
            }
        }

        /// <summary>
        /// Checks if there is already an enabled task for the given machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <returns>Boolean</returns>
        internal bool HasEnabledTaskForMachine(BaseMachine machine)
        {
            var enabledTasks = this.Tasks.Where(task => task.IsEnabled).ToList();
            return enabledTasks.Any(task => task.Machine.Equals(machine));
        }

        /// <summary>
        /// Notify that an assertion has failed.
        /// </summary>
        /// <param name="text">Bug report</param>
        /// <param name="killTasks">Kill tasks</param>
        internal void NotifyAssertionFailure(string text, bool killTasks = true)
        {
            this.BugReport = text;
            ErrorReporter.Report(text);

            this.BugFound = true;

            if (killTasks)
            {
                this.Stop();
            }
        }

        /// <summary>
        /// Stops the scheduler.
        /// </summary>
        internal void Stop()
        {
            this.KillRemainingTasks();
            throw new TaskCanceledException();
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the task id of the given machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <returns>TaskId</returns>
        private TaskInfo GetTaskFromMachine(BaseMachine machine)
        {
            TaskInfo taskInfo = null;
            foreach (var task in this.Tasks)
            {
                if (task.Machine.Equals(machine))
                {
                    taskInfo = task;
                    break;
                }
            }

            return taskInfo;
        }

        /// <summary>
        /// Kills any remaining tasks at the end of the schedule.
        /// </summary>
        private void KillRemainingTasks()
        {
            foreach (var task in this.Tasks)
            {
                task.IsActive = true;
                task.IsEnabled = false;

                if (!task.IsCompleted)
                {
                    lock (task)
                    {
                        System.Threading.Monitor.PulseAll(task);
                    }
                }
            }
        }

        #endregion
    }
}
