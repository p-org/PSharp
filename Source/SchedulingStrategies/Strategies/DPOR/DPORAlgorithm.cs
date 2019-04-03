// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies.DPOR
{
    /// <summary>
    /// The actual DPOR algorithm used by <see cref="DPORStrategy"/>.
    ///
    /// This is actually the "Source DPOR" algorithm.
    ///
    /// Implementation notes:
    ///
    /// Note that when we store indexes, they start at 1.
    /// This allows 0 to mean: null / not yet seen.
    /// Thus, when accessing e.g. Vcs, we must subtract 1.
    /// But most accesses should be done via a method to hide this.
    ///
    /// The happens-before relation (HBR) is assigned as follows:
    /// - create, start, stop with the same target id are totally-ordered
    /// - operations from the same thread are totally ordered.
    /// - corresponding send-receive operation pairs are ordered.
    /// - sends to the same target id are totally-ordered.
    ///
    /// That last one is expected, but a bit annoying when doing random DPOR
    /// because it limits the races (see below) between send operations to the same target id.
    ///
    /// The HBR is tracked using vector clocks (VCs).
    ///
    /// A pair of operations A and B is a race iff:
    /// - A happens-before B
    /// - and A and B are from different threads
    /// - and there does not exist an operation C such that A happens-before C happens-before B.
    ///
    /// In other words, A and B must be directly related in the HBR with no intervening operation
    /// connecting them. They do not need to be adjacent though
    /// (i.e. in a schedule, ACB, A and B might still be a race, unless A hb C hb B).
    /// We check this in the code by checking if A hb B *before* we update the
    /// VC of B to include the A-B edge; if A already happens-before B then this is not a race.
    /// </summary>
    internal class DPORAlgorithm
    {
        /// <summary>
        /// An upper bound of the number of threads (schedulables).
        /// Will be increased as needed.
        /// </summary>
        private int NumThreads;

        /// <summary>
        /// The total number of steps (visible operations).
        /// Will be increased as needed.
        /// </summary>
        private int NumSteps;

        /// <summary>
        /// A map from thread id to the index of the last op
        /// performed by the thread.
        /// </summary>
        private int[] ThreadIdToLastOpIndex;

        /// <summary>
        /// A map from target id to the last create, start or stop operation.
        /// Used to update the HBR/VCs.
        /// </summary>
        private int[] TargetIdToLastCreateStartEnd;

        /// <summary>
        /// A map from target id to the last send (*to* this target).
        /// Used to update the HBR/VCs.
        /// </summary>
        private int[] TargetIdToLastSend;

        /// <summary>
        /// A map from target id to the first send (*to* this target).
        /// TODO: Used in random DPOR to slightly limit the search
        /// of prior sends that could race with the current send.
        /// </summary>
        private int[] TargetIdToFirstSend;

        /// <summary>
        /// The list of vector clocks (VCs).
        /// We store a VC for each visible operation.
        /// Thus, this is a map from a step index to the operation's VC.
        /// Given a step index i and thread id c:
        ///   Vcs[(i-1)*NumThreads + c] == the vector clock of thread c.
        /// </summary>
        private int[] Vcs;

        /// <summary>
        /// A way for the ISchedulable to assert conditions.
        /// </summary>
        private readonly IContract Contract;

        /// <summary>
        /// A list of all races.
        /// Only used when performing random DPOR.
        /// </summary>
        private readonly List<Race> Races;

        /// <summary>
        /// The missing thread ids when replaying a reversed race.
        /// When performing random DPOR,
        /// we pick a race, reverse it, and "replay" it.
        /// In the replay, some threads may not get created
        /// (because the replay is different)
        /// and we must be careful when writing the RaceReplaySuffix,
        /// subtracting 1 for every missing thread that is less than
        /// the thread id we want to record.
        /// This field tracks those threads.
        /// It is a list of thread ids (not a map).
        /// </summary>
        private readonly List<int> MissingThreadIds;

        /// <summary>
        /// When performing random DPOR,
        /// this field gives the schedule (as a list of thread ids)
        /// that should be followed in order to reverse a randomly chosen race.
        /// </summary>
        public readonly List<TidForRaceReplay> RaceReplaySuffix;

        /// <summary>
        /// An index for the <see cref="RaceReplaySuffix"/> (for convenience) to be used
        /// when replaying a reversed race.
        /// </summary>
        public int ReplayRaceIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="DPORAlgorithm"/> class.
        /// </summary>
        public DPORAlgorithm(IContract contract)
        {
            this.Contract = contract;

            // Initial estimates.
            this.NumThreads = 4;
            this.NumSteps = 1 << 8;

            this.ThreadIdToLastOpIndex = new int[this.NumThreads];
            this.TargetIdToLastCreateStartEnd = new int[this.NumThreads];
            this.TargetIdToLastSend = new int[this.NumThreads];
            this.TargetIdToFirstSend = new int[this.NumThreads];
            this.Vcs = new int[this.NumSteps * this.NumThreads];
            this.Races = new List<Race>(this.NumSteps);
            this.RaceReplaySuffix = new List<TidForRaceReplay>();
            this.MissingThreadIds = new List<int>();
            this.ReplayRaceIndex = 0;
        }

        private void FromVCSetVC(int from, int to)
        {
            int fromI = (from - 1) * this.NumThreads;
            int toI = (to - 1) * this.NumThreads;
            for (int i = 0; i < this.NumThreads; ++i)
            {
                this.Vcs[toI] = this.Vcs[fromI];
                ++fromI;
                ++toI;
            }
        }

        private void ForVCSetClockToValue(int vc, int clock, int value)
        {
            this.Vcs[((vc - 1) * this.NumThreads) + clock] = value;
        }

        private int ForVCGetClock(int vc, int clock) => this.Vcs[((vc - 1) * this.NumThreads) + clock];

        private void ForVCJoinFromVC(int to, int from)
        {
            int fromI = (from - 1) * this.NumThreads;
            int toI = (to - 1) * this.NumThreads;
            for (int i = 0; i < this.NumThreads; ++i)
            {
                if (this.Vcs[fromI] > this.Vcs[toI])
                {
                    this.Vcs[toI] = this.Vcs[fromI];
                }

                ++fromI;
                ++toI;
            }
        }

        private void ClearVC(int vc)
        {
            int vcI = (vc - 1) * this.NumThreads;
            for (int i = 0; i < this.NumThreads; ++i)
            {
                this.Vcs[vcI] = 0;
                ++vcI;
            }
        }

        private int[] GetVC(int vc)
        {
            int[] res = new int[this.NumThreads];
            int fromI = (vc - 1) * this.NumThreads;
            for (int i = 0; i < this.NumThreads; ++i)
            {
                res[i] = this.Vcs[fromI];
                ++fromI;
            }

            return res;
        }

        private bool HB(int threadIdOfA, int vca, int vcb)
        {
            // A hb B
            // iff:
            // A's index <= B.VC[A's tid]

            // ThreadEntry aStep = GetSelectedThreadEntry(stack, vca);
            return vca <= this.ForVCGetClock(vcb, /*aStep.Id*/ threadIdOfA);
        }

        private ThreadEntry GetSelectedThreadEntry(Stack stack, int index)
        {
            var list = GetThreadsAt(stack, index);
            return list.List[list.GetSelected(this.Contract)];
        }

        /// <summary>
        /// Checks if two operations are reversible. Assumes both operations passed in are dependent.
        /// </summary>
        private bool Reversible(Stack stack, int index1, int index2)
        {
            var step1 = this.GetSelectedThreadEntry(stack, index1);
            var step2 = this.GetSelectedThreadEntry(stack, index2);
            return step1.OpType == OperationType.Send && step2.OpType == OperationType.Send;
        }

        private static ThreadEntryList GetThreadsAt(Stack stack, int index) => stack.StackInternal[index - 1];

        /// <summary>
        /// The main entry point to the DPOR algorithm.
        /// </summary>
        /// <param name="stack">Should contain a terminal schedule.</param>
        /// <param name="rand">If non-null, then a randomized DPOR algorithm will be used.</param>
        public void DoDPOR(Stack stack, IRandomNumberGenerator rand)
        {
            this.UpdateFieldsAndRealocateDatastructuresIfNeeded(stack);

            // Indexes start at 1.
            for (int i = 1; i < this.NumSteps; ++i)
            {
                ThreadEntry step = this.GetSelectedThreadEntry(stack, i);
                if (this.ThreadIdToLastOpIndex[step.Id] > 0)
                {
                    this.FromVCSetVC(this.ThreadIdToLastOpIndex[step.Id], i);
                }
                else
                {
                    this.ClearVC(i);
                }

                this.ForVCSetClockToValue(i, step.Id, i);
                this.ThreadIdToLastOpIndex[step.Id] = i;

                int targetId = step.TargetId;
                if (step.OpType == OperationType.Create && i + 1 < this.NumSteps)
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
                            this.TargetIdToLastCreateStartEnd[targetId];
                        this.TargetIdToLastCreateStartEnd[targetId] = i;
                        break;
                    }

                    case OperationType.Send:
                    {
                        lastAccessIndex = this.TargetIdToLastSend[targetId];
                        this.TargetIdToLastSend[targetId] = i;
                        if (this.TargetIdToFirstSend[targetId] == 0)
                        {
                            this.TargetIdToFirstSend[targetId] = i;
                        }

                        break;
                    }

                    case OperationType.Receive:
                    {
                        lastAccessIndex = step.SendStepIndex;
                        break;
                    }

                    case OperationType.WaitForQuiescence:
                        for (int j = 0; j < this.ThreadIdToLastOpIndex.Length; j++)
                        {
                            if (j == step.Id || this.ThreadIdToLastOpIndex[j] == 0)
                            {
                                continue;
                            }

                            this.ForVCJoinFromVC(i, this.ThreadIdToLastOpIndex[j]);
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
                    this.AddBacktrack(stack, rand != null, lastAccessIndex, i, step);

                    // Random DPOR skips joining for sends.
                    if (!(rand != null && step.OpType == OperationType.Send))
                    {
                        this.ForVCJoinFromVC(i, lastAccessIndex);
                    }
                }
            }

            if (rand != null)
            {
                this.DoRandomRaceReverse(stack, rand);
            }
        }

        private void DoRandomRaceReverse(Stack stack, IRandomNumberGenerator rand)
        {
            int raceIndex = rand.Next(this.Races.Count);
            if (raceIndex == 0)
            {
                return;
            }

            Race race = this.Races[raceIndex];

            this.Contract.Assert(this.RaceReplaySuffix.Count == 0, "Tried to reverse race but replay suffix was not empty!");

            int threadIdOfA = this.GetSelectedThreadEntry(stack, race.A).Id;

            // Add to RaceReplaySuffix: all steps between a and b that do not h.a. a.
            for (int i = race.A; i < race.B; ++i)
            {
                if (this.HB(threadIdOfA, race.A, i))
                {
                    // Skip it. But track the missing thread id if this is a create operation.
                    if (this.GetSelectedThreadEntry(stack, i).OpType == OperationType.Create)
                    {
                        var missingThreadId = GetThreadsAt(stack, i).List.Count;
                        var index = this.MissingThreadIds.BinarySearch(missingThreadId);

                        // We should not find it.
                        this.Contract.Assert(index < 0);

                        // Get the next largest index (see BinarySearch).
                        index = ~index;

                        // Insert before the next largest item.
                        this.MissingThreadIds.Insert(index, missingThreadId);
                    }
                }
                else
                {
                    // Add thread id to the RaceReplaySuffix, but adjust
                    // it for missing thread ids.
                    this.AddThreadIdToRaceReplaySuffix(GetThreadsAt(stack, i));
                }
            }

            this.AddThreadIdToRaceReplaySuffix(GetThreadsAt(stack, race.B));
            this.AddThreadIdToRaceReplaySuffix(GetThreadsAt(stack, race.A));

            // Remove steps from a onwards. Indexes start at one so we must subtract 1.
            stack.StackInternal.RemoveRange(race.A - 1, stack.StackInternal.Count - (race.A - 1));
        }

        private void AddThreadIdToRaceReplaySuffix(ThreadEntryList tidEntryList)
        {
            // Add thread id to the RaceReplaySuffix, but adjust
            // it for missing thread ids.
            int tid = tidEntryList.GetSelected(this.Contract);
            var index = this.MissingThreadIds.BinarySearch(tid);

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

            this.RaceReplaySuffix.Add(new TidForRaceReplay(tid - index, tidEntryList.NondetChoices));
        }

        private void DoRandomDPORAddRaces(Stack stack, int lastAccessIndex, int i, ThreadEntry step)
        {
            // Note: Only sends are reversible
            // so we will only get here if step.OpType is a send
            // so the if below is redundant.
            // But I am leaving this here in case we end up with more reversible types.
            // The following assert will then fail and more thought will be needed.
            // The if below is probably sufficient though, so the assert can probably just be removed.
            this.Contract.Assert(step.OpType == OperationType.Send);

            // Easy case (non-sends).
            if (step.OpType != OperationType.Send)
            {
                this.Races.Add(new Race(lastAccessIndex, i));
                return;
            }

            // Harder case (sends).

            // In random DPOR, we don't just want the last race;
            // we want all races.
            // In random DPOR, we don't join the VCs of sends,
            // so a send will potentially race with many prior sends to the
            // same mailbox.
            // We scan the stack looking for concurrent sends.
            // We only need to start scanning from the first send to this mailbox.
            int firstSend = this.TargetIdToFirstSend[step.TargetId];
            this.Contract.Assert(
                firstSend > 0,
                "We should only get here if a send races with a prior send, but it does not.");

            for (int j = firstSend; j < i; j++)
            {
                var entry = this.GetSelectedThreadEntry(stack, j);
                if (entry.OpType != OperationType.Send
                    || entry.Id == step.Id
                    || this.HB(entry.Id, j, i))
                {
                    continue;
                }

                this.Races.Add(new Race(j, i));
            }
        }

        private void AddBacktrack(Stack stack, bool doRandomDPOR, int lastAccessIndex, int i, ThreadEntry step)
        {
            var aTidEntries = GetThreadsAt(stack, lastAccessIndex);
            var a = this.GetSelectedThreadEntry(stack, lastAccessIndex);
            if (this.HB(a.Id, lastAccessIndex, i) || !this.Reversible(stack, lastAccessIndex, i))
            {
                return;
            }

            if (doRandomDPOR)
            {
                this.DoRandomDPORAddRaces(stack, lastAccessIndex, i, step);
                return;
            }

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
                candidateThreadIds.Add(step.Id);
            }

            var lookingFor = new HashSet<int>();
            for (int j = 0; j < aTidEntries.List.Count; ++j)
            {
                if (j != a.Id &&
                    j != step.Id &&
                    (aTidEntries.List[j].Enabled || aTidEntries.List[j].OpType == OperationType.Yield))
                {
                    lookingFor.Add(j);
                }
            }

            int[] vc = new int[this.NumThreads];
            vc[a.Id] = lastAccessIndex;
            if (lookingFor.Count > 0)
            {
                for (int k = lastAccessIndex + 1; k < i; ++k)
                {
                    var kEntry = this.GetSelectedThreadEntry(stack, k);
                    if (!lookingFor.Contains(kEntry.Id))
                    {
                        continue;
                    }

                    lookingFor.Remove(kEntry.Id);
                    bool doesHaAnother = false;
                    for (int t = 0; t < this.NumThreads; ++t)
                    {
                        if (vc[t] > 0 &&
                            vc[t] <= this.ForVCGetClock(k, t))
                        {
                            doesHaAnother = true;
                            break;
                        }
                    }

                    if (!doesHaAnother)
                    {
                        candidateThreadIds.Add(kEntry.Id);
                    }

                    if (lookingFor.Count == 0)
                    {
                        break;
                    }
                }
            }

            // Make sure at least one candidate is found.
            this.Contract.Assert(candidateThreadIds.Count > 0, "DPOR: There were no candidate backtrack points.");

            // Is one already backtracked?
            foreach (var tid in candidateThreadIds)
            {
                if (aTidEntries.List[tid].Backtrack)
                {
                    return;
                }
            }

            {
                // None are backtracked, so we have to pick one.
                // Try to pick one that is slept first.
                // Start from thread b.tid:
                int sleptThread = step.Id;
                for (int k = 0; k < this.NumThreads; ++k)
                {
                    if (candidateThreadIds.Contains(sleptThread) &&
                        aTidEntries.List[sleptThread].Sleep)
                    {
                        aTidEntries.List[sleptThread].Backtrack = true;
                        return;
                    }

                    ++sleptThread;
                    if (sleptThread >= this.NumThreads)
                    {
                        sleptThread = 0;
                    }
                }
            }

            {
                // None are slept.
                // Avoid picking threads that are disabled (due to yield hack)
                // Start from thread b.tid:
                int backtrackThread = step.Id;
                for (int k = 0; k < this.NumThreads; ++k)
                {
                    if (candidateThreadIds.Contains(backtrackThread) &&
                        aTidEntries.List[backtrackThread].Enabled)
                    {
                        aTidEntries.List[backtrackThread].Backtrack = true;
                        return;
                    }

                    ++backtrackThread;
                    if (backtrackThread >= this.NumThreads)
                    {
                        backtrackThread = 0;
                    }
                }
            }

            {
                // None are slept and enabled.
                // Start from thread b.tid:
                int backtrackThread = step.Id;
                for (int k = 0; k < this.NumThreads; ++k)
                {
                    if (candidateThreadIds.Contains(backtrackThread))
                    {
                        aTidEntries.List[backtrackThread].Backtrack = true;
                        return;
                    }

                    ++backtrackThread;
                    if (backtrackThread >= this.NumThreads)
                    {
                        backtrackThread = 0;
                    }
                }
            }

            this.Contract.Assert(false, "DPOR: Did not manage to add backtrack point.");
        }

        private void UpdateFieldsAndRealocateDatastructuresIfNeeded(Stack stack)
        {
            // GetTop will throw if 0 steps have been performed but this should never happen.
            this.NumThreads = stack.GetTopAsRealTop().List.Count;
            this.NumSteps = stack.StackInternal.Count;

            int temp = this.ThreadIdToLastOpIndex.Length;
            while (temp < this.NumThreads)
            {
                temp <<= 1;
            }

            if (this.ThreadIdToLastOpIndex.Length < temp)
            {
                this.ThreadIdToLastOpIndex = new int[temp];
                this.TargetIdToLastCreateStartEnd = new int[temp];
                this.TargetIdToLastSend = new int[temp];
                this.TargetIdToFirstSend = new int[temp];
            }
            else
            {
                Array.Clear(this.ThreadIdToLastOpIndex, 0, this.ThreadIdToLastOpIndex.Length);
                Array.Clear(this.TargetIdToLastCreateStartEnd, 0, this.TargetIdToLastCreateStartEnd.Length);
                Array.Clear(this.TargetIdToLastSend, 0, this.TargetIdToLastSend.Length);
                Array.Clear(this.TargetIdToFirstSend, 0, this.TargetIdToFirstSend.Length);
            }

            int numClocks = this.NumThreads * this.NumSteps;

            temp = this.Vcs.Length;

            while (temp < numClocks)
            {
                temp <<= 1;
            }

            if (this.Vcs.Length < temp)
            {
                this.Vcs = new int[temp];
            }

            this.Races.Clear();
            this.RaceReplaySuffix.Clear();
            this.MissingThreadIds.Clear();
            this.ReplayRaceIndex = 0;
        }
    }
}
