//-----------------------------------------------------------------------
// <copyright file="BugFindingScheduler.cs">
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices.Scheduling
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
        /// Number of explored steps.
        /// </summary>
        internal int ExploredSteps
        {
            get { return this.Strategy.GetExploredSteps(); }
        }

        /// <summary>
        /// Maximum number of explored steps.
        /// </summary>
        internal int MaxExploredSteps
        {
            get { return this.Strategy.GetMaxExploredSteps(); }
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
            if (id == null || id == this.Runtime.RootTaskId || this.BugFound)
            {
                return;
            }

            // Check if the scheduling steps bound has been reached.
            if (this.Strategy.HasReachedDepthBound())
            {
                var msg = IO.Format("Scheduling steps bound of " +
                    $"{this.Strategy.GetDepthBound()} reached.");
                if (this.Runtime.Configuration.ConsiderDepthBoundHitAsBug)
                {
                    this.Runtime.BugFinder.NotifyAssertionFailure(msg, true);
                }
                else
                {
                    IO.Debug($"<ScheduleDebug> {msg}");
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
                IO.Debug($"<ScheduleDebug> Unable to schedule task {id}.");
                this.KillRemainingMachines();
                throw new TaskCanceledException();
            }

            MachineInfo next = null;
            if (!this.Strategy.TryGetNext(out next, this.MachineInfos, machineInfo))
            {
                IO.Debug("<ScheduleDebug> Schedule explored.");
                this.KillRemainingMachines();
                throw new TaskCanceledException();
            }

            this.Runtime.ProgramTrace.AddSchedulingChoice(next.Machine);
            if (this.Runtime.Configuration.CacheProgramState &&
                this.Runtime.Configuration.SafetyPrefixBound <= this.ExploredSteps)
            {
                this.Runtime.StateCache.CaptureState(this.Runtime.ProgramTrace.Peek());
            }

            IO.Debug($"<ScheduleDebug> Schedule task {next.Id} of machine " +
                $"{next.Machine.GetType()}({next.Machine.Id.MVal}).");

            if (next.IsWaitingToReceive)
            {
                string message = IO.Format("Livelock detected. Machine " +
                    $"'{next.Machine.GetType()}({next.Machine.Id.MVal})' is " +
                    "waiting for an event, but no other machine is enabled.");
                this.Runtime.BugFinder.NotifyAssertionFailure(message, true);
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
                        IO.Debug($"<ScheduleDebug> Sleep task {machineInfo.Id} of machine " +
                            $"{machineInfo.Machine.GetType()}({machineInfo.Machine.Id.MVal}).");
                        System.Threading.Monitor.Wait(machineInfo);
                        IO.Debug($"<ScheduleDebug> Wake up task {machineInfo.Id} of machine " +
                            $"{machineInfo.Machine.GetType()}({machineInfo.Machine.Id.MVal}).");
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
        /// <returns>Boolean</returns>
        internal bool GetNextNondeterministicChoice(int maxValue, string uniqueId = null)
        {
            // Check if the scheduling steps bound has been reached.
            if (this.Strategy.HasReachedDepthBound())
            {
                var msg = IO.Format("Scheduling steps bound of " +
                    $"{this.Strategy.GetDepthBound()} reached.");
                if (this.Runtime.Configuration.ConsiderDepthBoundHitAsBug)
                {
                    this.Runtime.BugFinder.NotifyAssertionFailure(msg, true);
                }
                else
                {
                    IO.Debug($"<ScheduleDebug> {msg}");
                    this.KillRemainingMachines();
                    throw new TaskCanceledException();
                }
            }

            var choice = false;
            if (!this.Strategy.GetNextChoice(maxValue, out choice))
            {
                IO.Debug("<ScheduleDebug> Schedule explored.");
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

            if (this.Runtime.Configuration.CacheProgramState &&
                this.Runtime.Configuration.SafetyPrefixBound <= this.ExploredSteps)
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

            IO.Debug($"<ScheduleDebug> Created task {machineInfo.Id} for machine " +
                $"{machineInfo.Machine.GetType()}({machineInfo.Machine.Id.MVal}).");

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

            IO.Debug($"<ScheduleDebug> Started task {machineInfo.Id} of machine " +
                $"{machineInfo.Machine.GetType()}({machineInfo.Machine.Id.MVal}).");

            lock (machineInfo)
            {
                machineInfo.HasStarted = true;
                System.Threading.Monitor.PulseAll(machineInfo);
                while (!machineInfo.IsActive)
                {
                    IO.Debug($"<ScheduleDebug> Sleep task {machineInfo.Id} of machine " +
                        $"{machineInfo.Machine.GetType()}({machineInfo.Machine.Id.MVal}).");
                    System.Threading.Monitor.Wait(machineInfo);
                    IO.Debug($"<ScheduleDebug> Wake up task {machineInfo.Id} of machine " +
                        $"{machineInfo.Machine.GetType()}({machineInfo.Machine.Id.MVal}).");
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
            machineInfo.IsWaitingToReceive = true;

            IO.Debug($"<ScheduleDebug> Task {machineInfo.Id} of machine " +
                $"{machineInfo.Machine.GetType()}({machineInfo.Machine.Id.MVal}) " +
                "is waiting to receive an event.");
        }

        /// <summary>
        /// Notify that the machine received an event that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal void NotifyTaskReceivedEvent(AbstractMachine machine)
        {
            var machineInfo = this.MachineInfos.First(mi => mi.Machine.Equals(machine) && !mi.IsCompleted);
            machineInfo.IsWaitingToReceive = false;

            IO.Debug($"<ScheduleDebug> Task {machineInfo.Id} of machine " +
                $"{machineInfo.Machine.GetType()}({machineInfo.Machine.Id.MVal}) " +
                "received an event and unblocked.");
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

            IO.Debug($"<ScheduleDebug> Completed task {machineInfo.Id} of machine " +
                $"{machineInfo.Machine.GetType()}({machineInfo.Machine.Id.MVal}).");

            machineInfo.IsEnabled = false;
            machineInfo.IsCompleted = true;

            this.Schedule();

            IO.Debug($"<ScheduleDebug> Exit task {machineInfo.Id} of machine " +
                $"{machineInfo.Machine.GetType()}({machineInfo.Machine.Id.MVal}).");
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
            if (!this.BugFound)
            {
                this.BugReport = text;

                ErrorReporter.Report(text);

                IO.Log("<StrategyLog> Found bug using " +
                    $"'{this.Runtime.Configuration.SchedulingStrategy}' strategy.");

                if (this.Strategy.GetDescription().Length > 0)
                {
                    IO.Log($"<StrategyLog> {this.Strategy.GetDescription()}");
                }

                this.BugFound = true;

                if (this.Runtime.Configuration.AttachDebugger)
                {
                    System.Diagnostics.Debugger.Break();
                }
            }

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
