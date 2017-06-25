//-----------------------------------------------------------------------
// <copyright file="CycleDetectionStrategy.cs">
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

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.StateCaching;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;
using Microsoft.PSharp.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Strategy for detecting liveness property violations using partial state-caching
    /// and cycle-replaying. It contains a nested <see cref="ISchedulingStrategy"/> that
    /// is used for scheduling decisions. Note that liveness property violations are
    /// checked only if the nested strategy is fair.
    /// </summary>
    internal sealed class CycleDetectionStrategy : LivenessCheckingStrategy
    {
        /// <summary>
        /// The state cache of the program.
        /// </summary>
        internal StateCache StateCache;

        /// <summary>
        /// The schedule trace of the program.
        /// </summary>
        internal ScheduleTrace ScheduleTrace;

        /// <summary>
        /// Monitors that are stuck in the hot state
        /// for the duration of the latest found
        /// potential cycle.
        /// </summary>
        private ISet<Monitor> HotMonitors;

        /// <summary>
        /// The latest found potential cycle.
        /// </summary>
        private IList<Tuple<ScheduleStep, State>> PotentialCycle;

        /// <summary>
        /// Is strategy trying to replay a potential cycle.
        /// </summary>
        private bool IsReplayingCycle;

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
        private IRandomNumberGenerator Random;

        /// <summary>
        /// Creates a liveness strategy that checks the specific monitors
        /// for liveness property violations, and uses the specified
        /// strategy for scheduling decisions.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="cache">StateCache</param>
        /// <param name="trace">ScheduleTrace</param>
        /// <param name="monitors">List of monitors</param>
        /// <param name="strategy">ISchedulingStrategy</param>
        internal CycleDetectionStrategy(Configuration configuration, StateCache cache, ScheduleTrace trace,
            List<Monitor> monitors, ISchedulingStrategy strategy)
            : base(configuration, monitors, strategy)
        {
            StateCache = cache;
            ScheduleTrace = trace;

            HotMonitors = new HashSet<Monitor>();
            PotentialCycle = new List<Tuple<ScheduleStep, State>>();

            LivenessTemperature = 0;
            EndOfCycleIndex = 0;
            CurrentCycleIndex = 0;

            Seed = Configuration.RandomSchedulingSeed ?? DateTime.Now.Millisecond;
            Random = new DefaultRandomNumberGenerator(Seed);
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public override bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            if (IsReplayingCycle)
            {
                var enabledChoices = choices.Where(choice => choice.IsEnabled).ToList();
                if (enabledChoices.Count == 0)
                {
                    next = null;
                    return false;
                }

                ScheduleStep nextStep = PotentialCycle[CurrentCycleIndex].Item1;
                if (nextStep.Type != ScheduleStepType.SchedulingChoice)
                {
                    Debug.WriteLine("<LivenessDebug> Trace is not reproducible: next step is not a scheduling choice.");
                    EscapeUnfairCycle();
                    return SchedulingStrategy.GetNext(out next, choices, current);
                }

                Debug.WriteLine($"<LivenessDebug> Replaying '{nextStep.Index}' '{nextStep.ScheduledMachineId}'.");

                next = enabledChoices.FirstOrDefault(choice => choice.Id == nextStep.ScheduledMachineId);
                if (next == null)
                {
                    Debug.WriteLine($"<LivenessDebug> Trace is not reproducible: cannot detect machine with id '{nextStep.ScheduledMachineId}'.");
                    EscapeUnfairCycle();
                    return SchedulingStrategy.GetNext(out next, choices, current);
                }

                CurrentCycleIndex++;
                if (CurrentCycleIndex == PotentialCycle.Count)
                {
                    CurrentCycleIndex = 0;
                }

                return true;
            }
            else
            {
                return SchedulingStrategy.GetNext(out next, choices, current);
            }
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public override bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            if (IsReplayingCycle)
            {
                ScheduleStep nextStep = PotentialCycle[CurrentCycleIndex].Item1;
                if ((nextStep.Type == ScheduleStepType.SchedulingChoice) || nextStep.BooleanChoice == null)
                {
                    Debug.WriteLine("<LivenessDebug> Trace is not reproducible: next step is " +
                        "not a nondeterministic boolean choice.");
                    EscapeUnfairCycle();
                    return SchedulingStrategy.GetNextBooleanChoice(maxValue, out next);
                }

                Debug.WriteLine($"<LivenessDebug> Replaying '{nextStep.Index}' '{nextStep.BooleanChoice.Value}'.");

                next = nextStep.BooleanChoice.Value;

                CurrentCycleIndex++;
                if (CurrentCycleIndex == PotentialCycle.Count)
                {
                    CurrentCycleIndex = 0;
                }

                return true;
            }
            else
            {
                return SchedulingStrategy.GetNextBooleanChoice(maxValue, out next);
            }
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public override bool GetNextIntegerChoice(int maxValue, out int next)
        {
            if (IsReplayingCycle)
            {
                ScheduleStep nextStep = PotentialCycle[CurrentCycleIndex].Item1;
                if (nextStep.Type != ScheduleStepType.NondeterministicChoice ||
                    nextStep.IntegerChoice == null)
                {
                    Debug.WriteLine("<LivenessDebug> Trace is not reproducible: next step is " +
                        "not a nondeterministic integer choice.");
                    EscapeUnfairCycle();
                    return SchedulingStrategy.GetNextIntegerChoice(maxValue, out next);
                }

                Debug.WriteLine($"<LivenessDebug> Replaying '{nextStep.Index}' '{nextStep.IntegerChoice.Value}'.");

                next = nextStep.IntegerChoice.Value;

                CurrentCycleIndex++;
                if (CurrentCycleIndex == PotentialCycle.Count)
                {
                    CurrentCycleIndex = 0;
                }

                return true;
            }
            else
            {
                return SchedulingStrategy.GetNextIntegerChoice(maxValue, out next);
            }
        }

        /// <summary>
        /// Prepares for the next scheduling choice. This is invoked
        /// directly after a scheduling choice has been chosen, and
        /// can be used to invoke specialised post-choice actions.
        /// </summary>
        public override void PrepareForNextChoice()
        {
            CaptureProgramState();

            if (PotentialCycle.Count > 0)
            {
                // Only check for a liveness property violation
                // if there is a potential cycle.
                CheckLivenessTemperature();
            }
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration</returns>
        public override bool PrepareForNextIteration()
        {
            if (IsReplayingCycle)
            {
                CurrentCycleIndex = 0;
                return true;
            }
            else
            {
                return base.PrepareForNextIteration();
            }
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public override void Reset()
        {
            if (IsReplayingCycle)
            {
                CurrentCycleIndex = 0;
            }
            else
            {
                base.Reset();
            }
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public override bool HasReachedMaxSchedulingSteps()
        {
            if (IsReplayingCycle)
            {
                return false;
            }
            else
            {
                return base.HasReachedMaxSchedulingSteps();
            }
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public override bool IsFair()
        {
            if (IsReplayingCycle)
            {
                return true;
            }
            else
            {
                return base.IsFair();
            }
        }

        /// <summary>
        /// Captures the current program state.
        /// </summary>
        private void CaptureProgramState()
        {
            if (Configuration.SafetyPrefixBound <= GetExploredSteps())
            {
                State capturedState = null;
                bool stateExists = StateCache.CaptureState(out capturedState, ScheduleTrace.Peek(), Monitors);
                if (stateExists)
                {
                    Debug.WriteLine("<LivenessDebug> Detected potential infinite execution.");
                    CheckLivenessAtTraceCycle(capturedState.Fingerprint);
                }
            }
        }

        /// <summary>
        /// Checks the liveness temperature of each monitor, and
        /// reports an error if one of the liveness monitors has
        /// passed the temperature threshold.
        /// </summary>
        private void CheckLivenessTemperature()
        {
            var coldMonitors = HotMonitors.Where(m => m.IsInColdState()).ToList();
            if (coldMonitors.Count > 0)
            {
                foreach (var coldMonitor in coldMonitors)
                {
                    Debug.WriteLine("<LivenessDebug> Trace is not reproducible: monitor " +
                        $"{coldMonitor.Id} transitioned to a cold state.");
                }

                EscapeUnfairCycle();
                return;
            }

            var randomWalkScheduleTrace = ScheduleTrace.Where(val => val.Index > EndOfCycleIndex);
            foreach (var step in randomWalkScheduleTrace)
            {
                State state = StateCache[step];
                if (!PotentialCycle.Any(val => val.Item2.Fingerprint.Equals(state.Fingerprint)))
                {
                    state.PrettyPrint();
                    Debug.WriteLine("<LivenessDebug> Detected a state that does not belong to the potential cycle.");
                    EscapeUnfairCycle();
                    return;
                }
            }

            //// Increments the temperature of each monitor.
            //foreach (var monitor in HotMonitors)
            //{
            //    string message = IO.Utilities.Format("Monitor '{0}' detected infinite execution that " +
            //        "violates a liveness property.", monitor.GetType().Name);
            //    Runtime.Scheduler.NotifyAssertionFailure(message, false);
            //}

            LivenessTemperature++;
            if (LivenessTemperature > Configuration.LivenessTemperatureThreshold)
            {
                foreach (var monitor in HotMonitors)
                {
                    monitor.CheckLivenessTemperature(LivenessTemperature);
                }

                //foreach (var monitor in HotMonitors)
                //{
                //    string message = IO.Utilities.Format("Monitor '{0}' detected infinite execution that " +
                //        "violates a liveness property.", monitor.GetType().Name);
                //    Runtime.Scheduler.NotifyAssertionFailure(message, false);
                //}

                //Runtime.Scheduler.Stop();
            }
        }

        /// <summary>
        /// Checks liveness at a schedule trace cycle.
        /// </summary>
        /// <param name="root">Cycle start</param>
        internal void CheckLivenessAtTraceCycle(Fingerprint root)
        {
            // If there is a potential cycle found, do not create a new one until the
            // liveness checker has finished exploring the current cycle.
            if (PotentialCycle.Count > 0)
            {
                return;
            }

            List<int> checkIndex = new List<int>();
            for (int i = ScheduleTrace.Count - 1; i >= 0; i--)
            {
                if (ScheduleTrace.Peek().Equals(ScheduleTrace[i]))
                {
                    continue;
                }

                if (StateCache[ScheduleTrace[i]].Fingerprint.Equals(root))
                {
                    checkIndex.Add(ScheduleTrace[i].Index);
                }
            }

            var checkIndexRand = checkIndex.First();
            var index = ScheduleTrace.Count - 1;

            do
            {
                var scheduleStep = ScheduleTrace[index];
                index--;
                var state = StateCache[scheduleStep];
                PotentialCycle.Insert(0, Tuple.Create(scheduleStep, state));

                Debug.WriteLine("<LivenessDebug> Cycle contains {0} with {1}.",
                    scheduleStep.Type, state.Fingerprint.ToString());
            }
            while (index > 0 && ScheduleTrace[index] != null &&
                ScheduleTrace[index].Index != checkIndexRand);

            if (Configuration.EnableDebugging)
            {
                Debug.WriteLine("<LivenessDebug> ------------ SCHEDULE ------------.");

                foreach (var x in ScheduleTrace)
                {
                    if (x.Type == ScheduleStepType.SchedulingChoice)
                    {
                        Debug.WriteLine($"{x.Index} :: {x.Type} :: {x.ScheduledMachineId} :: {StateCache[x].Fingerprint}");
                    }
                    else if (x.BooleanChoice != null)
                    {
                        Debug.WriteLine($"{x.Index} :: {x.Type} :: {x.BooleanChoice.Value} :: {StateCache[x].Fingerprint}");
                    }
                    else
                    {
                        Debug.WriteLine($"{x.Index} :: {x.Type} :: {x.IntegerChoice.Value} :: {StateCache[x].Fingerprint}");
                    }
                }

                Debug.WriteLine("<LivenessDebug> ----------------------------------.");
                Debug.WriteLine("<LivenessDebug> ------------- CYCLE --------------.");

                foreach (var x in PotentialCycle)
                {
                    if (x.Item1.Type == ScheduleStepType.SchedulingChoice)
                    {
                        Debug.WriteLine($"{x.Item1.Index} :: {x.Item1.Type} :: {x.Item1.ScheduledMachineId}");
                    }
                    else if (x.Item1.BooleanChoice != null)
                    {
                        Debug.WriteLine($"{x.Item1.Index} :: {x.Item1.Type} :: {x.Item1.BooleanChoice.Value}");
                    }
                    else
                    {
                        Debug.WriteLine($"{x.Item1.Index} :: {x.Item1.Type} :: {x.Item1.IntegerChoice.Value}");
                    }

                    x.Item2.PrettyPrint();
                }

                Debug.WriteLine("<LivenessDebug> ----------------------------------.");
            }

            if (!IsSchedulingFair(PotentialCycle))
            {
                Debug.WriteLine("<LivenessDebug> Scheduling in cycle is unfair.");
                PotentialCycle.Clear();
            }
            else if (!IsNondeterminismFair(PotentialCycle))
            {
                Debug.WriteLine("<LivenessDebug> Nondeterminism in cycle is unfair.");
                PotentialCycle.Clear();
            }

            if (PotentialCycle.Count == 0)
            {
                bool isFairCycleFound = false;
                int counter = Math.Min(checkIndex.Count, 5);
                while (!isFairCycleFound && counter > 0)
                {
                    var randInd = Random.Next(checkIndex.Count - 1);
                    checkIndexRand = checkIndex[randInd];

                    index = ScheduleTrace.Count - 1;
                    do
                    {
                        var scheduleStep = ScheduleTrace[index];
                        index--;
                        var state = StateCache[scheduleStep];
                        PotentialCycle.Insert(0, Tuple.Create(scheduleStep, state));

                        Debug.WriteLine("<LivenessDebug> Cycle contains {0} with {1}.",
                            scheduleStep.Type, state.Fingerprint.ToString());
                    }
                    while (index > 0 && ScheduleTrace[index] != null &&
                        ScheduleTrace[index].Index != checkIndexRand);

                    if (IsSchedulingFair(PotentialCycle) && IsNondeterminismFair(PotentialCycle))
                    {
                        isFairCycleFound = true;
                        break;
                    }
                    else
                    {
                        PotentialCycle.Clear();
                    }

                    counter--;
                }

                if (!isFairCycleFound)
                {
                    Debug.WriteLine("<LivenessDebug> Scheduling in cycle is unfair.");
                    PotentialCycle.Clear();
                    return;
                }
            }

            Debug.WriteLine("<LivenessDebug> Cycle execution is fair.");

            HotMonitors = GetHotMonitors(PotentialCycle);
            if (HotMonitors.Count > 0)
            {
                EndOfCycleIndex = PotentialCycle.Select(val => val.Item1).Min(val => val.Index);
                Configuration.LivenessTemperatureThreshold = 10 * PotentialCycle.Count;
                IsReplayingCycle = true;
            }
            else
            {
                PotentialCycle.Clear();
            }
        }

        /// <summary>
        /// Checks if the scheduling is fair in a schedule trace cycle.
        /// </summary>
        /// <param name="cycle">Cycle of states</param>
        private bool IsSchedulingFair(IEnumerable<Tuple<ScheduleStep, State>> cycle)
        {
            var result = false;

            var enabledMachines = new HashSet<ulong>();
            var scheduledMachines = new HashSet<ulong>();

            var schedulingChoiceSteps = cycle.Where(
                val => val.Item1.Type == ScheduleStepType.SchedulingChoice);
            foreach (var step in schedulingChoiceSteps)
            {
                scheduledMachines.Add(step.Item1.ScheduledMachineId);
            }

            foreach (var state in cycle)
            {
                enabledMachines.UnionWith(state.Item2.EnabledMachineIds);
            }

            foreach (var m in enabledMachines)
            {
                Debug.WriteLine("<LivenessDebug> Enabled machine {0}.", m);
            }

            foreach (var m in scheduledMachines)
            {
                Debug.WriteLine("<LivenessDebug> Scheduled machine {0}.", m);
            }

            if (enabledMachines.Count == scheduledMachines.Count)
            {
                result = true;
            }

            return result;
        }

        private bool IsNondeterminismFair(IEnumerable<Tuple<ScheduleStep, State>> cycle)
        {
            var fairNondeterministicChoiceSteps = cycle.Where(
                val => val.Item1.Type == (ScheduleStepType.FairNondeterministicChoice) &&
                val.Item1.BooleanChoice != null);
            foreach (var step in fairNondeterministicChoiceSteps)
            {
                var choices = fairNondeterministicChoiceSteps.Where(c => c.Item1.NondetId.Equals(step.Item1.NondetId));
                var falseChoices = choices.Where(c => c.Item1.BooleanChoice == false).Count();
                var trueChoices = choices.Where(c => c.Item1.BooleanChoice == true).Count();
                if (trueChoices == 0 || falseChoices == 0)
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
        /// Escapes the unfair cycle and continues to explore the
        /// schedule with the original scheduling strategy.
        /// </summary>
        private void EscapeUnfairCycle()
        {
            Debug.WriteLine("<LivenessDebug> Escaped from unfair cycle.");

            PotentialCycle.Clear();
            HotMonitors.Clear();

            LivenessTemperature = 0;
            EndOfCycleIndex = 0;
            CurrentCycleIndex = 0;

            IsReplayingCycle = false;
        }
    }
}
