//-----------------------------------------------------------------------
// <copyright file="Stack.cs">
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

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// The stack datastructure used by <see cref="DPORStrategy"/> to perform the
    /// depth-first search.
    /// </summary>
    public class Stack
    {
        /// <summary>
        /// The actual stack.
        /// </summary>
        public readonly List<TidEntryList> StackInternal = new List<TidEntryList>();

        private int NextStackPos;

        private Random Rand;

        private IAsserter Asserter;

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
        /// <param name="asserter"></param>
        public Stack(Random rand, IAsserter asserter)
        {
            Rand = rand;
            Asserter = asserter;
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
            
            Asserter.Assert(NextStackPos <= StackInternal.Count, "DFS strategy unexpected stack state.");

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
            Asserter.Assert(NextStackPos == StackInternal.Count, "DFS Strategy: top of stack is not aligned.");
            return GetTop();
        }

        /// <summary>
        /// Get the next thread to schedule: either the preselected thread entry
        /// from the current schedule prefix that we are replaying or the first
        /// suitable thread entry from the real top of the stack.
        /// </summary>
        /// <returns></returns>
        public int GetSelectedOrFirstBacktrackNotSlept(int startingFrom)
        {
            var top = GetTop();

            if (StackInternal.Count > NextStackPos)
            {
                return top.GetSelected(Asserter);
            }

            int res = top.TryGetSelected(Asserter);
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
                
                if (top.IsNoneSelected(Asserter))
                {
                    Pop();
                }
            }
            

            // Pop until there are some tid entries that are not done/slept OR stack is empty.
            while (StackInternal.Count > 0)
            {
                TidEntryList top = GetTopAsRealTop();
                top.SetSelectedToSleep(Asserter);
                top.ClearSelected(Asserter);

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
            Asserter.Assert(NextStackPos == StackInternal.Count, "DFS Strategy: top of stack is not aligned.");
            StackInternal.RemoveAt(StackInternal.Count - 1);
            --NextStackPos;
        }

        private void CheckMatches(List<TidEntry> list)
        {
            Asserter.Assert(
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
    }
}