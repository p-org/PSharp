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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.PSharp.IO;
using System;

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
        /// Map from unique ids to schedulable infos.
        /// </summary>
        private Dictionary<ulong, SchedulableInfo> Infos;
        
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
        /// The currently schedulable info.
        /// </summary>
        internal SchedulableInfo ScheduledMachine { get; private set; }

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
            this.Infos = new Dictionary<ulong, SchedulableInfo>();
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

            if (this.Runtime.Configuration.CacheProgramState &&
                this.Runtime.Configuration.SafetyPrefixBound <= this.ExploredSteps &&
                this.Runtime.ScheduleTrace.Count > 0)
            {
                this.Runtime.StateCache.CaptureState(this.Runtime.ScheduleTrace.Peek());
            }

            SchedulableInfo current = this.ScheduledMachine;
            ISchedulable next = null;

            var choices = this.Infos.Values.OrderBy(choice => choice.Id).Select(choice => choice as ISchedulable).ToList();
            if (!this.Strategy.TryGetNext(out next, choices, current))
            {
                // Checks if the program has livelocked.
                this.CheckIfProgramHasLivelocked(choices.Select(choice => choice as SchedulableInfo));

                Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                this.HasFullyExploredSchedule = true;
                this.Stop();
            }

            this.ScheduledMachine = next as SchedulableInfo;

            this.Runtime.ScheduleTrace.AddSchedulingChoice(next.Id);
            this.ScheduledMachine.ProgramCounter = 0;
            
            // Checks the liveness monitors for potential liveness bugs.
            this.Runtime.LivenessChecker.CheckLivenessAtShedulingStep();

            Debug.WriteLine($"<ScheduleDebug> Schedule '{next.Name}' with task id '{this.ScheduledMachine.TaskId}'.");

            if (current != next)
            {
                current.IsActive = false;
                lock (next)
                {
                    this.ScheduledMachine.IsActive = true;
                    System.Threading.Monitor.PulseAll(next);
                }
                
                lock (current)
                {
                    if (current.IsCompleted)
                    {
                        return;
                    }

                    while (!current.IsActive)
                    {
                        Debug.WriteLine($"<ScheduleDebug> Sleep '{current.Name}' with task id '{current.TaskId}'.");
                        System.Threading.Monitor.Wait(current);
                        Debug.WriteLine($"<ScheduleDebug> Wake up '{current.Name}' with task id '{current.TaskId}'.");
                    }

                    if (!current.IsEnabled)
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
        internal bool GetNextNondeterministicBooleanChoice(int maxValue, string uniqueId = null)
        {
            // Checks if synchronisation not controlled by P# was used.
            this.CheckIfExternalSynchronizationIsUsed();

            // Checks if the scheduling steps bound has been reached.
            this.CheckIfSchedulingStepsBoundIsReached();

            if (this.Runtime.Configuration.CacheProgramState &&
                this.Runtime.Configuration.SafetyPrefixBound <= this.ExploredSteps &&
                this.Runtime.ScheduleTrace.Count > 0)
            {
                this.Runtime.StateCache.CaptureState(this.Runtime.ScheduleTrace.Peek());
            }

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

            foreach (var m in Infos.Values)
            {
                if (m.IsActive)
                {
                    m.ProgramCounter++;
                    break;
                }
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


            if (this.Runtime.Configuration.CacheProgramState &&
                this.Runtime.Configuration.SafetyPrefixBound <= this.ExploredSteps &&
                this.Runtime.ScheduleTrace.Count > 0)
            {
                this.Runtime.StateCache.CaptureState(this.Runtime.ScheduleTrace.Peek());
            }

            var choice = 0;
            if (!this.Strategy.GetNextIntegerChoice(maxValue, out choice))
            {
                Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                this.Stop();
            }

            this.Runtime.ScheduleTrace.AddNondeterministicIntegerChoice(choice);

            //this.Runtime.GetProgramStatePrint();
            // Checks the liveness monitors for potential liveness bugs.
            this.Runtime.LivenessChecker.CheckLivenessAtShedulingStep();

            return choice;
        }

        /// <summary>
        /// Waits for the event handler to start.
        /// </summary>
        /// <param name="info">SchedulableInfo</param>
        internal void WaitForEventHandlerToStart(SchedulableInfo info)
        {
            lock (info)
            {
                if (this.Infos.Count == 1)
                {
                    info.IsActive = true;
                    System.Threading.Monitor.PulseAll(info);
                }
                else
                {
                    while (!info.HasStarted)
                    {
                        System.Threading.Monitor.Wait(info);
                    }
                }
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

        #region notifications

        /// <summary>
        /// Notify that an event handler has been created.
        /// </summary>
        /// <param name="info">SchedulableInfo</param>
        internal void NotifyEventHandlerCreated(SchedulableInfo info)
        {
            if (!this.Infos.ContainsKey(info.Id))
            {
                if (this.Infos.Count == 0)
                {
                    this.ScheduledMachine = info;
                }

                this.Infos.Add(info.Id, info);
            }

            Debug.WriteLine($"<ScheduleDebug> Created event handler of '{info.Name}' with task id '{info.TaskId}'.");
        }

        /// <summary>
        /// Notify that the event handler has started.
        /// </summary>
        /// <param name="info">SchedulableInfo</param>
        internal void NotifyEventHandlerStarted(SchedulableInfo info)
        {
            Debug.WriteLine($"<ScheduleDebug> Started event handler of '{info.Name}' with task id '{info.TaskId}'.");

            lock (info)
            {
                info.HasStarted = true;
                System.Threading.Monitor.PulseAll(info);
                while (!info.IsActive)
                {
                    Debug.WriteLine($"<ScheduleDebug> Sleep '{info.Name}' with task id '{info.TaskId}'.");
                    System.Threading.Monitor.Wait(info);
                    Debug.WriteLine($"<ScheduleDebug> Wake up '{info.Name}' with task id '{info.TaskId}'.");
                }

                if (!info.IsEnabled)
                {
                    throw new ExecutionCanceledException();
                }
            }
        }

        /// <summary>
        /// Notify that the event handler has completed.
        /// </summary>
        /// <param name="info">SchedulableInfo</param>
        internal void NotifyEventHandlerCompleted(SchedulableInfo info)
        {
            Debug.WriteLine($"<ScheduleDebug> Completed event handler of '{info.Name}' with task id '{info.TaskId}'.");

            info.IsEnabled = false;
            info.IsCompleted = true;

            this.Schedule();

            Debug.WriteLine($"<ScheduleDebug> Exit event handler of '{info.Name}' with task id '{info.TaskId}'.");
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

                this.Runtime.Logger.WriteLine($"<ErrorLog> {text}");
                this.Runtime.Logger.WriteLine("<StrategyLog> Found bug using " +
                    $"'{this.Runtime.Configuration.SchedulingStrategy}' strategy.");

                if (this.Strategy.GetDescription().Length > 0)
                {
                    this.Runtime.Logger.WriteLine($"<StrategyLog> {this.Strategy.GetDescription()}");
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

        #endregion

        #region utilities

        /// <summary>
        /// Returns the enabled schedulable ids.
        /// </summary>
        /// <returns>Enabled machine ids</returns>
        internal HashSet<ulong> GetEnabledSchedulableIds()
        {
            var enabledSchedulableIds = new HashSet<ulong>();
            foreach (var machineInfo in this.Infos.Values)
            {
                if (machineInfo.IsEnabled)
                {
                    enabledSchedulableIds.Add(machineInfo.Id);
                }
            }

            return enabledSchedulableIds;
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
                report.LassoLength += this.Runtime.LivenessChecker.GetLassoLength();
                report.BugTraceLength += this.Strategy.GetExploredSteps() - report.LassoLength;
                report.MinBugTraceLength = Math.Min(report.MinBugTraceLength, report.BugTraceLength);
                report.MaxBugTraceLength = Math.Max(report.MaxBugTraceLength, report.BugTraceLength);

                report.MinLassoLength = Math.Min(report.MinLassoLength, report.LassoLength);
                report.MaxLassoLength = Math.Max(report.MaxLassoLength, report.LassoLength);
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
            var availableMachines = this.Infos.Values.Where(choice => choice.IsEnabled).ToList();
            return availableMachines.Count;
        }

        /// <summary>
        /// Checks for a livelock. This happens when there are no more enabled
        /// machines, but there is one or more non-enabled machines that are
        /// waiting to receive an event.
        /// </summary>
        /// <param name="choices">Infos</param>
        private void CheckIfProgramHasLivelocked(IEnumerable<SchedulableInfo> choices)
        {
            var blockedChoices = choices.Where(choice => choice.IsWaitingToReceive).ToList();
            if (blockedChoices.Count > 0)
            {
                string message = "Livelock detected.";
                for (int i = 0; i < blockedChoices.Count; i++)
                {
                    message += IO.Utilities.Format($" '{blockedChoices[i].Name}'");
                    if (i == blockedChoices.Count - 2)
                    {
                        message += " and";
                    }
                    else if (i < blockedChoices.Count - 1)
                    {
                        message += ",";
                    }
                }

                message += blockedChoices.Count == 1 ? " is " : " are ";
                message += "waiting for an event, but no other schedulable choices are enabled.";
                this.Runtime.Scheduler.NotifyAssertionFailure(message, true);
            }
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

            if (this.ScheduledMachine.TaskId != taskId.Value)
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
            foreach (var machineInfo in this.Infos.Values)
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
