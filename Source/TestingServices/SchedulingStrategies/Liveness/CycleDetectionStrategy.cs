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

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.IO;
using Microsoft.PSharp.TestingServices.SchedulingStrategies;
using Microsoft.PSharp.TestingServices.StateCaching;
using Microsoft.PSharp.TestingServices.Tracing.Schedule;

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
        private readonly StateCache StateCache;

        /// <summary>
        /// The schedule trace of the program.
        /// </summary>
        private readonly ScheduleTrace ScheduleTrace;

        /// <summary>
        /// Monitors that are stuck in the hot state
        /// for the duration of the latest found
        /// potential cycle.
        /// </summary>
        private HashSet<Monitor> HotMonitors;

        /// <summary>
        /// The latest found potential cycle.
        /// </summary>
        private readonly List<ScheduleStep> PotentialCycle;

        /// <summary>
        /// Fingerprints captured in the latest potential cycle.
        /// </summary>
        private readonly HashSet<Fingerprint> PotentialCycleFingerprints;

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
        private readonly int Seed;

        /// <summary>
        /// Randomizer.
        /// </summary>
        private readonly IRandomNumberGenerator Random;

        /// <summary>
        /// Map of fingerprints to schedule step indexes.
        /// </summary>
        private readonly Dictionary<Fingerprint, List<int>> FingerprintIndexMap;

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
            PotentialCycle = new List<ScheduleStep>();
            PotentialCycleFingerprints = new HashSet<Fingerprint>();

            LivenessTemperature = 0;
            EndOfCycleIndex = 0;
            CurrentCycleIndex = 0;

            Seed = Configuration.RandomSchedulingSeed ?? DateTime.Now.Millisecond;
            Random = new DefaultRandomNumberGenerator(Seed);

            FingerprintIndexMap = new Dictionary<Fingerprint, List<int>>();
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
            CaptureAndCheckProgramState();

            if (IsReplayingCycle)
            {
                var enabledChoices = choices.Where(choice => choice.IsEnabled).ToList();
                if (enabledChoices.Count == 0)
                {
                    next = null;
                    return false;
                }

                ScheduleStep nextStep = PotentialCycle[CurrentCycleIndex];
                if (nextStep.Type != ScheduleStepType.SchedulingChoice)
                {
                    Debug.WriteLine("<LivenessDebug> Trace is not reproducible: next step is not a scheduling choice.");
                    EscapeUnfairCycle();
                    return SchedulingStrategy.GetNext(out next, choices, current);
                }

                Debug.WriteLine("<LivenessDebug> Replaying '{0}' '{1}'.", nextStep.Index, nextStep.ScheduledMachineId);

                next = enabledChoices.FirstOrDefault(choice => choice.Id == nextStep.ScheduledMachineId);
                if (next == null)
                {
                    Debug.WriteLine("<LivenessDebug> Trace is not reproducible: cannot detect machine with id '{0}'.", nextStep.ScheduledMachineId);
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
            CaptureAndCheckProgramState();

            if (IsReplayingCycle)
            {
                ScheduleStep nextStep = PotentialCycle[CurrentCycleIndex];
                if ((nextStep.Type == ScheduleStepType.SchedulingChoice) || nextStep.BooleanChoice == null)
                {
                    Debug.WriteLine("<LivenessDebug> Trace is not reproducible: next step is not a nondeterministic boolean choice.");
                    EscapeUnfairCycle();
                    return SchedulingStrategy.GetNextBooleanChoice(maxValue, out next);
                }

                Debug.WriteLine("<LivenessDebug> Replaying '{0}' '{1}'.", nextStep.Index, nextStep.BooleanChoice.Value);

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
            CaptureAndCheckProgramState();

            if (IsReplayingCycle)
            {
                ScheduleStep nextStep = PotentialCycle[CurrentCycleIndex];
                if (nextStep.Type != ScheduleStepType.NondeterministicChoice ||
                    nextStep.IntegerChoice == null)
                {
                    Debug.WriteLine("<LivenessDebug> Trace is not reproducible: next step is not a nondeterministic integer choice.");
                    EscapeUnfairCycle();
                    return SchedulingStrategy.GetNextIntegerChoice(maxValue, out next);
                }

                Debug.WriteLine("<LivenessDebug> Replaying '{0}' '{1}'.", nextStep.Index, nextStep.IntegerChoice.Value);

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
        /// Captures the program state and checks for liveness violations.
        /// </summary>
        private void CaptureAndCheckProgramState()
        {
            if (this.ScheduleTrace.Count == 0)
            {
                return;
            }

            if (Configuration.SafetyPrefixBound <= GetScheduledSteps())
            {
                State capturedState;
                Fingerprint fingerprint;
                bool stateExists = StateCache.CaptureState(out capturedState, out fingerprint,
                    FingerprintIndexMap, ScheduleTrace.Peek(), Monitors);
                if (stateExists)
                {
                    Debug.WriteLine("<LivenessDebug> Detected potential infinite execution.");
                    CheckLivenessAtTraceCycle(FingerprintIndexMap[fingerprint]);
                }
            }

            if (PotentialCycle.Count > 0)
            {
                // Only check for a liveness property violation
                // if there is a potential cycle.
                CheckLivenessTemperature();
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
                if (Debug.IsEnabled)
                {
                    foreach (var coldMonitor in coldMonitors)
                    {
                        Debug.WriteLine("<LivenessDebug> Trace is not reproducible: monitor " +
                            "{0} transitioned to a cold state.", coldMonitor.Id);
                    }
                }

                EscapeUnfairCycle();
                return;
            }

            var randomWalkScheduleTrace = ScheduleTrace.Where(val => val.Index > EndOfCycleIndex);
            foreach (var step in randomWalkScheduleTrace)
            {
                State state = step.State;
                if (!PotentialCycleFingerprints.Contains(state.Fingerprint))
                {
                    if (Debug.IsEnabled)
                    {
                        state.PrettyPrint();
                        Debug.WriteLine("<LivenessDebug> Detected a state that does not belong to the potential cycle.");
                    }

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
        /// <param name="indices">Indices corresponding to the fingerprint of root</param>
        private void CheckLivenessAtTraceCycle(List<int> indices)
        {
            // If there is a potential cycle found, do not create a new one until the
            // liveness checker has finished exploring the current cycle.
            if (PotentialCycle.Count > 0)
            {
                return;
            }

            var checkIndexRand = indices[indices.Count - 2];
            var index = ScheduleTrace.Count - 1;

            for (int i = checkIndexRand + 1; i <= index; i++)
            {
                var scheduleStep = ScheduleTrace[i];
                PotentialCycle.Add(scheduleStep);
                PotentialCycleFingerprints.Add(scheduleStep.State.Fingerprint);
                Debug.WriteLine("<LivenessDebug> Cycle contains {0} with {1}.",
                    scheduleStep.Type, scheduleStep.State.Fingerprint.ToString());
            }

            DebugPrintScheduleTrace();
            DebugPrintPotentialCycle();

            if (!IsSchedulingFair(PotentialCycle))
            {
                Debug.WriteLine("<LivenessDebug> Scheduling in cycle is unfair.");
                PotentialCycle.Clear();
                PotentialCycleFingerprints.Clear();
            }
            else if (!IsNondeterminismFair(PotentialCycle))
            {
                Debug.WriteLine("<LivenessDebug> Nondeterminism in cycle is unfair.");
                PotentialCycle.Clear();
                PotentialCycleFingerprints.Clear();
            }

            if (PotentialCycle.Count == 0)
            {
                bool isFairCycleFound = false;
                int counter = Math.Min(indices.Count - 1, 3);
                while (counter > 0)
                {
                    var randInd = Random.Next(indices.Count - 2);
                    checkIndexRand = indices[randInd];

                    index = ScheduleTrace.Count - 1;
                    for (int i = checkIndexRand + 1; i <= index; i++)
                    {
                        var scheduleStep = ScheduleTrace[i];
                        PotentialCycle.Add(scheduleStep);
                        PotentialCycleFingerprints.Add(scheduleStep.State.Fingerprint);
                        Debug.WriteLine("<LivenessDebug> Cycle contains {0} with {1}.",
                            scheduleStep.Type, scheduleStep.State.Fingerprint.ToString());
                    }

                    if (IsSchedulingFair(PotentialCycle) && IsNondeterminismFair(PotentialCycle))
                    {
                        isFairCycleFound = true;
                        break;
                    }
                    else
                    {
                        PotentialCycle.Clear();
                        PotentialCycleFingerprints.Clear();
                    }

                    counter--;
                }

                if (!isFairCycleFound)
                {
                    PotentialCycle.Clear();
                    PotentialCycleFingerprints.Clear();
                    return;
                }
            }

            Debug.WriteLine("<LivenessDebug> Cycle execution is fair.");

            HotMonitors = GetHotMonitors(PotentialCycle);
            if (HotMonitors.Count > 0)
            {
                EndOfCycleIndex = PotentialCycle.Select(val => val).Min(val => val.Index);
                Configuration.LivenessTemperatureThreshold = 10 * PotentialCycle.Count;
                IsReplayingCycle = true;
            }
            else
            {
                PotentialCycle.Clear();
                PotentialCycleFingerprints.Clear();
            }
        }

        /// <summary>
        /// Checks if the scheduling is fair in a schedule trace cycle.
        /// </summary>
        /// <param name="cycle">Cycle of states</param>
        private static bool IsSchedulingFair(List<ScheduleStep> cycle)
        {
            var result = false;

            var enabledMachines = new HashSet<ulong>();
            var scheduledMachines = new HashSet<ulong>();

            var schedulingChoiceSteps = cycle.Where(
                val => val.Type == ScheduleStepType.SchedulingChoice);
            foreach (var step in schedulingChoiceSteps)
            {
                scheduledMachines.Add(step.ScheduledMachineId);
            }

            foreach (var step in cycle)
            {
                enabledMachines.UnionWith(step.State.EnabledMachineIds);
            }

            if (Debug.IsEnabled)
            {
                foreach (var m in enabledMachines)
                {
                    Debug.WriteLine("<LivenessDebug> Enabled machine {0}.", m);
                }

                foreach (var m in scheduledMachines)
                {
                    Debug.WriteLine("<LivenessDebug> Scheduled machine {0}.", m);
                }
            }

            if (enabledMachines.Count == scheduledMachines.Count)
            {
                result = true;
            }

            return result;
        }

        private static bool IsNondeterminismFair(List<ScheduleStep> cycle)
        {
            var fairNondeterministicChoiceSteps = cycle.Where(
                val => val.Type == (ScheduleStepType.FairNondeterministicChoice) &&
                val.BooleanChoice != null).ToList();
            foreach (var step in fairNondeterministicChoiceSteps)
            {
                var choices = fairNondeterministicChoiceSteps.Where(c => c.NondetId.Equals(step.NondetId)).ToList();
                var falseChoices = choices.Count(c => c.BooleanChoice == false);
                var trueChoices = choices.Count(c => c.BooleanChoice == true);
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
        private static HashSet<Monitor> GetHotMonitors(List<ScheduleStep> cycle)
        {
            var hotMonitors = new HashSet<Monitor>();

            foreach (var step in cycle)
            {
                foreach (var kvp in step.State.MonitorStatus)
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
                    foreach (var kvp in step.State.MonitorStatus)
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

            HotMonitors.Clear();
            PotentialCycle.Clear();
            PotentialCycleFingerprints.Clear();

            LivenessTemperature = 0;
            EndOfCycleIndex = 0;
            CurrentCycleIndex = 0;

            IsReplayingCycle = false;
        }

        /// <summary>
        /// Prints the program schedule trace. Works only
        /// if debug mode is enabled.
        /// </summary>
        private void DebugPrintScheduleTrace()
        {
            if (Debug.IsEnabled)
            {
                Debug.WriteLine("<LivenessDebug> ------------ SCHEDULE ------------.");

                foreach (var step in ScheduleTrace)
                {
                    if (step.Type == ScheduleStepType.SchedulingChoice)
                    {
                        Debug.WriteLine($"{step.Index} :: {step.Type} :: {step.ScheduledMachineId} :: {step.State.Fingerprint}");
                    }
                    else if (step.BooleanChoice != null)
                    {
                        Debug.WriteLine($"{step.Index} :: {step.Type} :: {step.BooleanChoice.Value} :: {step.State.Fingerprint}");
                    }
                    else
                    {
                        Debug.WriteLine($"{step.Index} :: {step.Type} :: {step.IntegerChoice.Value} :: {step.State.Fingerprint}");
                    }
                }

                Debug.WriteLine("<LivenessDebug> ----------------------------------.");
            }
        }

        /// <summary>
        /// Prints the potential cycle. Works only if
        /// debug mode is enabled.
        /// </summary>
        private void DebugPrintPotentialCycle()
        {
            if (Debug.IsEnabled)
            {
                Debug.WriteLine("<LivenessDebug> ------------- CYCLE --------------.");

                foreach (var step in PotentialCycle)
                {
                    if (step.Type == ScheduleStepType.SchedulingChoice)
                    {
                        Debug.WriteLine($"{step.Index} :: {step.Type} :: {step.ScheduledMachineId}");
                    }
                    else if (step.BooleanChoice != null)
                    {
                        Debug.WriteLine($"{step.Index} :: {step.Type} :: {step.BooleanChoice.Value}");
                    }
                    else
                    {
                        Debug.WriteLine($"{step.Index} :: {step.Type} :: {step.IntegerChoice.Value}");
                    }

                    step.State.PrettyPrint();
                }

                Debug.WriteLine("<LivenessDebug> ----------------------------------.");
            }
        }
    }
}
