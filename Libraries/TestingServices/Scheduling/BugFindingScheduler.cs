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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Class implementing the basic P# bug-finding scheduler.
    /// </summary>
    internal sealed class BugFindingScheduler
    {
        #region fields

        /// <summary>
        /// The P# bug-finding runtime.
        /// </summary>
        private BugFindingRuntime Runtime;

        /// <summary>
        /// The scheduling strategy to be used for bug-finding.
        /// </summary>
        private ISchedulingStrategy Strategy;

        /// <summary>
        /// Map from task ids to machine infos.
        /// </summary>
        private ConcurrentDictionary<int, MachineInfo> TaskMap;
        
        /// <summary>
        /// The scheduler completion source.
        /// </summary>
        private readonly TaskCompletionSource<bool> CompletionSource;
        
        /// <summary>
        /// Checks if the scheduler is running.
        /// </summary>
        private bool IsSchedulerRunning;

        #endregion

        #region properties

        /// <summary>
        /// Number of explored steps.
        /// </summary>
        internal int ExploredSteps => this.Strategy.GetExploredSteps();

        /// <summary>
        /// Checks if the schedule has been fully explored.
        /// </summary>
        internal bool HasFullyExploredSchedule { get; private set; }

        /// <summary>
        /// True if a bug was found.
        /// </summary>
        internal bool BugFound { get; private set; }

        /// <summary>
        /// Bug report.
        /// </summary>
        internal string BugReport { get; private set; }

        #endregion

        #region constructors

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">BugFindingRuntime</param>
        /// <param name="strategy">SchedulingStrategy</param>
        internal BugFindingScheduler(BugFindingRuntime runtime, ISchedulingStrategy strategy)
        {
            this.Runtime = runtime;
            this.Strategy = strategy;
            this.TaskMap = new ConcurrentDictionary<int, MachineInfo>();
            this.CompletionSource = new TaskCompletionSource<bool>();
            this.IsSchedulerRunning = true;
            this.BugFound = false;
            this.HasFullyExploredSchedule = false;
        }

        #endregion

        #region scheduling methods

        /// <summary>
        /// Schedules the next machine to execute.
        /// </summary>
        internal void Schedule()
        {
            int? taskId = Task.CurrentId;

            // If the caller is the root task, then return.
            if (taskId != null && taskId == this.Runtime.RootTaskId)
            {
                return;
            }

            if (!this.IsSchedulerRunning)
            {
                this.Stop();
            }

            // Checks if synchronisation not controlled by P# was used.
            this.CheckIfExternalSynchronizationIsUsed();

            // Checks if the scheduling steps bound has been reached.
            this.CheckIfSchedulingStepsBoundIsReached();

            MachineInfo machineInfo = this.TaskMap[(int)taskId];
            MachineInfo next = null;

            var choices = this.TaskMap.Values.OrderBy(mi => mi.Machine.Id.Value);
            if (!this.Strategy.TryGetNext(out next, choices, machineInfo))
            {
                Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                this.HasFullyExploredSchedule = true;
                this.Stop();
            }

            this.Runtime.ScheduleTrace.AddSchedulingChoice(next.Machine.Id);
            next.Machine.ProgramCounter = 0;

            if (this.Runtime.Configuration.CacheProgramState &&
                this.Runtime.Configuration.SafetyPrefixBound <= this.ExploredSteps)
            {
                this.Runtime.StateCache.CaptureState(this.Runtime.ScheduleTrace.Peek());
            }
            //this.Runtime.GetProgramStatePrint();
            // Checks the liveness monitors for potential liveness bugs.
            this.Runtime.LivenessChecker.CheckLivenessAtShedulingStep();

            Debug.WriteLine($"<ScheduleDebug> Schedule task '{next.Id}' of machine " +
                $"'{next.Machine.Id}'.");

            if (next.IsWaitingToReceive)
            {
                string message = IO.Utilities.Format("Livelock detected. Machine " +
                    $"'{next.Machine.Id}' is waiting for an event, " +
                    "but no other machine is enabled.");
                this.Runtime.Scheduler.NotifyAssertionFailure(message, true);
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
                        Debug.WriteLine($"<ScheduleDebug> Sleep task '{machineInfo.Id}' of machine " +
                            $"'{machineInfo.Machine.Id}'.");
                        System.Threading.Monitor.Wait(machineInfo);
                        Debug.WriteLine($"<ScheduleDebug> Wake up task '{machineInfo.Id}' of machine " +
                            $"'{machineInfo.Machine.Id}'.");
                    }

                    if (!machineInfo.IsEnabled)
                    {
                        throw new ExecutionCanceledException();
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
            // Checks if synchronisation not controlled by P# was used.
            this.CheckIfExternalSynchronizationIsUsed();

            // Checks if the scheduling steps bound has been reached.
            this.CheckIfSchedulingStepsBoundIsReached();

            var choice = false;
            if (!this.Strategy.GetNextBooleanChoice(maxValue, out choice))
            {
                Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                this.Stop();
            }

            if (uniqueId == null)
            {
                this.Runtime.ScheduleTrace.AddNondeterministicBooleanChoice(choice);
            }
            else
            {
                this.Runtime.ScheduleTrace.AddFairNondeterministicBooleanChoice(uniqueId, choice);
            }

            foreach(var m in TaskMap.Values)
            {
                if (m.IsActive)
                {
                    m.Machine.ProgramCounter++;
                    break;
                }
            }
            
            if (this.Runtime.Configuration.CacheProgramState &&
                this.Runtime.Configuration.SafetyPrefixBound <= this.ExploredSteps)
            {
                this.Runtime.StateCache.CaptureState(this.Runtime.ScheduleTrace.Peek());
            }
            //this.Runtime.GetProgramStatePrint();
            // Checks the liveness monitors for potential liveness bugs.
            this.Runtime.LivenessChecker.CheckLivenessAtShedulingStep();
            
            return choice;
        }

        /// <summary>
        /// Returns the next nondeterministic integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <returns>Integer</returns>
        internal int GetNextNondeterministicIntegerChoice(int maxValue)
        {
            // Checks if synchronisation not controlled by P# was used.
            this.CheckIfExternalSynchronizationIsUsed();

            // Checks if the scheduling steps bound has been reached.
            this.CheckIfSchedulingStepsBoundIsReached();

            var choice = 0;
            if (!this.Strategy.GetNextIntegerChoice(maxValue, out choice))
            {
                Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                this.Stop();
            }

            this.Runtime.ScheduleTrace.AddNondeterministicIntegerChoice(choice);
            
            if (this.Runtime.Configuration.CacheProgramState &&
                this.Runtime.Configuration.SafetyPrefixBound <= this.ExploredSteps)
            {
                this.Runtime.StateCache.CaptureState(this.Runtime.ScheduleTrace.Peek());
            }
            //this.Runtime.GetProgramStatePrint();
            // Checks the liveness monitors for potential liveness bugs.
            this.Runtime.LivenessChecker.CheckLivenessAtShedulingStep();

            return choice;
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
        /// Notify that a new task has been created for the given machine.
        /// </summary>
        /// <param name="id">TaskId</param>
        /// <param name="machine">Machine</param>
        internal void NotifyNewTaskCreated(int id, AbstractMachine machine)
        {
            var machineInfo = new MachineInfo(id, machine);

            Debug.WriteLine($"<ScheduleDebug> Created task '{machineInfo.Id}' for machine " +
                $"'{machineInfo.Machine.Id}'.");

            if (this.TaskMap.Count == 0)
            {
                machineInfo.IsActive = true;
            }

            this.TaskMap.TryAdd(id, machineInfo);
        }

        /// <summary>
        /// Notify that the task has started.
        /// </summary>
        internal void NotifyTaskStarted()
        {
            int? id = Task.CurrentId;
            if (id == null)
            {
                return;
            }

            var machineInfo = this.TaskMap[(int)id];

            Debug.WriteLine($"<ScheduleDebug> Started task '{machineInfo.Id}' of machine " +
                $"'{machineInfo.Machine.Id}'.");

            lock (machineInfo)
            {
                machineInfo.HasStarted = true;
                System.Threading.Monitor.PulseAll(machineInfo);
                while (!machineInfo.IsActive)
                {
                    Debug.WriteLine($"<ScheduleDebug> Sleep task '{machineInfo.Id}' of machine " +
                        $"'{machineInfo.Machine.Id}'.");
                    System.Threading.Monitor.Wait(machineInfo);
                    Debug.WriteLine($"<ScheduleDebug> Wake up task '{machineInfo.Id}' of machine " +
                        $"'{machineInfo.Machine.Id}'.");
                }

                if (!machineInfo.IsEnabled)
                {
                    throw new ExecutionCanceledException();
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

            Debug.WriteLine($"<ScheduleDebug> Task '{machineInfo.Id}' of machine " +
                $"'{machineInfo.Machine.Id}' is waiting to receive an event.");
        }

        /// <summary>
        /// Notify that the machine received an event that it was waiting for.
        /// </summary>
        /// <param name="machine">Machine</param>
        internal void NotifyTaskReceivedEvent(AbstractMachine machine)
        {
            var machineInfo = this.TaskMap.Values.First(mi => mi.Machine.Equals(machine) && !mi.IsCompleted);
            machineInfo.IsWaitingToReceive = false;

            Debug.WriteLine($"<ScheduleDebug> Task '{machineInfo.Id}' of machine " +
                $"'{machineInfo.Machine.Id}' received an event and unblocked.");
        }

        /// <summary>
        /// Notify that the task has completed.
        /// </summary>
        internal void NotifyTaskCompleted()
        {
            int? id = Task.CurrentId;
            if (id == null)
            {
                return;
            }

            var machineInfo = this.TaskMap[(int)id];

            Debug.WriteLine($"<ScheduleDebug> Completed task '{machineInfo.Id}' of machine " +
                $"'{machineInfo.Machine.Id}'.");

            machineInfo.IsEnabled = false;
            machineInfo.IsCompleted = true;

            this.Schedule();

            Debug.WriteLine($"<ScheduleDebug> Exit task '{machineInfo.Id}' of machine " +
                $"'{machineInfo.Machine.Id}'.");

            this.TaskMap.TryRemove((int)id, out machineInfo);
        }

        /// <summary>
        /// Notify that an assertion has failed.
        /// </summary>
        /// <param name="text">Bug report</param>
        /// <param name="killTasks">Kill tasks</param>
        internal void NotifyAssertionFailure(string text, bool killTasks = true)
        {
            System.Console.WriteLine("Number of scheduled steps: " + this.Strategy.GetExploredSteps());
            if (!this.BugFound)
            {
                this.BugReport = text;

                ErrorReporter.Report(this.Runtime.Logger, text);

                this.Runtime.Log("<StrategyLog> Found bug using " +
                    $"'{this.Runtime.Configuration.SchedulingStrategy}' strategy.");

                if (this.Strategy.GetDescription().Length > 0)
                {
                    this.Runtime.Log($"<StrategyLog> {this.Strategy.GetDescription()}");
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
            this.IsSchedulerRunning = false;
            this.KillRemainingMachines();

            // Check if the completion source is completed. If not synchronize on
            // it (as it can only be set once) and set its result.
            if (!this.CompletionSource.Task.IsCompleted)
            {
                lock (this.CompletionSource)
                {
                    if (!this.CompletionSource.Task.IsCompleted)
                    {
                        this.CompletionSource.SetResult(true);
                    }
                }
            }

            throw new ExecutionCanceledException();
        }

        /// <summary>
        /// Blocks until the scheduler terminates.
        /// </summary>
        internal void Wait() => this.CompletionSource.Task.Wait();

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

        #endregion

        #region utilities

        /// <summary>
        /// Returns the enabled machines.
        /// </summary>
        /// <returns>Enabled machines</returns>
        internal HashSet<MachineId> GetEnabledMachines()
        {
            var enabledMachines = new HashSet<MachineId>();
            foreach (var machineInfo in this.TaskMap.Values)
            {
                if (machineInfo.IsEnabled && !machineInfo.IsWaitingToReceive)
                {
                    enabledMachines.Add(machineInfo.Machine.Id);
                }
            }

            return enabledMachines;
        }

        /// <summary>
        /// Returns a test report with the scheduling statistics.
        /// </summary>
        /// <returns>TestReport</returns>
        internal TestReport GetReport()
        {
            TestReport report = new TestReport(this.Runtime.Configuration);
            report.NumberOfDiscardedCycles += this.Runtime.LivenessChecker.DiscardedCycles;
            if (this.BugFound)
            {
                report.NumOfFoundBugs++;
                report.BugReports.Add(this.BugReport);
            }

            if (this.Strategy.IsFair())
            {
                report.NumOfExploredFairSchedules++;

                if (this.Strategy.HasReachedMaxSchedulingSteps())
                {
                    report.MaxFairStepsHitInFairTests++;
                }

                if (this.ExploredSteps >= report.Configuration.MaxUnfairSchedulingSteps)
                {
                    report.MaxUnfairStepsHitInFairTests++;
                }

                if (!this.Strategy.HasReachedMaxSchedulingSteps())
                {
                    report.TotalExploredFairSteps += this.ExploredSteps;

                    if (report.MinExploredFairSteps < 0 ||
                        report.MinExploredFairSteps > this.ExploredSteps)
                    {
                        report.MinExploredFairSteps = this.ExploredSteps;
                    }

                    if (report.MaxExploredFairSteps < this.ExploredSteps)
                    {
                        report.MaxExploredFairSteps = this.ExploredSteps;
                    }
                }
            }
            else
            {
                report.NumOfExploredUnfairSchedules++;

                if (this.Strategy.HasReachedMaxSchedulingSteps())
                {
                    report.MaxUnfairStepsHitInUnfairTests++;
                }
            }

            return report;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the number of available machines to schedule.
        /// </summary>
        /// <returns>Int</returns>
        private int NumberOfAvailableMachinesToSchedule()
        {
            var availableMachines = this.TaskMap.Values.Where(
                m => m.IsEnabled && !m.IsWaitingToReceive).ToList();
            return availableMachines.Count;
        }

        /// <summary>
        /// Checks if external (non-P#) synchronisation was used to invoke
        /// the scheduler. If yes, it stops the scheduler, reports an error
        /// and kills all enabled machines.
        /// </summary>
        private void CheckIfExternalSynchronizationIsUsed()
        {
            int? taskId = Task.CurrentId;
            if (taskId == null)
            {
                string message = IO.Utilities.Format("Detected synchronization context " +
                    "that is not controlled by the P# runtime.");
                this.NotifyAssertionFailure(message, true);
            }

            if (!this.TaskMap.ContainsKey((int)taskId))
            {
                string message = IO.Utilities.Format($"Detected task with id '{taskId}' " +
                    "that is not controlled by the P# runtime.");
                this.NotifyAssertionFailure(message, true);
            }
        }

        /// <summary>
        /// Checks if the scheduling steps bound has been reached. If yes,
        /// it stops the scheduler and kills all enabled machines.
        /// </summary>
        private void CheckIfSchedulingStepsBoundIsReached()
        {
            if (this.Strategy.HasReachedMaxSchedulingSteps())
            {
                var msg = IO.Utilities.Format("Scheduling steps bound of {0} reached.",
                    this.Strategy.IsFair() ? this.Runtime.Configuration.MaxFairSchedulingSteps :
                    this.Runtime.Configuration.MaxUnfairSchedulingSteps);

                if (this.Runtime.Configuration.ConsiderDepthBoundHitAsBug)
                {
                    this.Runtime.Scheduler.NotifyAssertionFailure(msg, true);
                }
                else
                {
                    Debug.WriteLine($"<ScheduleDebug> {msg}");
                    this.Stop();
                }
            }
        }

        /// <summary>
        /// Kills any remaining machines at the end of the schedule.
        /// </summary>
        private void KillRemainingMachines()
        {
            foreach (var machineInfo in this.TaskMap.Values)
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
