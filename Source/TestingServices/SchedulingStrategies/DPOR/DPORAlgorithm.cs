//-----------------------------------------------------------------------
// <copyright file="DPORAlgorithm.cs">
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

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// The actual DPOR algorithm used by <see cref="DPORStrategy"/>.
    /// </summary>
    public class DPORAlgorithm
    {
        private int NumThreads;
        private int NumSteps;
        
        private int[] ThreadIdToLastOpIndex; 
        private int[] TargetIdToLastCreateStartEnd;
        private int[] TargetIdToLastSend;
        private int[] Vcs;

        private readonly IAsserter Asserter;

        private readonly List<Race> Races;

        /// <summary>
        /// When performing randomized DPOR,
        /// this field gives the schedule (as a list of thread ids)
        /// that should be followed in order to reverse a randomly chosen race.
        /// </summary>
        public readonly List<int> RaceReplaySuffix;

        /// <summary>
        /// An index for the <see cref="RaceReplaySuffix"/> (for convenience) to be used 
        /// when replaying a reversed race.
        /// </summary>
        public int ReplayRaceIndex;

        private readonly List<int> MissingThreadIds;

        /// <summary>
        /// Construct the DPOR algorithm.
        /// </summary>
        public DPORAlgorithm(IAsserter asserter)
        {
            Asserter = asserter;
            // initial estimates
            NumThreads = 4;
            NumSteps = 1 << 8;

            ThreadIdToLastOpIndex = new int[NumThreads];
            TargetIdToLastCreateStartEnd = new int[NumThreads];
            TargetIdToLastSend = new int[NumThreads];
            Vcs = new int[NumSteps * NumThreads];
            Races = new List<Race>(NumSteps);
            RaceReplaySuffix = new List<int>();
            MissingThreadIds = new List<int>();
            ReplayRaceIndex = 0;
        }


        private void FromVCSetVC(int from, int to)
        {
            int fromI = (from-1) * NumThreads;
            int toI = (to-1) * NumThreads;
            for (int i = 0; i < NumThreads; ++i)
            {
                Vcs[toI] = Vcs[fromI];
                ++fromI;
                ++toI;
            }
        }

        private void ForVCSetClockToValue(int vc, int clock, int value)
        {
            Vcs[(vc - 1) * NumThreads + clock] = value;
        }

        private int ForVCGetClock(int vc, int clock)
        {
            return Vcs[(vc - 1) * NumThreads + clock];
        }

        private void ForVCJoinFromVC(int to, int from)
        {
            int fromI = (from - 1) * NumThreads;
            int toI = (to - 1) * NumThreads;
            for (int i = 0; i < NumThreads; ++i)
            {
                if (Vcs[fromI] > Vcs[toI])
                {
                    Vcs[toI] = Vcs[fromI];
                }
                ++fromI;
                ++toI;
            }
        }

        private void ClearVC(int vc)
        {
            int vcI = (vc - 1) * NumThreads;
            for (int i = 0; i < NumThreads; ++i)
            {
                Vcs[vcI] = 0;
                ++vcI;
            }
        }

        private int[] GetVC(int vc)
        {
            int[] res = new int[NumThreads];
            int fromI = (vc - 1) * NumThreads;
            for (int i = 0; i < NumThreads; ++i)
            {
                res[i] = Vcs[fromI];
                ++fromI;
            }
            return res;
        }

        private bool HB(Stack stack, int vc1, int vc2)
        {
            // A hb B
            // iff:
            // A's index <= B.VC[A's tid]

            TidEntry aStep = GetSelectedTidEntry(stack, vc1);

            return vc1 <= ForVCGetClock(vc2, aStep.Id);
        }


        private TidEntry GetSelectedTidEntry(Stack stack, int index)
        {
            var list = GetThreadsAt(stack, index);
            return list.List[list.GetSelected(Asserter)];
        }

        /// <summary>
        /// Checks if two operations are reversible.
        /// Assumes both operations passed in are dependent.
        /// </summary>
        /// <param name="stack">Stack</param>
        /// <param name="index1"></param>
        /// <param name="index2"></param>
        /// <returns>Are the operations reversible?</returns>
        private bool Reversible(Stack stack, int index1, int index2)
        {
            var step1 = GetSelectedTidEntry(stack, index1);
            var step2 = GetSelectedTidEntry(stack, index2);
            return step1.OpType == OperationType.Send &&
                   step2.OpType == OperationType.Send;
        }

        private static TidEntryList GetThreadsAt(Stack stack, int index)
        {
            return stack.StackInternal[(int)index - 1];
        }


        /// <summary>
        /// The main entry point to the DPOR algorithm.
        /// </summary>
        /// <param name="stack">Should contain a terminal schedule.</param>
        /// <param name="rand">If non-null, then a randomized DPOR algorithm will be used.</param>
        public void DoDPOR(Stack stack, Random rand)
        {
            UpdateFieldsAndRealocateDatastructuresIfNeeded(stack);
            
            // Indexes start at 1.

            for (int i = 1; i < NumSteps; ++i)
            {
                TidEntry step = GetSelectedTidEntry(stack, i);
                if (ThreadIdToLastOpIndex[step.Id] > 0)
                {
                    FromVCSetVC(ThreadIdToLastOpIndex[step.Id], i);
                }
                else
                {
                    ClearVC(i);
                }
                ForVCSetClockToValue(i, (int) step.Id, i);
                ThreadIdToLastOpIndex[step.Id] = i;

                int targetId = step.TargetId;
                if (step.OpType == OperationType.Create && i + 1 < NumSteps)
                {
                    targetId = GetThreadsAt(stack, i).List.Count;
                }

                if (targetId < 0)
                {
                    continue;
                }

                int lastAccessIndex = 0;

                switch (step.OpType)
                {
                    case OperationType.Start:
                    case OperationType.Stop:
                    case OperationType.Create:
                    case OperationType.Join:
                    {
                        lastAccessIndex =
                            TargetIdToLastCreateStartEnd[targetId];
                        TargetIdToLastCreateStartEnd[targetId] = i;
                        break;
                    }
                    case OperationType.Send:
                    {
                        lastAccessIndex = TargetIdToLastSend[targetId];
                        TargetIdToLastSend[targetId] = i;
                        break;
                    }
                    case OperationType.Receive:
                    {
                        lastAccessIndex = (int) step.SendStepIndex;
                        break;
                    }
                    case OperationType.WaitForQuiescence:
                        for (int j = 0; j < ThreadIdToLastOpIndex.Length; j++)
                        {
                            if (j == step.Id || ThreadIdToLastOpIndex[j] == 0)
                            {
                                continue;
                            }
                            ForVCJoinFromVC(i, ThreadIdToLastOpIndex[j]);
                        }
                        // Continue. No backtrack.
                        continue;

                    case OperationType.Yield:
                        // Nothing.
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                    
                if (lastAccessIndex > 0)
                {
                    AddBacktrack(stack, lastAccessIndex, i, step);

                    ForVCJoinFromVC(i, lastAccessIndex);
                }
            }

            if (rand != null)
            {
                DoRandomRaceReverse(stack, rand);
            }

        }

        private void DoRandomRaceReverse(Stack stack, Random rand)
        {
            int raceIndex = rand.Next(Races.Count);
            Race race = Races[raceIndex];

            Asserter.Assert(RaceReplaySuffix.Count == 0, "Tried to reverse race but replay suffix was not empty!");

            // Add to RaceReplaySuffix: all steps between a and b that do not h.a. a.
            for (int i = race.A; i < race.B; ++i)
            {
                if (HB(stack, (int) race.A, (int) i))
                {
                    // Skip it.
                    // But track the missing thread id if this is a create operation.
                    if (GetSelectedTidEntry(stack, (int) i).OpType == OperationType.Create)
                    {
                        var missingThreadId = GetThreadsAt(stack, (int) i).List.Count;
                        var index = MissingThreadIds.BinarySearch(missingThreadId);
                        // We should not find it.
                        Asserter.Assert(index < 0);
                        // Get the next largest index (see BinarySearch).
                        index = ~index;
                        // Insert before the next largest item.
                        MissingThreadIds.Insert(index, missingThreadId);
                    }
                }
                else
                {
                    // Add thread id to the RaceReplaySuffix, but adjust
                    // it for missing thread ids.
                    AddThreadIdToRaceReplaySuffix(GetSelectedTidEntry(stack, (int) i).Id);
                }
            }

            AddThreadIdToRaceReplaySuffix(GetSelectedTidEntry(stack, (int) race.B).Id);
            AddThreadIdToRaceReplaySuffix(GetSelectedTidEntry(stack, (int) race.A).Id);

            // Remove steps from a onwards. Indexes start at one so we must subtract 1.
            stack.StackInternal.RemoveRange(race.A - 1, stack.StackInternal.Count - (race.A - 1));

        }

        private void AddThreadIdToRaceReplaySuffix(int threadId)
        {
            // Add thread id to the RaceReplaySuffix, but adjust
            // it for missing thread ids.

            var index = MissingThreadIds.BinarySearch(threadId);
            // Make it so index is the number of missing thread ids before and including threadId.
            // e.g. if missingThreadIds = [3,6,9]
            // 3 => index + 1  = 1
            // 4 => ~index     = 1
            if (index >= 0)
            {
                index += 1;
            }
            else
            {
                index = ~index;
            }

            RaceReplaySuffix.Add(threadId - index);
        }

        private void AddBacktrack(Stack stack,
            int lastAccessIndex,
            int i,
            TidEntry step)
        {
            var aTidEntries = GetThreadsAt(stack, lastAccessIndex);
            var a = GetSelectedTidEntry(stack, lastAccessIndex);
            if (HB(stack, lastAccessIndex, i) ||
                !Reversible(stack, lastAccessIndex, i)) return;

            Races.Add(new Race((int)lastAccessIndex, (int)i));

            // PSEUDOCODE: Find candidates.
            //  There is a race between `a` and `b`.
            //  Must find first steps after `a` that do not HA `a`
            //  (except maybe b.tid) and do not HA each other.
            // candidates = {}
            // if b.tid is enabled before a:
            //   add b.tid to candidates
            // lookingFor = set of enabled threads before a - a.tid - b.tid.
            // let vc = [0,0,...]
            // vc[a.tid] = a;
            // for k = aIndex+1 to bIndex:
            //   if lookingFor does not contain k.tid:
            //     continue
            //   remove k.tid from lookingFor
            //   doesHaAnother = false
            //   foreach t in tids:
            //     if vc[t] hb k:
            //       doesHaAnother = true
            //       break
            //   vc[k.tid] = k
            //   if !doesHaAnother:
            //     add k.tid to candidates
            //   if lookingFor is empty:
            //     break


            var candidateThreadIds = new HashSet<int>();
            
            if (aTidEntries.List.Count > step.Id && 
                (aTidEntries.List[step.Id].Enabled || aTidEntries.List[step.Id].OpType == OperationType.Yield))
            {
                candidateThreadIds.Add((int) step.Id);
            }
            var lookingFor = new HashSet<int>();
            for (int j = 0; j < aTidEntries.List.Count; ++j)
            {
                if (j != a.Id &&
                    j != step.Id &&
                    (aTidEntries.List[(int) j].Enabled || aTidEntries.List[(int)j].OpType == OperationType.Yield))
                {
                    lookingFor.Add(j);
                }
            }

            int[] vc = new int[NumThreads];
            vc[a.Id] = lastAccessIndex;
            if (lookingFor.Count > 0)
            {
                for (int k = lastAccessIndex + 1; k < i; ++k)
                {
                    var kEntry = GetSelectedTidEntry(stack, k);
                    if (!lookingFor.Contains((int) kEntry.Id)) continue;

                    lookingFor.Remove((int) kEntry.Id);
                    bool doesHaAnother = false;
                    for (int t = 0; t < NumThreads; ++t)
                    {
                        if (vc[t] > 0 &&
                            vc[t] <= ForVCGetClock(k, t))
                        {
                            doesHaAnother = true;
                            break;
                        }
                    }
                    if (!doesHaAnother)
                    {
                        candidateThreadIds.Add((int) kEntry.Id);
                    }
                    if (lookingFor.Count == 0)
                    {
                        break;
                    }
                }
            }

            // Make sure at least one candidate is found

            if (candidateThreadIds.Count == 0)
            {
                Asserter.Assert(false, "DPOR: There were no candidate backtrack points.");
            }

            // Is one already backtracked?
            foreach (var tid in candidateThreadIds)
            {
                if (aTidEntries.List[(int) tid].Backtrack)
                {
                    return;
                }
            }

            // None are backtracked, so we have to pick one.
            // Try to pick one that is slept first.
            // Start from thread b.tid:
            {
                int sleptThread = (int) step.Id;
                for (int k = 0; k < NumThreads; ++k)
                {
                    if (candidateThreadIds.Contains(sleptThread) &&
                        aTidEntries.List[(int)sleptThread].Sleep)
                    {
                        aTidEntries.List[(int)sleptThread].Backtrack = true;
                        return;
                    }
                    ++sleptThread;
                    if (sleptThread >= NumThreads)
                    {
                        sleptThread = 0;
                    }
                }
            }

            // None are slept.
            // Avoid picking threads that are disabled (due to yield hack)
            // Start from thread b.tid:
            {
                int backtrackThread = (int)step.Id;
                for (int k = 0; k < NumThreads; ++k)
                {
                    if (candidateThreadIds.Contains(backtrackThread) &&
                        aTidEntries.List[(int) backtrackThread].Enabled)
                    {
                        aTidEntries.List[(int) backtrackThread].Backtrack = true;
                        return;
                    }
                    ++backtrackThread;
                    if (backtrackThread >= NumThreads)
                    {
                        backtrackThread = 0;
                    }
                }
            }

            // None are slept and enabled.
            // Start from thread b.tid:
            {
                int backtrackThread = (int)step.Id;
                for (int k = 0; k < NumThreads; ++k)
                {
                    if (candidateThreadIds.Contains(backtrackThread))
                    {
                        aTidEntries.List[(int)backtrackThread].Backtrack = true;
                        return;
                    }
                    ++backtrackThread;
                    if (backtrackThread >= NumThreads)
                    {
                        backtrackThread = 0;
                    }
                }
            }

            Asserter.Assert(false, "DPOR: Did not manage to add backtrack point.");
        }


        private void UpdateFieldsAndRealocateDatastructuresIfNeeded(Stack stack)
        {
            NumThreads = (int) stack.GetTopAsRealTop().List.Count;
            NumSteps = (int) stack.StackInternal.Count;

            int temp = ThreadIdToLastOpIndex.Length;
            while (temp < NumThreads)
            {
                temp <<= 1;
            }

            if (ThreadIdToLastOpIndex.Length < temp)
            {
                ThreadIdToLastOpIndex = new int[temp];
                TargetIdToLastCreateStartEnd = new int[temp];
                TargetIdToLastSend = new int[temp];
            }
            else
            {
                Array.Clear(ThreadIdToLastOpIndex, 0, ThreadIdToLastOpIndex.Length);
                Array.Clear(TargetIdToLastCreateStartEnd, 0, TargetIdToLastCreateStartEnd.Length);
                Array.Clear(TargetIdToLastSend, 0, TargetIdToLastSend.Length);
            }

            int numClocks = NumThreads * NumSteps;

            temp = Vcs.Length;

            while (temp < numClocks)
            {
                temp <<= 1;
            }

            if (Vcs.Length < temp)
            {
                Vcs = new int[temp];
            }

            Races.Clear();
            RaceReplaySuffix.Clear();
            MissingThreadIds.Clear();
            ReplayRaceIndex = 0;

        }

    }
}