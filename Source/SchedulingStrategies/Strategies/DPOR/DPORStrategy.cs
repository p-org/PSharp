// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;

using Microsoft.PSharp.TestingServices.SchedulingStrategies.DPOR;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// Dynamic partial-order reduction (DPOR) scheduling strategy.
    /// In fact, this uses the Source DPOR algorithm.
    /// </summary>
    public class DPORStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The stack datastructure used to perform the
        /// depth-first search.
        /// </summary>
        private readonly Stack Stack;

        /// <summary>
        /// The actual DPOR algorithm implementation.
        /// </summary>
        private readonly DPORAlgorithm Dpor;

        /// <summary>
        /// Whether to use sleep sets.
        /// See <see cref="SleepSets"/>.
        /// </summary>
        private readonly bool UseSleepSets;

        /// <summary>
        /// If non-null, we perform random DPOR
        /// using this RNG.
        /// </summary>
        private readonly IRandomNumberGenerator Rand;

        /// <summary>
        /// A way for the ISchedulable to assert conditions.
        /// </summary>
        private readonly IContract Contract;

        /// <summary>
        /// The step limit.
        /// TODO: implement the step limit.
        /// </summary>
        private readonly int StepLimit;

        /// <summary>
        /// When doing random DPOR, we do an initial execution
        /// and then try to reverse races.
        /// This int specifies how many iterations of race reversing to perform
        /// before performing a new initial iteration.
        /// </summary>
        private readonly int RaceReversalIterationsLimit;

        /// <summary>
        /// Counter for <see cref="RaceReversalIterationsLimit"/>.
        /// </summary>
        private int NumRaceReversalIterationsCounter;

        /// <summary>
        /// Number of iterations.
        /// </summary>
        private int NumIterations;

        /// <summary>
        /// Initializes a new instance of the <see cref="DPORStrategy"/> class.
        /// </summary>
        public DPORStrategy(
            IContract contract,
            IRandomNumberGenerator rand = null,
            int raceReversalIterationsLimit = -1,
            int stepLimit = -1,
            bool useSleepSets = true,
            bool dpor = true)
        {
            this.Contract = contract;
            this.Rand = rand;
            this.StepLimit = stepLimit;
            this.Stack = new Stack(rand, this.Contract);
            this.Dpor = dpor ? new DPORAlgorithm(this.Contract) : null;
            this.UseSleepSets = rand is null && useSleepSets;
            this.RaceReversalIterationsLimit = raceReversalIterationsLimit;
            this.Reset();
        }

        /// <summary>
        /// Returns or forces the next choice to schedule.
        /// </summary>
        private bool GetNextHelper(
            ref ISchedulable next,
            List<ISchedulable> choices,
            ISchedulable current)
        {
            int currentSchedulableId = (int)current.Id;

            // "Yield" and "Waiting for quiescence" hack.
            if (choices.TrueForAll(info => !info.IsEnabled))
            {
                if (choices.Exists(info => info.NextOperationType == OperationType.Yield))
                {
                    foreach (var actorInfo in choices)
                    {
                        if (actorInfo.NextOperationType == OperationType.Yield)
                        {
                            actorInfo.IsEnabled = true;
                        }
                    }
                }
                else if (choices.Exists(
                    info => info.NextOperationType == OperationType.WaitForQuiescence))
                {
                    foreach (var actorInfo in choices)
                    {
                        if (actorInfo.NextOperationType == OperationType.WaitForQuiescence)
                        {
                            actorInfo.IsEnabled = true;
                        }
                    }
                }
            }

            // Forced choice.
            if (next != null)
            {
                this.AbdandonReplay(false);
            }

            bool added = this.Stack.Push(choices);
            ThreadEntryList top = this.Stack.GetTop();

            this.Contract.Assert(next is null || added, "DPOR: Forced choice implies we should have added to stack.");

            if (added)
            {
                if (this.UseSleepSets)
                {
                    SleepSets.UpdateSleepSets(this.Stack, this.Contract);
                }

                if (this.Dpor is null)
                {
                    top.SetAllEnabledToBeBacktracked(this.Contract);
                }
                else if (this.Dpor.RaceReplaySuffix.Count > 0 && this.Dpor.ReplayRaceIndex < this.Dpor.RaceReplaySuffix.Count)
                {
                    // Replaying a race:
                    var tidReplay = this.Dpor.RaceReplaySuffix[this.Dpor.ReplayRaceIndex];

                    // Restore the nondet choices on the top of stack.
                    top.NondetChoices = tidReplay.NondetChoices;

                    // Add the replay tid to the backtrack set.
                    top.List[tidReplay.Id].Backtrack = true;
                    this.Contract.Assert(
                        top.List[tidReplay.Id].Enabled
                        || top.List[tidReplay.Id].OpType == OperationType.Yield);
                    ++this.Dpor.ReplayRaceIndex;
                }
                else
                {
                    // TODO: Here is where we can combine with another scheduler:
                    // For now, we just do round-robin when doing DPOR and random when doing random DPOR.

                    // If our choice is forced by parent scheduler:
                    if (next != null)
                    {
                        top.AddToBacktrack((int)next.Id, this.Contract);
                    }
                    else if (this.Rand is null)
                    {
                        top.AddFirstEnabledNotSleptToBacktrack(currentSchedulableId, this.Contract);
                    }
                    else
                    {
                        top.AddRandomEnabledNotSleptToBacktrack(this.Rand);
                    }
                }
            }
            else if (this.Rand != null)
            {
                // When doing random DPOR: we are replaying a schedule prefix so rewind the nondet choices now.
                top.RewindNondetChoicesForReplay();
            }

            int nextTid = this.Stack.GetSelectedOrFirstBacktrackNotSlept(currentSchedulableId);

            if (nextTid < 0)
            {
                next = null;

                // TODO: if nextTidIndex == DPORAlgorithm.SLEEP_SET_BLOCKED then let caller know that this is the case.
                // I.e. this is not deadlock.
                return false;
            }

            if (top.TryGetSelected() != nextTid)
            {
                top.SetSelected(nextTid, this.Contract);
            }

            this.Contract.Assert(nextTid < choices.Count);
            next = choices[nextTid];

            // TODO: Part of yield hack.
            if (!next.IsEnabled &&
                next.NextOperationType == OperationType.Yield)
            {
                // Uncomment to avoid waking a yielding thread.
                // next = null;
                // TODO: let caller know that this is not deadlock.
                // return false;
                next.IsEnabled = true;
            }

            this.Contract.Assert(next.IsEnabled);
            return true;
        }

        /// <summary>
        /// Returns or forces the next boolean choice.
        /// </summary>
        private bool GetNextBooleanChoiceHelper(ref bool? next)
        {
            if (next != null)
            {
                this.AbdandonReplay(true);
                return true;
            }

            next = this.Stack.GetTop().MakeOrReplayNondetChoice(true, this.Rand, this.Contract) == 1;
            return true;
        }

        /// <summary>
        /// Returns or forces the next integer choice.
        /// </summary>
        private bool GetNextIntegerChoiceHelper(ref int? next)
        {
            if (next != null)
            {
                this.AbdandonReplay(true);
                return true;
            }

            next = this.Stack.GetTop().MakeOrReplayNondetChoice(false, this.Rand, this.Contract);
            return true;
        }

        /// <summary>
        /// Abandon the replay of a schedule prefix and/or a race suffice.
        /// </summary>
        private void AbdandonReplay(bool clearNonDet)
        {
            this.Contract.Assert(this.Rand != null, "DPOR: Forced choices are only supported with random DPOR.");

            // Abandon remaining stack entries and race replay.
            if (clearNonDet)
            {
                this.Stack.GetTop().ClearNondetChoicesFromNext();
            }

            this.Stack.ClearAboveTop();
            this.Dpor.ReplayRaceIndex = int.MaxValue;
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        public bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            next = null;
            return this.GetNextHelper(ref next, choices, current);
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            bool? nextTemp = null;
            this.GetNextBooleanChoiceHelper(ref nextTemp);
            this.Contract.Assert(nextTemp != null);
            next = nextTemp.Value;
            return true;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            int? nextTemp = null;
            this.GetNextIntegerChoiceHelper(ref nextTemp);
            this.Contract.Assert(nextTemp != null);
            next = nextTemp.Value;
            return true;
        }

        /// <summary>
        /// Forces the next entity to be scheduled.
        /// </summary>
        public void ForceNext(ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            ISchedulable temp = next;
            bool res = this.GetNextHelper(ref temp, choices, current);
            this.Contract.Assert(res, "DPOR scheduler refused to schedule a forced choice.");
            this.Contract.Assert(temp == next, "DPOR scheduler changed forced next choice.");
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            bool? nextTemp = next;
            bool res = this.GetNextBooleanChoiceHelper(ref nextTemp);
            this.Contract.Assert(res, "DPOR scheduler refused to schedule a forced boolean choice.");
            this.Contract.Assert(
                nextTemp.HasValue && nextTemp.Value == next,
                "DPOR scheduler changed forced next boolean choice.");
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            int? nextTemp = next;
            bool res = this.GetNextIntegerChoiceHelper(ref nextTemp);
            this.Contract.Assert(res, "DPOR scheduler refused to schedule a forced integer choice.");
            this.Contract.Assert(
                nextTemp.HasValue && nextTemp.Value == next,
                "DPOR scheduler changed forced next integer choice.");
        }

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        public int GetScheduledSteps() => this.Stack.GetNumSteps();

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        public bool HasReachedMaxSchedulingSteps() => this.StepLimit > 0 && this.Stack.GetNumSteps() >= this.StepLimit;

        /// <summary>
        /// Checks if this a fair scheduling strategy.
        /// </summary>
        public bool IsFair() => false;

        /// <summary>
        /// Prepares the next scheduling iteration.
        /// </summary>
        public bool PrepareForNextIteration()
        {
            ++this.NumIterations;
            this.Dpor?.DoDPOR(this.Stack, this.Rand);

            this.Stack.PrepareForNextSchedule();

            if (this.Rand != null && this.RaceReversalIterationsLimit >= 0)
            {
                ++this.NumRaceReversalIterationsCounter;
                if (this.NumRaceReversalIterationsCounter >= this.RaceReversalIterationsLimit)
                {
                    this.NumRaceReversalIterationsCounter = 0;
                    this.AbdandonReplay(false);
                }
            }

            return this.Rand != null || this.Stack.GetInternalSize() != 0;
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.Stack.Clear();
            this.NumIterations = 0;
            this.NumRaceReversalIterationsCounter = 0;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public string GetDescription() => "DPOR";
    }
}
