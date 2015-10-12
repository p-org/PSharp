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
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class implementing the basic P# bug-finding scheduler.
    /// </summary>
    internal class BugFindingScheduler
    {
        #region fields

        /// <summary>
        /// The scheduling strategy to be used for bug-finding.
        /// </summary>
        protected ISchedulingStrategy Strategy;

        /// <summary>
        /// List of tasks to schedule.
        /// </summary>
        protected List<TaskInfo> Tasks;

        /// <summary>
        /// Map from task ids to task infos.
        /// </summary>
        protected Dictionary<int, TaskInfo> TaskMap;

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
            this.TaskMap = new Dictionary<int, TaskInfo>();
            this.BugFound = false;
        }

        /// <summary>
        /// Schedules the next machine to execute.
        /// </summary>
        internal virtual void Schedule()
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

            TaskInfo taskInfo = null;
            if (this.TaskMap.ContainsKey((int)id))
            {
                taskInfo = this.TaskMap[(int)id];
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
        /// Returns the next nondeterministic choice.
        /// </summary>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean value</returns>
        internal bool GetNextNondeterministicChoice(string uniqueId = null)
        {
            var choice = false;
            if (!this.Strategy.GetNextChoice(out choice))
            {
                Output.Debug("<ScheduleDebug> Schedule explored.");
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
            
            if (PSharpRuntime.Configuration.CheckLiveness &&
                PSharpRuntime.Configuration.CacheProgramState &&
                PSharpRuntime.Configuration.SafetyPrefixBound <= this.SchedulingPoints)
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
        internal virtual void NotifyNewTaskCreated(int id, BaseMachine machine)
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
        }

        /// <summary>
        /// Notify that the task has started.
        /// </summary>
        internal virtual void NotifyTaskStarted()
        {
            int? id = Task.CurrentId;
            if (id == null)
            {
                return;
            }

            var taskInfo = this.TaskMap[(int)id];

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
        /// Notify that the task is waiting to receive an event.
        /// </summary>
        /// <param name="id">TaskId</param>
        internal void NotifyTaskBlockedOnEvent(int? id)
        {
            var taskInfo = this.TaskMap[(int)id];

            Output.Debug("<ScheduleDebug> Task {0} of machine {1}({2}) " +
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

            Output.Debug("<ScheduleDebug> Task {0} of machine {1}({2}) " +
                "received an event and unblocked.", taskInfo.Id, taskInfo.Machine.GetType(),
                taskInfo.Machine.Id.MVal);

            taskInfo.IsWaiting = false;
        }

        /// <summary>
        /// Notify that the task has completed.
        /// </summary>
        internal virtual void NotifyTaskCompleted()
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

            this.Schedule();

            Output.Debug("<ScheduleDebug> Exit task {0} of machine {1}({2}).",
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

            Output.Log("<StrategyLog> Found bug using the " + this.Strategy.GetDescription() + " strategy.");

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

        #region protected methods

        /// <summary>
        /// Returns the task id of the given machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <returns>TaskId</returns>
        protected TaskInfo GetTaskFromMachine(BaseMachine machine)
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
        protected void KillRemainingTasks()
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
