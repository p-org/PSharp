//-----------------------------------------------------------------------
// <copyright file="Scheduler.cs" company="Microsoft">
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

using Microsoft.PSharp.Tooling;

namespace Microsoft.PSharp.Scheduling
{
    /// <summary>
    /// Class implementing the P# bug-finding scheduler.
    /// </summary>
    internal sealed class Scheduler
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
        /// Map from task ids to task infos.
        /// </summary>
        private Dictionary<int, TaskInfo> TaskMap;

        /// <summary>
        /// True if the scheduler managed to reach a terminal
        /// state in the program.
        /// </summary>
        internal bool ProgramTerminated
        {
            get; private set;
        }

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
            get; private set;
        }

        #endregion

        #region internal methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="strategy">SchedulingStrategy</param>
        internal Scheduler(ISchedulingStrategy strategy)
        {
            this.Strategy = strategy;
            this.Tasks = new List<TaskInfo>();
            this.TaskMap = new Dictionary<int, TaskInfo>();
            this.ProgramTerminated = false;
            this.BugFound = false;
            this.SchedulingPoints = 0;
        }

        /// <summary>
        /// Schedules the next machine to execute.
        /// </summary>
        /// <param name="id">TaskId</param>
        internal void Schedule(int? id)
        {
            if (id == null || !this.TaskMap.ContainsKey((int)id))
            {
                return;
            }

            var taskInfo = this.TaskMap[(int)id];

            TaskInfo next = null;
            if (Configuration.DepthBound > 0 && this.SchedulingPoints == Configuration.DepthBound)
            {
                Output.Debug(DebugType.Testing, "<ScheduleDebug> Depth bound of {0} reached.",
                    Configuration.DepthBound);
                this.KillRemainingTasks();
                throw new TaskCanceledException();
            }
            else if (!this.Strategy.TryGetNext(out next, this.Tasks))
            {
                Output.Debug(DebugType.Testing, "<ScheduleDebug> Schedule explored.");
                this.ProgramTerminated = true;
                this.KillRemainingTasks();
                throw new TaskCanceledException();
            }

            if (Configuration.CheckLiveness && Configuration.CacheProgramState)
            {
                PSharpRuntime.StateExplorer.CacheStateAtSchedulingChoice(next.Machine);
            }

            Output.Debug(DebugType.Testing, "<ScheduleDebug> Schedule task {0} of machine {1}({2}).",
                next.Id, next.Machine.GetType(), next.Machine.Id.MVal);

            if (!taskInfo.IsCompleted)
            {
                this.SchedulingPoints++;
            }

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

            if (Configuration.CheckLiveness && Configuration.CacheProgramState &&
                uniqueId != null)
            {
                PSharpRuntime.StateExplorer.CacheStateAtNondeterministicChoice(uniqueId, choice);
            }

            return choice;
        }

        /// <summary>
        /// Returns the enabled machines.
        /// </summary>
        /// <returns>Enabled machines</returns>
        internal HashSet<Machine> GetEnabledMachines()
        {
            var enabledMachines = new HashSet<Machine>();
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
        internal void NotifyNewTaskCreated(int id, Machine machine)
        {
            var taskInfo = new TaskInfo(id, machine);

            Output.Debug(DebugType.Testing, "<ScheduleDebug> Created task {0} for machine {1}({2}).",
                taskInfo.Id, taskInfo.Machine.GetType(), taskInfo.Machine.Id.MVal);

            if (this.Tasks.Count == 0)
            {
                taskInfo.IsActive = true;
            }

            this.TaskMap.Add(id, taskInfo);
            this.Tasks.Add(taskInfo);
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

            var taskInfo = this.TaskMap[(int)id];

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
        internal bool HasEnabledTaskForMachine(Machine machine)
        {
            var enabledTasks = this.Tasks.Where(task => task.IsEnabled).ToList();
            return enabledTasks.Any(task => task.Machine.Equals(machine));
        }

        /// <summary>
        /// Notify that an assertion has failed.
        /// </summary>
        /// <param name="terminateScheduler">Terminate the scheduler</param>
        internal void NotifyAssertionFailure(bool terminateScheduler = true)
        {
            this.BugFound = true;

            if (terminateScheduler)
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
