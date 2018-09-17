// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies.DPOR
{
    /// <summary>
    /// The stack datastructure used by <see cref="DPORStrategy"/> to perform the
    /// depth-first search.
    /// </summary>
    internal class Stack
    {
        /// <summary>
        /// The actual stack.
        /// </summary>
        public readonly List<TidEntryList> StackInternal = new List<TidEntryList>();

        private int NextStackPos;

        private readonly IRandomNumberGenerator Rand;

        private readonly IContract Contract;

        /// <summary>
        /// If no thread id can be chosen,
        /// a negative thread id is returned.
        /// This indicates that some threads
        /// were enabled, but they were all slept (due to <see cref="SleepSets"/>).
        /// </summary>
        public const int SLEEP_SET_BLOCKED = -2;

        /// <summary>
        /// Construct the stack.
        /// </summary>
        /// <param name="rand">If non-null, then randomized DPOR is assumed.
        /// The stack will not be backtracked in <see cref="PrepareForNextSchedule"/>.</param>
        /// <param name="contract">IContract</param>
        public Stack(IRandomNumberGenerator rand, IContract contract)
        {
            Rand = rand;
            Contract = contract;
        }

        /// <summary>
        /// Push a list of tid entries onto the stack.
        /// If we are replaying, this will verify that
        /// the list is what we expected.
        /// </summary>
        /// <param name="machines"></param>
        /// <param name="prevThreadIndex"></param>
        /// <returns>true if a new element was added to the stack, otherwise the existing entry was verified</returns>
        public bool Push(List<ISchedulable> machines, int prevThreadIndex)
        {
            List<TidEntry> list = new List<TidEntry>();

            foreach (var machineInfo in machines)
            {
                list.Add(
                    new TidEntry(
                        (int) machineInfo.Id,
                        machineInfo.IsEnabled,
                        machineInfo.NextOperationType,
                        machineInfo.NextTargetType,
                        (int) machineInfo.NextTargetId,
                        (int) machineInfo.NextOperationMatchingSendIndex));
            }
            
            Contract.Assert(NextStackPos <= StackInternal.Count, "DFS strategy unexpected stack state.");

            bool added = NextStackPos == StackInternal.Count;

            if (added)
            {
                StackInternal.Add(new TidEntryList(list));
            }
            else
            {
                CheckMatches(list);
            }

            ++NextStackPos;

            return added;
        }

        /// <summary>
        /// Get the number of entries on the stack 
        /// (not including those that are yet to be replayed).
        /// </summary>
        /// <returns></returns>
        public int GetNumSteps()
        {
            return NextStackPos;
        }

        /// <summary>
        /// Get the real size of the stack
        /// (including entries that are yet to be replayed).
        /// </summary>
        /// <returns></returns>
        public int GetInternalSize()
        {
            return StackInternal.Count;
        }

        /// <summary>
        /// Get the top entry of the stack.
        /// </summary>
        /// <returns></returns>
        public TidEntryList GetTop()
        {
            return StackInternal[NextStackPos - 1];
        }

        /// <summary>
        /// Get the second from top entry of the stack.
        /// </summary>
        /// <returns></returns>
        public TidEntryList GetSecondFromTop()
        {
            return StackInternal[NextStackPos - 2];
        }

        /// <summary>
        /// Gets the top of stack and also ensures that this is the real top of stack.
        /// </summary>
        /// <returns></returns>
        public TidEntryList GetTopAsRealTop()
        {
            Contract.Assert(NextStackPos == StackInternal.Count, "DFS Strategy: top of stack is not aligned.");
            return GetTop();
        }

        /// <summary>
        /// Get the next thread to schedule: either the preselected thread entry
        /// from the current schedule prefix that we are replaying or the first
        /// suitable thread entry from the real top of the stack.
        /// </summary>
        public int GetSelectedOrFirstBacktrackNotSlept(int startingFrom)
        {
            var top = GetTop();

            if (StackInternal.Count > NextStackPos)
            {
                return top.GetSelected(Contract);
            }

            int res = top.TryGetSelected(Contract);
            return res >= 0 ? res : top.GetFirstBacktrackNotSlept(startingFrom);
        }

        /// <summary>
        /// Prepare for the next schedule by popping entries from the stack
        /// until we find some tid entries that are not slept.
        /// </summary>
        public void PrepareForNextSchedule()
        {
            if (Rand != null)
            {
                NextStackPos = 0;
                return;

            }

            // Deadlock / sleep set blocked; no selected tid entry.
            {
                TidEntryList top = GetTopAsRealTop();
                
                if (top.IsNoneSelected(Contract))
                {
                    Pop();
                }
            }
            

            // Pop until there are some tid entries that are not done/slept OR stack is empty.
            while (StackInternal.Count > 0)
            {
                TidEntryList top = GetTopAsRealTop();

                if (top.BacktrackNondetChoices(Contract))
                {
                    break;
                }

                top.SetSelectedToSleep(Contract);
                top.ClearSelected(Contract);

                if (!top.AllDoneOrSlept())
                {
                    break;
                }

                Pop();
            }

            NextStackPos = 0;
        }

        private void Pop()
        {
            Contract.Assert(NextStackPos == StackInternal.Count, "DFS Strategy: top of stack is not aligned.");
            StackInternal.RemoveAt(StackInternal.Count - 1);
            --NextStackPos;
        }

        private void CheckMatches(List<TidEntry> list)
        {
            Contract.Assert(
                StackInternal[NextStackPos].List.SequenceEqual(list, TidEntry.ComparerSingleton), 
                "DFS strategy detected nondeterminism when replaying.");
        }

        /// <summary>
        /// Clear the stack.
        /// </summary>
        public void Clear()
        {
            StackInternal.Clear();
        }

        /// <summary>
        /// Clear all entries beyond the current top of stack.
        /// </summary>
        public void ClearAboveTop()
        {
            StackInternal.RemoveRange(NextStackPos, StackInternal.Count - NextStackPos);
        }
    }
}