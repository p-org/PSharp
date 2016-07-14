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
using System.Collections.Concurrent;
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
        /// Collection of machines to schedule.
        /// </summary>
        protected ConcurrentBag<MachineInfo> MachineInfos;

        /// <summary>
        /// Map from task ids to machine infos.
        /// </summary>
        protected ConcurrentDictionary<int, MachineInfo> TaskMap;

        /// <summary>
        /// Checks if the scheduler is running.
        /// </summary>
        protected bool IsSchedulerRunning;

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
        /// Checks if the schedule has been fully explored.
        /// </summary>
        protected internal bool HasFullyExploredSchedule { get; protected set; }

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
            this.MachineInfos = new ConcurrentBag<MachineInfo>();
            this.TaskMap = new ConcurrentDictionary<int, MachineInfo>();
            this.IsSchedulerRunning = true;
            this.BugFound = false;
            this.HasFullyExploredSchedule = false;
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

            if (this.BugFound || !this.IsSchedulerRunning)
            {
                this.Stop();
            }

            // Checks if the scheduling steps bound has been reached.
            this.CheckIfSchedulingStepsBoundIsReached(false);

            MachineInfo machineInfo = null;
            if (!this.TaskMap.TryGetValue((int)id, out machineInfo))
            {
                IO.Debug($"<ScheduleDebug> Unable to schedule task '{id}'.");
                this.Stop();
            }

            MachineInfo next = null;
            if (!this.Strategy.TryGetNext(out next, this.MachineInfos, machineInfo))
            {
                IO.Debug("<ScheduleDebug> Schedule explored.");
                this.HasFullyExploredSchedule = true;
                this.Stop();
            }

            this.Runtime.ScheduleTrace.AddSchedulingChoice(next.Machine);

            // Checks the liveness monitors for potential liveness bugs.
            this.Runtime.LivenessChecker.CheckLivenessAtShedulingStep();
            if (this.Runtime.Configuration.CacheProgramState &&
                this.Runtime.Configuration.SafetyPrefixBound <= this.ExploredSteps)
            {
                this.Runtime.StateCache.CaptureState(this.Runtime.ScheduleTrace.Peek());
            }

            IO.Debug($"<ScheduleDebug> Schedule task '{next.Id}' of machine " +
                $"'{next.Machine.Id}'.");

            if (next.IsWaitingToReceive)
            {
                string message = IO.Format("Livelock detected. Machine " +
                    $"'{next.Machine.Id}' is waiting for an event, " +
                    "but no other machine is enabled.");
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
        /// Returns the next nondeterministic boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="uniqueId">Unique id</param>
        /// <returns>Boolean</returns>
        internal bool GetNextNondeterministicBooleanChoice(
            int maxValue, string uniqueId = null)
        {
            // Checks if the scheduling steps bound has been reached.
            this.CheckIfSchedulingStepsBoundIsReached(false);

            var choice = false;
            if (!this.Strategy.GetNextBooleanChoice(maxValue, out choice))
            {
                IO.Debug("<ScheduleDebug> Schedule explored.");
                this.KillRemainingMachines();
                throw new OperationCanceledException();
            }

            if (uniqueId == null)
            {
                this.Runtime.ScheduleTrace.AddNondeterministicBooleanChoice(choice);
            }
            else
            {
                this.Runtime.ScheduleTrace.AddFairNondeterministicBooleanChoice(uniqueId, choice);
            }

            // Checks the liveness monitors for potential liveness bugs.
            this.Runtime.LivenessChecker.CheckLivenessAtShedulingStep();
            if (this.Runtime.Configuration.CacheProgramState &&
                this.Runtime.Configuration.SafetyPrefixBound <= this.ExploredSteps)
            {
                this.Runtime.StateCache.CaptureState(this.Runtime.ScheduleTrace.Peek());
            }

            return choice;
        }

        /// <summary>
        /// Returns the next nondeterministic integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <returns>Integer</returns>
        internal int GetNextNondeterministicIntegerChoice(int maxValue)
        {
            // Checks if the scheduling steps bound has been reached.
            this.CheckIfSchedulingStepsBoundIsReached(false);

            var choice = 0;
            if (!this.Strategy.GetNextIntegerChoice(maxValue, out choice))
            {
                IO.Debug("<ScheduleDebug> Schedule explored.");
                this.KillRemainingMachines();
                throw new OperationCanceledException();
            }

            this.Runtime.ScheduleTrace.AddNondeterministicIntegerChoice(choice);

            // Checks the liveness monitors for potential liveness bugs.
            this.Runtime.LivenessChecker.CheckLivenessAtShedulingStep();
            if (this.Runtime.Configuration.CacheProgramState &&
                this.Runtime.Configuration.SafetyPrefixBound <= this.ExploredSteps)
            {
                this.Runtime.StateCache.CaptureState(this.Runtime.ScheduleTrace.Peek());
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

            IO.Debug($"<ScheduleDebug> Created task '{machineInfo.Id}' for machine " +
                $"'{machineInfo.Machine.Id}'.");

            if (this.MachineInfos.Count == 0)
            {
                machineInfo.IsActive = true;
            }

            this.MachineInfos.Add(machineInfo);
            this.TaskMap.TryAdd(id, machineInfo);
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
        /// Notify that the task is waiting to receive an event.
        /// </summary>
        /// <param name="id">TaskId</param>
        internal void NotifyTaskBlockedOnEvent(int? id)
        {
            var machineInfo = this.TaskMap[(int)id];
            machineInfo.IsWaitingToReceive = true;

            IO.Debug($"<ScheduleDebug> Task '{machineInfo.Id}' of machine " +
                $"'{machineInfo.Machine.Id}' is waiting to receive an event.");
        }

        /// <summary>
        /// Notify that the machine received an event that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal void NotifyTaskReceivedEvent(AbstractMachine machine)
        {
            var machineInfo = this.MachineInfos.First(mi => mi.Machine.Equals(machine) && !mi.IsCompleted);
            machineInfo.IsWaitingToReceive = false;

            IO.Debug($"<ScheduleDebug> Task '{machineInfo.Id}' of machine " +
                $"'{machineInfo.Machine.Id}' received an event and unblocked.");
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

            IO.Debug($"<ScheduleDebug> Completed task '{machineInfo.Id}' of machine " +
                $"'{machineInfo.Machine.Id}'.");

            machineInfo.IsEnabled = false;
            machineInfo.IsCompleted = true;

            this.Schedule();

            IO.Debug($"<ScheduleDebug> Exit task '{machineInfo.Id}' of machine " +
                $"'{machineInfo.Machine.Id}'.");
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
        /// Switches the scheduler to the specified scheduling strategy,
        /// and returns the previously installed strategy.
        /// </summary>
        /// <param name="strategy">ISchedulingStrategy</param>
        /// <returns>ISchedulingStrategy</returns>
        internal ISchedulingStrategy SwitchSchedulingStrategy(ISchedulingStrategy strategy)
        {
            ISchedulingStrategy previous = this.Strategy;
            this.Strategy = strategy;
            return previous;
        }

        /// <summary>
        /// Stops the scheduler.
        /// </summary>
        internal void Stop()
        {
            this.IsSchedulerRunning = false;
            this.KillRemainingMachines();
            throw new OperationCanceledException();
        }

        #endregion

        #region protected methods

        /// <summary>
        /// Returns the number of available machines to schedule.
        /// </summary>
        /// <returns>Int</returns>
        protected int NumberOfAvailableMachinesToSchedule()
        {
            var availableMachines = this.MachineInfos.Where(
                m => m.IsEnabled && !m.IsBlocked && !m.IsWaitingToReceive).ToList();
            return availableMachines.Count;
        }

        /// <summary>
        /// Checks if the scheduling steps bound has been reached.
        /// If yes, it stops the scheduler and kills all enabled
        /// machines.
        /// </summary>
        /// <param name="isSchedulingDecision">Is a machine scheduling decision</param>
        protected void CheckIfSchedulingStepsBoundIsReached(bool isSchedulingDecision)
        {
            if (this.Strategy.HasReachedMaxSchedulingSteps())
            {
                var msg = IO.Format("Scheduling steps bound of " +
                    $"{this.Runtime.Configuration.MaxSchedulingSteps} reached.");

                if (isSchedulingDecision &&
                    this.NumberOfAvailableMachinesToSchedule() == 0)
                {
                    this.HasFullyExploredSchedule = true;
                }

                if (this.Runtime.Configuration.ConsiderDepthBoundHitAsBug)
                {
                    this.Runtime.BugFinder.NotifyAssertionFailure(msg, true);
                }
                else
                {
                    IO.Debug($"<ScheduleDebug> {msg}");
                    this.KillRemainingMachines();
                    throw new OperationCanceledException();
                }
            }
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
