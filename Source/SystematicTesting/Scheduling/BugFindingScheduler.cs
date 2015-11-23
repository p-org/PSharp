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

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.SystematicTesting.Scheduling
{
    /// <summary>
    /// Class implementing the basic P# bug-finding scheduler.
    /// </summary>
    internal class BugFindingScheduler
    {
        #region fields

        /// <summary>
        /// The P# runtime.
        /// </summary>
        protected PSharpBugFindingRuntime Runtime;

        /// <summary>
        /// The scheduling strategy to be used for bug-finding.
        /// </summary>
        protected ISchedulingStrategy Strategy;

        /// <summary>
        /// List of machines to schedule.
        /// </summary>
        protected List<MachineInfo> MachineInfos;

        /// <summary>
        /// Map from task ids to machine infos.
        /// </summary>
        protected Dictionary<int, MachineInfo> TaskMap;

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
        /// Maximum number of scheduling points.
        /// </summary>
        internal int MaxSchedulingPoints
        {
            get { return this.Strategy.GetMaxSchedulingSteps(); }
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
        /// <param name="runtime">PSharpBugFindingRuntime</param>
        /// <param name="strategy">SchedulingStrategy</param>
        internal BugFindingScheduler(PSharpBugFindingRuntime runtime, ISchedulingStrategy strategy)
        {
            this.Runtime = runtime;
            this.Strategy = strategy;
            this.MachineInfos = new List<MachineInfo>();
            this.TaskMap = new Dictionary<int, MachineInfo>();
            this.BugFound = false;
        }

        /// <summary>
        /// Schedules the next machine to execute.
        /// </summary>
        internal virtual void Schedule()
        {
            int? id = Task.CurrentId;
            if (id == null || id == this.Runtime.RootTaskId)
            {
                return;
            }

            // Check if the exploration depth-bound has been reached.
            if (this.Strategy.HasReachedDepthBound())
            {
                var msg = Output.Format("Depth bound of {0} reached.", this.Strategy.GetDepthBound());
                if (this.Runtime.Configuration.ConsiderDepthBoundHitAsBug)
                {
                    this.Runtime.BugFinder.NotifyAssertionFailure(msg, true);
                }
                else
                {
                    Output.Debug("<ScheduleDebug> {0}", msg);

                    this.KillRemainingMachines();
                    throw new TaskCanceledException();
                }
            }

            MachineInfo machineInfo = null;
            if (this.TaskMap.ContainsKey((int)id))
            {
                machineInfo = this.TaskMap[(int)id];
            }
            else
            {
                Output.Debug("<ScheduleDebug> Unable to schedule machineInfo {0}.", id);
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
                Output.Debug("<ScheduleDebug> Schedule explored.");
                this.KillRemainingMachines();
                throw new TaskCanceledException();
            }

            this.Runtime.ProgramTrace.AddSchedulingChoice(next.Machine);
            if (this.Runtime.Configuration.CheckLiveness &&
                this.Runtime.Configuration.CacheProgramState &&
                this.Runtime.Configuration.SafetyPrefixBound <= this.SchedulingPoints)
            {
                this.Runtime.StateCache.CaptureState(this.Runtime.ProgramTrace.Peek());
            }

            Output.Debug("<ScheduleDebug> Schedule machineInfo {0} of machine {1}({2}).",
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
                        Output.Debug("<ScheduleDebug> Sleep machineInfo {0} of machine {1}({2}).",
                            machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);
                        System.Threading.Monitor.Wait(machineInfo);
                        Output.Debug("<ScheduleDebug> Wake up machineInfo {0} of machine {1}({2}).",
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
        /// Returns the next nondeterministic choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean value</returns>
        internal bool GetNextNondeterministicChoice(int maxValue, string uniqueId = null)
        {
            var choice = false;
            if (!this.Strategy.GetNextChoice(maxValue, out choice))
            {
                Output.Debug("<ScheduleDebug> Schedule explored.");
                this.KillRemainingMachines();
                throw new TaskCanceledException();
            }

            if (uniqueId == null)
            {
                this.Runtime.ProgramTrace.AddNondeterministicChoice(choice);
            }
            else
            {
                this.Runtime.ProgramTrace.AddFairNondeterministicChoice(uniqueId, choice);
            }
            
            if (this.Runtime.Configuration.CheckLiveness &&
                this.Runtime.Configuration.CacheProgramState &&
                this.Runtime.Configuration.SafetyPrefixBound <= this.SchedulingPoints)
            {
                this.Runtime.StateCache.CaptureState(this.Runtime.ProgramTrace.Peek());
            }

            return choice;
        }

        /// <summary>
        /// Returns the enabled machines.
        /// </summary>
        /// <returns>Enabled machines</returns>
        internal HashSet<AbstractMachine> GetEnabledMachines()
        {
            var enabledMachines = new HashSet<AbstractMachine>();
            foreach (var machineInfo in this.MachineInfos)
            {
                if (machineInfo.IsEnabled)
                {
                    enabledMachines.Add(machineInfo.Machine);
                }
            }

            return enabledMachines;
        }

        /// <summary>
        /// Notify that a new task has been created for the given machine.
        /// </summary>
        /// <param name="id">TaskId</param>
        /// <param name="machine">Machine</param>
        internal virtual void NotifyNewTaskCreated(int id, AbstractMachine machine)
        {
            var machineInfo = new MachineInfo(id, machine);

            Output.Debug("<ScheduleDebug> Created machineInfo {0} for machine {1}({2}).",
                machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);

            if (this.MachineInfos.Count == 0)
            {
                machineInfo.IsActive = true;
            }

            this.MachineInfos.Add(machineInfo);
            this.TaskMap.Add(id, machineInfo);
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

            var machineInfo = this.TaskMap[(int)id];

            Output.Debug("<ScheduleDebug> Started machineInfo {0} of machine {1}({2}).",
                machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);

            lock (machineInfo)
            {
                machineInfo.HasStarted = true;
                System.Threading.Monitor.PulseAll(machineInfo);
                while (!machineInfo.IsActive)
                {
                    Output.Debug("<ScheduleDebug> Sleep machineInfo {0} of machine {1}({2}).",
                        machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);
                    System.Threading.Monitor.Wait(machineInfo);
                    Output.Debug("<ScheduleDebug> Wake up machineInfo {0} of machine {1}({2}).",
                        machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);
                }

                if (!machineInfo.IsEnabled)
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
            var machineInfo = this.TaskMap[(int)id];

            Output.Debug("<ScheduleDebug> Task {0} of machine {1}({2}) " +
                "is waiting to receive an event.", machineInfo.Id, machineInfo.Machine.GetType(),
                machineInfo.Machine.Id.MVal);

            machineInfo.IsWaiting = true;
        }

        /// <summary>
        /// Notify that the machine received an event that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal void NotifyTaskReceivedEvent(AbstractMachine machine)
        {
            var machineInfo = this.GetInfoFromMachine(machine);

            Output.Debug("<ScheduleDebug> Task {0} of machine {1}({2}) " +
                "received an event and unblocked.", machineInfo.Id, machineInfo.Machine.GetType(),
                machineInfo.Machine.Id.MVal);

            machineInfo.IsWaiting = false;
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
            
            var machineInfo = this.TaskMap[(int)id];

            Output.Debug("<ScheduleDebug> Completed machineInfo {0} of machine {1}({2}).",
                machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);

            machineInfo.IsEnabled = false;
            machineInfo.IsCompleted = true;

            this.Schedule();

            Output.Debug("<ScheduleDebug> Exit machineInfo {0} of machine {1}({2}).",
                machineInfo.Id, machineInfo.Machine.GetType(), machineInfo.Machine.Id.MVal);
        }

        /// <summary>
        /// Wait for the task to start.
        /// </summary>
        /// <param name="id">TaskId</param>
        internal void WaitForTaskToStart(int id)
        {
            var machineInfo = this.TaskMap[id];
            lock (machineInfo)
            {
                while (!machineInfo.HasStarted)
                {
                    System.Threading.Monitor.Wait(machineInfo);
                }
            }
        }

        /// <summary>
        /// Checks if there is already an enabled task for the given machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <returns>Boolean</returns>
        internal bool HasEnabledTaskForMachine(AbstractMachine machine)
        {
            var enabledTasks = this.MachineInfos.Where(machineInfo => machineInfo.IsEnabled).ToList();
            return enabledTasks.Any(machineInfo => machineInfo.Machine.Equals(machine));
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
            this.KillRemainingMachines();
            throw new TaskCanceledException();
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Returns the info of the given machine.
        /// </summary>
        /// <param name="machine">Machine</param>
        /// <returns>TaskId</returns>
        protected MachineInfo GetInfoFromMachine(AbstractMachine machine)
        {
            MachineInfo machineInfo = null;
            foreach (var mi in this.MachineInfos)
            {
                if (mi.Machine.Equals(machine))
                {
                    machineInfo = mi;
                    break;
                }
            }

            return machineInfo;
        }

        /// <summary>
        /// Kills any remaining machines at the end of the schedule.
        /// </summary>
        protected void KillRemainingMachines()
        {
            foreach (var machineInfo in this.MachineInfos)
            {
                machineInfo.IsActive = true;
                machineInfo.IsEnabled = false;

                if (!machineInfo.IsCompleted)
                {
                    lock (machineInfo)
                    {
                        System.Threading.Monitor.PulseAll(machineInfo);
                    }
                }
            }
        }

        #endregion
    }
}
