//-----------------------------------------------------------------------
// <copyright file="LivenessChecker.cs">
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

using Microsoft.PSharp.TestingServices.Scheduling;
using Microsoft.PSharp.TestingServices.StateCaching;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices.Liveness
{
    /// <summary>
    /// Class implementing the P# liveness property checker.
    /// </summary>
    internal sealed class LivenessChecker : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The P# runtime.
        /// </summary>
        private PSharpBugFindingRuntime Runtime;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private List<Monitor> Monitors;

        /// <summary>
        /// The latest found potential cycle.
        /// </summary>
        private IList<Tuple<ScheduleStep, State>> PotentialCycle;

        /// <summary>
        /// Monitors that are stuck in the hot state
        /// for the duration of the latest found
        /// potential cycle.
        /// </summary>
        private ISet<Monitor> HotMonitors;

        /// <summary>
        /// The scheduling strategy installed with the
        /// P# bugfinder.
        /// </summary>
        private ISchedulingStrategy BugFindingSchedulingStrategy;

        /// <summary>
        /// A counter that increases in each step of the execution,
        /// as long as the P# program remains in the same cycle,
        /// with the liveness monitors at the hot state.
        /// </summary>
        private int LivenessTemperature;

        /// <summary>
        /// The index of the last scheduling step in
        /// the currently detected cycle.
        /// </summary>
        private int EndOfCycleIndex;

        /// <summary>
        /// The current cycle index.
        /// </summary>
        private int CurrentCycleIndex;

        /// <summary>
        /// Nondeterminitic seed.
        /// </summary>
        private int Seed;

        /// <summary>
        /// Randomizer.
        /// </summary>
        private Random Random;

        #endregion

        #region internal methods

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="runtime">PSharpBugFindingRuntime</param>
        /// <param name="bugFindingSchedulingStrategy">ISchedulingStrategy</param>
        internal LivenessChecker(PSharpBugFindingRuntime runtime,
            ISchedulingStrategy bugFindingSchedulingStrategy)
        {
            this.Runtime = runtime;

            this.Monitors = new List<Monitor>();
            this.PotentialCycle = new List<Tuple<ScheduleStep, State>>();
            this.HotMonitors = new HashSet<Monitor>();

            this.LivenessTemperature = 0;
            this.EndOfCycleIndex = 0;
            this.CurrentCycleIndex = 0;
            this.BugFindingSchedulingStrategy = bugFindingSchedulingStrategy;

            this.Seed = this.Runtime.Configuration.RandomSchedulingSeed ?? DateTime.Now.Millisecond;
            this.Random = new Random(this.Seed);
        }

        /// <summary>
        /// Registers a new monitor.
        /// </summary>
        /// <param name="monitor">Monitor</param>
        internal void RegisterMonitor(Monitor monitor)
        {
            this.Monitors.Add(monitor);
        }

        /// <summary>
        /// Checks for any liveness property violations. This method
        /// checks the liveness temperature of each monitor, and
        /// reports an error if one of the liveness monitors has
        /// passed the temperature threshold.
        /// </summary>
        internal void CheckLivenessAtShedulingStep()
        {
            if (this.Runtime.Configuration.CacheProgramState &&
                this.PotentialCycle.Count > 0)
            {
                var coldMonitors = this.HotMonitors.Where(m => m.IsInColdState()).ToList();
                if (coldMonitors.Count > 0)
                {
                    foreach (var coldMonitor in coldMonitors)
                    {
                        IO.Debug("<LivenessDebug> Trace is not reproducible: monitor " +
                            $"{coldMonitor.Id} transitioned to a cold state.");
                    }
                    Console.WriteLine("xxxxxxxx Escaped due to cold state");
                    this.EscapeCycle();
                    return;
                }

                var randomWalkScheduleTrace = this.Runtime.ScheduleTrace.Where(
                    val => val.Index > this.EndOfCycleIndex);
                foreach (var step in randomWalkScheduleTrace)
                {
                    State state = this.Runtime.StateCache[step];
                    if (!this.PotentialCycle.Any(val => val.Item2.Fingerprint.Equals(state.Fingerprint)))
                    {
                        state.PrettyPrint();
                        IO.Debug("<LivenessDebug> Detected a state that does not belong to the potential cycle.");
                        this.EscapeCycle();
                        return;
                    }
                }

                this.LivenessTemperature++;
                if (this.LivenessTemperature > this.Runtime.Configuration.LivenessTemperatureThreshold)
                {
                    foreach (var monitor in this.HotMonitors)
                    {
                        string message = IO.Format("Monitor '{0}' detected infinite execution that " +
                            "violates a liveness property. Cyle length : {1}", monitor.GetType().Name, PotentialCycle.Count);
                        this.Runtime.BugFinder.NotifyAssertionFailure(message, false);
                    }

                    this.Runtime.BugFinder.Stop();
                }
            }
            else if (!this.Runtime.Configuration.CacheProgramState &&
                this.BugFindingSchedulingStrategy.IsFair())
            {
                foreach (var monitor in this.Monitors)
                {
                    monitor.CheckLivenessTemperature();
                }
            }
        }

        /// <summary>
        /// Checks for any liveness property violations. Requires
        /// the P# program to have naturally terminated.
        /// </summary>
        internal void CheckLivenessAtTermination()
        {
            // Checks if the program has naturally terminated.
            if (!this.Runtime.BugFinder.HasFullyExploredSchedule)
            {
                return;
            }

            foreach (var monitor in this.Monitors)
            {
                var stateName = "";
                if (monitor.IsInHotState(out stateName))
                {
                    string message = IO.Format("Monitor '{0}' detected liveness bug " +
                        "in hot state '{1}' at the end of program execution.",
                        monitor.GetType().Name, stateName);
                    this.Runtime.BugFinder.NotifyAssertionFailure(message, false);
                }
            }
        }

        /// <summary>
        /// Checks liveness at a schedule trace cycle.
        /// </summary>
        /// <param name="root">Cycle start</param>
        internal void CheckLivenessAtTraceCycle(Fingerprint root)
        {
            // If there is a potential cycle found, do not create a new
            // one until the liveness checker has finished exploring the
            // current cycle.
            if (this.PotentialCycle.Count > 0)
            {
                return;
            }
            Console.WriteLine("******* Potential cycle **********");
            do
            {
                var scheduleStep = this.Runtime.ScheduleTrace.Pop();
                var state = this.Runtime.StateCache[scheduleStep];
                this.PotentialCycle.Insert(0, Tuple.Create(scheduleStep, state));

                IO.Debug("<LivenessDebug> Cycle contains {0} with {1}.",
                    scheduleStep.Type, state.Fingerprint.ToString());

                // The state can be safely removed, because the liveness detection
                // algorithm currently removes cycles, so a specific state can only
                // appear once in the schedule trace.
                this.Runtime.StateCache.Remove(scheduleStep);
            }
            while (this.Runtime.ScheduleTrace.Peek() != null && !this.Runtime.StateCache[
                this.Runtime.ScheduleTrace.Peek()].Fingerprint.Equals(root));

            var rt = Runtime.ScheduleTrace.Peek();
            var rts = Runtime.StateCache[Runtime.ScheduleTrace.Peek()];

            if (Runtime.Configuration.EnableDebugging)
            {
                IO.Debug("<LivenessDebug> ------------ SCHEDULE ------------.");
                foreach (var x in this.Runtime.ScheduleTrace)
                {
                    if (x.Type == ScheduleStepType.SchedulingChoice)
                    {
                        IO.Debug($"{x.Index} :: {x.Type} :: {x.ScheduledMachineId} :: {this.Runtime.StateCache[x].Fingerprint}");
                    }
                    else if (x.BooleanChoice != null)
                    {
                        IO.Debug($"{x.Index} :: {x.Type} :: {x.BooleanChoice.Value} :: {this.Runtime.StateCache[x].Fingerprint}");
                    }
                    else
                    {
                        IO.Debug($"{x.Index} :: {x.Type} :: {x.IntegerChoice.Value} :: {this.Runtime.StateCache[x].Fingerprint}");
                    }
                }
                IO.Debug("<LivenessDebug> ----------------------------------.");
                IO.Debug("<LivenessDebug> ------------- CYCLE --------------.");
                foreach (var x in this.PotentialCycle)
                {
                    if (x.Item1.Type == ScheduleStepType.SchedulingChoice)
                    {
                        IO.Debug($"{x.Item1.Index} :: {x.Item1.Type} :: {x.Item1.ScheduledMachineId}");
                    }
                    else if (x.Item1.BooleanChoice != null)
                    {
                        IO.Debug($"{x.Item1.Index} :: {x.Item1.Type} :: {x.Item1.BooleanChoice.Value}");
                    }
                    else
                    {
                        IO.Debug($"{x.Item1.Index} :: {x.Item1.Type} :: {x.Item1.IntegerChoice.Value}");
                    }

                    x.Item2.PrettyPrint();
                }
                IO.Debug("<LivenessDebug> ----------------------------------.");
            }

            if (!this.IsSchedulingFair(this.PotentialCycle))
            {
                IO.Debug("<LivenessDebug> Scheduling in cycle is unfair.");
                this.PotentialCycle.Clear();
                return;
            }
            else if (!this.IsNondeterminismFair(this.PotentialCycle))
            {
                IO.Debug("<LivenessDebug> Nondeterminism in cycle is unfair.");
                this.PotentialCycle.Clear();
                return;
            }

            IO.Debug("<LivenessDebug> Cycle execution is fair.");
            
            this.HotMonitors = this.GetHotMonitors(this.PotentialCycle);
            if (this.HotMonitors.Count > 0)
            {
                this.EndOfCycleIndex = this.PotentialCycle.Select(val => val.Item1).Min(val => val.Index);
                //this.Runtime.Configuration.LivenessTemperatureThreshold = PotentialCycle.Count * 100;
                Console.WriteLine("FAIR CYCLE FOUND!!");
                Console.WriteLine("<LivenessDebug> ------------- CYCLE --------------.");
                foreach (var x in this.PotentialCycle)
                {
                    if (x.Item1.Type == ScheduleStepType.SchedulingChoice)
                    {
                        Console.WriteLine($"{x.Item1.Index} :: {x.Item1.Type} :: {x.Item1.ScheduledMachineId}");
                    }
                    else if (x.Item1.BooleanChoice != null)
                    {
                        Console.WriteLine($"{x.Item1.Index} :: {x.Item1.Type} :: {x.Item1.BooleanChoice.Value}");
                    }
                    else
                    {
                        Console.WriteLine($"{x.Item1.Index} :: {x.Item1.Type} :: {x.Item1.IntegerChoice.Value}");
                    }
                }
                Console.WriteLine("Root: " + rt.Index + " " + rt.Type);
                Console.WriteLine("<LivenessDebug> ----------------------------------.");
                //PotentialCycle.Clear();
                this.Runtime.BugFinder.SwitchSchedulingStrategy(this);
            }
            else
            {
                this.PotentialCycle.Clear();
            }
        }

        /// <summary>
        /// Returns the monitor status.
        /// </summary>
        /// <returns>Monitor status</returns>
        internal Dictionary<Monitor, MonitorStatus> GetMonitorStatus()
        {
            var monitors = new Dictionary<Monitor, MonitorStatus>();
            foreach (var monitor in this.Monitors)
            {
                MonitorStatus status = MonitorStatus.None;
                if (monitor.IsInHotState())
                {
                    status = MonitorStatus.Hot;
                }
                else if (monitor.IsInColdState())
                {
                    status = MonitorStatus.Cold;
                }

                monitors.Add(monitor, status);
            }

            return monitors;
        }

        #endregion

        #region utility methods

        /// <summary>
        /// Checks if the scheduling is fair in a schedule trace cycle.
        /// </summary>
        /// <param name="cycle">Cycle of states</param>
        private bool IsSchedulingFair(IEnumerable<Tuple<ScheduleStep, State>> cycle)
        {
            var result = false;

            var enabledMachines = new HashSet<MachineId>();
            var scheduledMachines = new HashSet<MachineId>();

            var schedulingChoiceSteps= cycle.Where(
                val => val.Item1.Type == ScheduleStepType.SchedulingChoice);
            foreach (var step in schedulingChoiceSteps)
            {
                scheduledMachines.Add(step.Item1.ScheduledMachineId);
                enabledMachines.UnionWith(step.Item2.EnabledMachines);
            }

            foreach (var m in enabledMachines)
            {
                IO.Debug("<LivenessDebug> Enabled machine {0}.", m);
            }

            foreach (var m in scheduledMachines)
            {
                IO.Debug("<LivenessDebug> Scheduled machine {0}.", m);
            }

            if (enabledMachines.Count == scheduledMachines.Count)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Checks if the nondeterminism is fair in a schedule trace cycle.
        /// </summary>
        /// <param name="cycle">Cycle of states</param>
        //private bool IsNondeterminismFair(IEnumerable<Tuple<ScheduleStep, State>> cycle)
        //{
        //    var result = false;

        //    var trueChoices = new HashSet<string>();
        //    var falseChoices = new HashSet<string>();

        //    var fairNondeterministicChoiceSteps = cycle.Where(
        //        val => val.Item1.Type == ScheduleStepType.FairNondeterministicChoice &&
        //        val.Item1.BooleanChoice != null);

        //    foreach (var step in fairNondeterministicChoiceSteps)
        //    {
        //        if (step.Item1.BooleanChoice.Value)
        //        {
        //            trueChoices.Add(step.Item1.NondetId);
        //        }
        //        else
        //        {
        //            falseChoices.Add(step.Item1.NondetId);
        //        }
        //    }

        //    if (trueChoices.Count == falseChoices.Count)
        //    {
        //        result = true;
        //    }

        //    return result;
        //}

        private bool IsNondeterminismFair(IEnumerable<Tuple<ScheduleStep, State>> cycle)
        {
            var fairNondeterministicChoiceSteps = cycle.Where(
                val => val.Item1.Type == ScheduleStepType.FairNondeterministicChoice &&
                val.Item1.BooleanChoice != null);

            foreach (var step in fairNondeterministicChoiceSteps)
            {
                var choices = fairNondeterministicChoiceSteps.Where(c => c.Item1.NondetId == step.Item1.NondetId);
                var falseChoices = choices.Where(c => c.Item1.BooleanChoice == false).Count();
                if (choices.Count() == falseChoices)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets all monitors that are in hot state, but not in cold
        /// state during the schedule trace cycle.
        /// </summary>
        /// <param name="cycle">Cycle of states</param>
        /// <returns>Monitors</returns>
        private HashSet<Monitor> GetHotMonitors(IEnumerable<Tuple<ScheduleStep, State>> cycle)
        {
            var hotMonitors = new HashSet<Monitor>();

            foreach (var step in cycle)
            {
                foreach (var kvp in step.Item2.MonitorStatus)
                {
                    if (kvp.Value == MonitorStatus.Hot)
                    {
                        hotMonitors.Add(kvp.Key);
                    }
                }
            }

            if (hotMonitors.Count > 0)
            {
                foreach (var step in cycle)
                {
                    foreach (var kvp in step.Item2.MonitorStatus)
                    {
                        if (kvp.Value == MonitorStatus.Cold &&
                            hotMonitors.Contains(kvp.Key))
                        {
                            hotMonitors.Remove(kvp.Key);
                        }
                    }
                }
            }

            return hotMonitors;
        }

        /// <summary>
        /// Escapes the current cycle and continues to explore
        /// the schedule with the original scheduling strategy.
        /// </summary>
        private void EscapeCycle()
        {
            IO.Debug("<LivenessDebug> Escaping from unfair cycle.");

            this.PotentialCycle.Clear();
            this.HotMonitors.Clear();

            this.LivenessTemperature = 0;
            this.EndOfCycleIndex = 0;
            this.CurrentCycleIndex = 0;

            this.Runtime.BugFinder.SwitchSchedulingStrategy(
                this.BugFindingSchedulingStrategy);
        }

        #endregion

        #region scheduling strategy methods

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        bool ISchedulingStrategy.TryGetNext(out MachineInfo next, IEnumerable<MachineInfo> choices,
            MachineInfo current)
        {
            var availableMachines = choices.Where(
                m => m.IsEnabled && !m.IsBlocked && !m.IsWaitingToReceive).ToList();
            if (availableMachines.Count == 0)
            {
                availableMachines = choices.Where(m => m.IsWaitingToReceive).ToList();
                if (availableMachines.Count == 0)
                {
                    next = null;
                    return false;
                }
            }

            if (this.Runtime.Configuration.EnableCycleReplayingStrategy)
            {
                ScheduleStep nextStep = this.PotentialCycle[this.CurrentCycleIndex].Item1;
                if (nextStep.Type != ScheduleStepType.SchedulingChoice)
                {
                    IO.Debug("<LivenessDebug> Trace is not reproducible: next step is not a scheduling choice.");
                    Console.WriteLine("xxxxxxxxxxx Escaping - can't replay: " + nextStep.Type + " " + nextStep.Index);
                    this.EscapeCycle();
                    return this.BugFindingSchedulingStrategy.TryGetNext(out next, choices, current);
                }

                IO.Debug($"<LivenessDebug> Replaying '{nextStep.Index}' '{nextStep.ScheduledMachineId}'.");

                next = availableMachines.FirstOrDefault(m => m.Machine.Id.Type.Equals(
                    nextStep.ScheduledMachineId.Type) &&
                    m.Machine.Id.Value == nextStep.ScheduledMachineId.Value);
                if (next == null)
                {
                    IO.Debug("<LivenessDebug> Trace is not reproducible: cannot detect machine with type " +
                        $"'{nextStep.ScheduledMachineId.Type}' and id '{nextStep.ScheduledMachineId.Value}'.");
                    this.EscapeCycle();
                    return this.BugFindingSchedulingStrategy.TryGetNext(out next, choices, current);
                }

                this.CurrentCycleIndex++;
                if (this.CurrentCycleIndex == this.PotentialCycle.Count)
                {
                    this.CurrentCycleIndex = 0;
                }
            }
            else
            {
                int idx = this.Random.Next(availableMachines.Count);
                next = availableMachines[idx];
            }
            
            return true;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        bool ISchedulingStrategy.GetNextBooleanChoice(int maxValue, out bool next)
        {
            if (this.Runtime.Configuration.EnableCycleReplayingStrategy)
            {
                ScheduleStep nextStep = this.PotentialCycle[this.CurrentCycleIndex].Item1;
                if (nextStep.Type != ScheduleStepType.NondeterministicChoice ||
                    nextStep.BooleanChoice == null)
                {
                    IO.Debug("<LivenessDebug> Trace is not reproducible: next step is " +
                        "not a nondeterministic boolean choice.");
                    Console.WriteLine("xxxxxxxxxxx Escaping - boolean choice: " + nextStep.Type + " " + nextStep.Index);
                    this.EscapeCycle();
                    return this.BugFindingSchedulingStrategy.GetNextBooleanChoice(maxValue, out next);
                }

                IO.Debug($"<LivenessDebug> Replaying '{nextStep.Index}' '{nextStep.BooleanChoice.Value}'.");

                next = nextStep.BooleanChoice.Value;

                this.CurrentCycleIndex++;
                if (this.CurrentCycleIndex == this.PotentialCycle.Count)
                {
                    this.CurrentCycleIndex = 0;
                }
            }
            else
            {
                next = false;
                if (this.Random.Next(maxValue) == 0)
                {
                    next = true;
                }
            }
            
            return true;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        bool ISchedulingStrategy.GetNextIntegerChoice(int maxValue, out int next)
        {
            if (this.Runtime.Configuration.EnableCycleReplayingStrategy)
            {
                ScheduleStep nextStep = this.PotentialCycle[this.CurrentCycleIndex].Item1;
                if (nextStep.Type != ScheduleStepType.NondeterministicChoice ||
                    nextStep.IntegerChoice == null)
                {
                    IO.Debug("<LivenessDebug> Trace is not reproducible: next step is " +
                        "not a nondeterministic integer choice.");
                    this.EscapeCycle();
                    return this.BugFindingSchedulingStrategy.GetNextIntegerChoice(maxValue, out next);
                }

                IO.Debug($"<LivenessDebug> Replaying '{nextStep.Index}' '{nextStep.IntegerChoice.Value}'.");

                next = nextStep.IntegerChoice.Value;

                this.CurrentCycleIndex++;
                if (this.CurrentCycleIndex == this.PotentialCycle.Count)
                {
                    this.CurrentCycleIndex = 0;
                }
            }
            else
            {
                next = this.Random.Next(maxValue);
            }
            
            return true;
        }

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        int ISchedulingStrategy.GetExploredSteps()
        {
            return this.BugFindingSchedulingStrategy.GetExploredSteps();
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        bool ISchedulingStrategy.HasReachedMaxSchedulingSteps()
        {
            return false;
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean</returns>
        bool ISchedulingStrategy.HasFinished()
        {
            return false;
        }

        /// <summary>
        /// Checks if this a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        bool ISchedulingStrategy.IsFair()
        {
            return true;
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        void ISchedulingStrategy.ConfigureNextIteration()
        {
            this.CurrentCycleIndex = 0;
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        void ISchedulingStrategy.Reset()
        {
            this.CurrentCycleIndex = 0;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        string ISchedulingStrategy.GetDescription()
        {
            return this.BugFindingSchedulingStrategy.GetDescription();
        }

        #endregion
    }
}
