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
using System.Linq;
using Microsoft.TestingServices.SchedulingStrategies;

namespace Microsoft.PSharp.TestingServices.Scheduling
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
        private readonly Random Rand;

        /// <summary>
        /// A way for the ISchedulable to assert conditions.
        /// </summary>
        private readonly IAsserter Asserter;

        // TODO: implement the step limit.
        /// <summary>
        /// The step limit.
        /// </summary>
        private readonly int StepLimit;


        private int NumIterations;

        /// <summary>
        /// Creates the DPOR strategy.
        /// </summary>
        /// <param name="asserter"></param>
        /// <param name="rand"></param>
        /// <param name="stepLimit"></param>
        /// <param name="useSleepSets"></param>
        /// <param name="dpor"></param>
        public DPORStrategy(IAsserter asserter, Random rand = null, int stepLimit = -1, bool useSleepSets = true, bool dpor = true)
        {
            Asserter = asserter;
            Rand = rand;
            StepLimit = stepLimit;
            Stack = new Stack(rand, Asserter);
            Dpor = dpor ? new DPORAlgorithm(Asserter) : null;
            UseSleepSets = rand == null && useSleepSets;
            Reset();
        }

        #region Implementation of ISchedulingStrategy

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            int currentSchedulableId = (int) current.Id;
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

            bool added = Stack.Push(choices, currentSchedulableId);
            TidEntryList top = Stack.GetTop();

            if (added)
            {
                if (UseSleepSets)
                {
                    SleepSets.UpdateSleepSets(Stack, Asserter);
                }

                if (Dpor == null)
                {
                    top.SetAllEnabledToBeBacktracked(Asserter);
                }
                else if (Dpor.RaceReplaySuffix.Count > 0 && Dpor.ReplayRaceIndex < Dpor.RaceReplaySuffix.Count)
                {
                    // Replaying a race:
                    var tidReplay = Dpor.RaceReplaySuffix[Dpor.ReplayRaceIndex];
                    // Restore the nondet choices on the top of stack.
                    top.NondetChoices = tidReplay.NondetChoices;
                    // Add the replay tid to the backtrack set.
                    top.List[tidReplay.Id].Backtrack = true;
                    Asserter.Assert(
                        top.List[tidReplay.Id].Enabled 
                        || top.List[tidReplay.Id].OpType == OperationType.Yield);
                    ++Dpor.ReplayRaceIndex;
                }
                else
                {
                    // TODO: Here is where we can combine with another scheduler:
                    // For now, we just do round-robin when doing DPOR and random when doing random DPOR.
                    if (Rand == null)
                    {
                        top.AddFirstEnabledNotSleptToBacktrack(currentSchedulableId, Asserter);
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

            if (top.TryGetSelected(Asserter) != nextTid)
            {
                top.SetSelected(nextTid, Asserter);
            }
            Asserter.Assert(nextTid < choices.Count);
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

            Asserter.Assert(next.IsEnabled);
            return true;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            next = Stack.GetTop().MakeOrReplayNondetChoice(true, Rand, Asserter) == 1;
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
            return Rand != null || Stack.GetInternalSize() != 0;
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            Stack.Clear();
            NumIterations = 0;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return "DPOR";
        }

        #endregion
    }
}
