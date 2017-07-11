//-----------------------------------------------------------------------
// <copyright file="DPORStrategy.cs">
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

        // TODO: implement the step limit.
        /// <summary>
        /// The step limit.
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
        /// Creates the DPOR strategy.
        /// </summary>
        public DPORStrategy(
            IContract contract, 
            IRandomNumberGenerator rand = null, 
            int raceReversalIterationsLimit = -1, 
            int stepLimit = -1, 
            bool useSleepSets = true, 
            bool dpor = true)
        {
            Contract = contract;
            Rand = rand;
            StepLimit = stepLimit;
            Stack = new Stack(rand, Contract);
            Dpor = dpor ? new DPORAlgorithm(Contract) : null;
            UseSleepSets = rand == null && useSleepSets;
            RaceReversalIterationsLimit = raceReversalIterationsLimit;
            Reset();
        }

        /// <summary>
        /// Returns or forces the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
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
                AbdandonReplay(false);
            }

            bool added = Stack.Push(choices, currentSchedulableId);
            TidEntryList top = Stack.GetTop();

            Contract.Assert(next == null || added, "DPOR: Forced choice implies we should have added to stack.");

            if (added)
            {
                if (UseSleepSets)
                {
                    SleepSets.UpdateSleepSets(Stack, Contract);
                }

                if (Dpor == null)
                {
                    top.SetAllEnabledToBeBacktracked(Contract);
                }
                else if (Dpor.RaceReplaySuffix.Count > 0 && Dpor.ReplayRaceIndex < Dpor.RaceReplaySuffix.Count)
                {
                    // Replaying a race:
                    var tidReplay = Dpor.RaceReplaySuffix[Dpor.ReplayRaceIndex];
                    // Restore the nondet choices on the top of stack.
                    top.NondetChoices = tidReplay.NondetChoices;
                    // Add the replay tid to the backtrack set.
                    top.List[tidReplay.Id].Backtrack = true;
                    Contract.Assert(
                        top.List[tidReplay.Id].Enabled
                        || top.List[tidReplay.Id].OpType == OperationType.Yield);
                    ++Dpor.ReplayRaceIndex;
                }
                else
                {
                    // TODO: Here is where we can combine with another scheduler:
                    // For now, we just do round-robin when doing DPOR and random when doing random DPOR.

                    // If our choice is forced by parent scheduler:
                    if (next != null)
                    {
                        top.AddToBacktrack((int) next.Id, Contract);
                    }
                    else if (Rand == null)
                    {
                        top.AddFirstEnabledNotSleptToBacktrack(currentSchedulableId, Contract);
                    }
                    else
                    {
                        top.AddRandomEnabledNotSleptToBacktrack(Rand);
                    }
                }
            }
            else if (Rand != null)
            {
                // When doing random DPOR: we are replaying a schedule prefix so rewind the nondet choices now.
                top.RewindNondetChoicesForReplay();
            }

            int nextTid = Stack.GetSelectedOrFirstBacktrackNotSlept(currentSchedulableId);

            if (nextTid < 0)
            {
                next = null;
                // TODO: if nextTidIndex == DPORAlgorithm.SLEEP_SET_BLOCKED then let caller know that this is the case.
                // I.e. this is not deadlock.
                return false;
            }

            if (top.TryGetSelected(Contract) != nextTid)
            {
                top.SetSelected(nextTid, Contract);
            }

            Contract.Assert(nextTid < choices.Count);
            next = choices[nextTid];

            // TODO: Part of yield hack.
            if (!next.IsEnabled &&
                next.NextOperationType == OperationType.Yield)
            {
                //                // Uncomment to avoid waking a yielding thread.
                //                next = null;
                //                // TODO: let caller know that this is not deadlock.
                //                return false;
                next.IsEnabled = true;
            }

            Contract.Assert(next.IsEnabled);
            return true;
        }

        /// <summary>
        /// Returns or forces the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        private bool GetNextBooleanChoiceHelper(int maxValue, ref bool? next)
        {
            if (next != null)
            {
                AbdandonReplay(true);
                return true;
            }

            next = Stack.GetTop().MakeOrReplayNondetChoice(true, Rand, Contract) == 1;
            return true;
        }

        /// <summary>
        /// Abandon the replay of a schedule prefix and/or a race suffice.
        /// </summary>
        private void AbdandonReplay(bool clearNonDet)
        {
            Contract.Assert(Rand != null, "DPOR: Forced choices are only supported with random DPOR.");
            // Abandon remaining stack entries and race replay.
            if (clearNonDet)
            {
                Stack.GetTop().ClearNondetChoicesFromNext();
            }
            Stack.ClearAboveTop();
            Dpor.ReplayRaceIndex = Int32.MaxValue;
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            next = null;
            return GetNextHelper(ref next, choices, current);
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            bool? nextTemp = null;
            GetNextBooleanChoiceHelper(maxValue, ref nextTemp);
            Contract.Assert(nextTemp != null);
            next = nextTemp.Value;
            return true;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            // TODO: 
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Forces the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public void ForceNext(ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            ISchedulable temp = next;
            bool res = GetNextHelper(ref temp, choices, current);
            Contract.Assert(res, "DPOR scheduler refused to schedule a forced choice.");
            Contract.Assert(temp == next, "DPOR scheduler changed forced next choice.");
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            bool? nextTemp = next;
            bool res = GetNextBooleanChoiceHelper(maxValue, ref nextTemp);
            Contract.Assert(res, "DPOR scheduler refused to schedule a forced boolean choice.");
            Contract.Assert(
                nextTemp.HasValue && nextTemp.Value == next, 
                "DPOR scheduler changed forced next boolean choice.");
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetScheduledSteps()
        {
            return Stack.GetNumSteps();
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            return StepLimit > 0 && Stack.GetNumSteps() >= StepLimit;
        }

        /// <summary>
        /// Checks if this a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            return false;
        }

        /// <summary>
        /// Prepares the next scheduling iteration.
        /// </summary>
        /// <returns>False if all schedules have been explored</returns>
        public bool PrepareForNextIteration()
        {
            ++NumIterations;
            Dpor?.DoDPOR(Stack, Rand);

            Stack.PrepareForNextSchedule();

            if (Rand != null && RaceReversalIterationsLimit >= 0)
            {
                ++NumRaceReversalIterationsCounter;
                if (NumRaceReversalIterationsCounter >= RaceReversalIterationsLimit)
                {
                    NumRaceReversalIterationsCounter = 0;
                    AbdandonReplay(false);
                }
            }

            return Rand != null || Stack.GetInternalSize() != 0;
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            Stack.Clear();
            NumIterations = 0;
            NumRaceReversalIterationsCounter = 0;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return "DPOR";
        }
    }
}
