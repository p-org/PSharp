//-----------------------------------------------------------------------
// <copyright file="TidEntryList.cs">
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
using System.Text;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies.DPOR
{
    /// <summary>
    /// The elements of the <see cref="Stack"/> 
    /// used by <see cref="DPORStrategy"/>.
    /// Stores a list of <see cref="TidEntry"/>;
    /// one for each <see cref="ISchedulable"/>.
    /// </summary>
    internal class TidEntryList
    {
        /// <summary>
        /// The actual list.
        /// </summary>
        public readonly List<TidEntry> List;

        private int SelectedEntry;

        /// <summary>
        /// A list of random choices made by the <see cref="SelectedEntry"/> thread as part of its
        /// visible operation.
        /// Can be null.
        /// </summary>
        public List<NonDetChoice> NondetChoices;

        /// <summary>
        /// When replaying/adding nondet choices,
        /// this is the index of the next nondet choice.
        /// </summary>
        private int NextNondetChoiceIndex;

        /// <summary>
        /// Construct a TidEntryList.
        /// </summary>
        /// <param name="list"></param>
        public TidEntryList(List<TidEntry> list)
        {
            List = list;
            SelectedEntry = -1;
            NondetChoices = null;
            NextNondetChoiceIndex = 0;
        }

        /// <summary>
        /// Get a nondet choice.
        /// This may replay a nondet choice or make (and record) a new nondet choice.
        /// </summary>
        /// <param name="isBoolChoice">If true, a boolean choice; otherwise, an int choice.</param>
        /// <param name="rand">Random</param>
        /// <param name="contract">IContract</param>
        public int MakeOrReplayNondetChoice(bool isBoolChoice, IRandomNumberGenerator rand, IContract contract)
        {
            contract.Assert(
                rand != null || isBoolChoice,
                "A DFS DPOR exploration of int nondeterminstic choices " +
                "is not currently supported because this won't scale.");

            if (NondetChoices == null)
            {
                NondetChoices = new List<NonDetChoice>();
            }


            if (NextNondetChoiceIndex < NondetChoices.Count)
            {
                // Replay:
                NonDetChoice choice = NondetChoices[NextNondetChoiceIndex];
                ++NextNondetChoiceIndex;
                contract.Assert(choice.IsBoolChoice == isBoolChoice);
                return choice.Choice;
            }

            // Adding a choice.
            contract.Assert(NextNondetChoiceIndex == NondetChoices.Count);
            NonDetChoice ndc = new NonDetChoice
            {
                IsBoolChoice = isBoolChoice,
                Choice = rand == null ? 0 : (isBoolChoice ? rand.Next(2) : rand.Next())
            };
            NondetChoices.Add(ndc);
            ++NextNondetChoiceIndex;
            return ndc.Choice;
        }

        /// <summary>
        /// This method is used in a DFS exploration of nondet choice.
        /// It will pop off bool choices that are 1 until
        /// it reaches a 0 that will then be changed to a 1.
        /// The NextNondetChoiceIndex will be reset ready for replay.
        /// </summary>
        /// <param name="contract">IContract</param>
        /// <returns>false if there are no more nondet choices to explore</returns>
        public bool BacktrackNondetChoices(IContract contract)
        {
            if (NondetChoices == null)
            {
                return false;
            }

            contract.Assert(NextNondetChoiceIndex == NondetChoices.Count);

            NextNondetChoiceIndex = 0;

            while (NondetChoices.Count > 0)
            {
                NonDetChoice choice = NondetChoices[NondetChoices.Count - 1];
                contract.Assert(choice.IsBoolChoice, "DFS DPOR only supports bool choices.");
                if (choice.Choice == 0)
                {
                    choice.Choice = 1;
                    NondetChoices[NondetChoices.Count - 1] = choice;
                    return true;
                }
                contract.Assert(choice.Choice == 1, "Unexpected choice value.");
                NondetChoices.RemoveAt(NondetChoices.Count - 1);
            }

            return false;
        }

        /// <summary>
        /// Prepares the list of nondet choices for replay.
        /// This is used by random DPOR, which does not need to
        /// backtrack individual nondet choices, but may need to replay all of them.
        /// </summary>
        public void RewindNondetChoicesForReplay()
        {
            NextNondetChoiceIndex = 0;
        }

        /// <summary>
        /// Add all enabled threads to the backtrack set.
        /// </summary>
        /// <param name="contract">IContract</param>
        public void SetAllEnabledToBeBacktracked(IContract contract)
        {
            foreach (var tidEntry in List)
            {
                if (tidEntry.Enabled)
                {
                    tidEntry.Backtrack = true;
                    // TODO: Remove?
                    contract.Assert(tidEntry.Enabled);
                }
            }
        }

        /// <summary>
        /// Utility method to show the enabled threads.
        /// </summary>
        /// <returns>string</returns>
        public string ShowEnabled()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (var tidEntry in List)
            {
                if (!tidEntry.Enabled)
                {
                    continue;
                }
                if (tidEntry.Id == SelectedEntry)
                {
                    sb.Append("*");
                }
                sb.Append("(");
                sb.Append(tidEntry.Id);
                sb.Append(", ");
                sb.Append(tidEntry.OpType);
                sb.Append(", ");
                sb.Append(tidEntry.TargetType);
                sb.Append("-");
                sb.Append(tidEntry.TargetId);
                sb.Append(") ");
            }

            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Utility method to show the selected thread.
        /// </summary>
        /// <param name="contract">IContract</param>
        public string ShowSelected(IContract contract)
        {
            int selectedIndex = TryGetSelected(contract);
            if (selectedIndex < 0)
            {
                return "-";
            }

            TidEntry selected = List[selectedIndex];
            int priorSend = selected.OpType == OperationType.Receive
                ? selected.SendStepIndex
                : -1;

            return $"({selected.Id}, {selected.OpType}, {selected.TargetType}, {selected.TargetId}, {priorSend})";
        }

        /// <summary>
        /// Utility method to show the threads in the backtrack set.
        /// </summary>
        /// <returns>string</returns>
        public string ShowBacktrack()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("[");
            foreach (var tidEntry in List)
            {
                if (!tidEntry.Backtrack)
                {
                    continue;
                }
                if (tidEntry.Id == SelectedEntry)
                {
                    sb.Append("*");
                }
                sb.Append("(");
                sb.Append(tidEntry.Id);
                sb.Append(", ");
                sb.Append(tidEntry.OpType);
                sb.Append(", ");
                sb.Append(tidEntry.TargetType);
                sb.Append("-");
                sb.Append(tidEntry.TargetId);
                sb.Append(") ");
            }

            sb.Append("]");
            return sb.ToString();
        }

        /// <summary>
        /// Gets the first thread in backtrack that is not slept.
        /// </summary>
        /// <returns></returns>
        public int GetFirstBacktrackNotSlept(int startingFrom)
        {
            int size = List.Count;
            int i = startingFrom;
            bool foundSlept = false;
            for (int count = 0; count < size; ++count)
            {
                if (List[i].Backtrack &&
                    !List[i].Sleep)
                {
                    return i;
                }
                if (List[i].Sleep)
                {
                    foundSlept = true;
                }
                ++i;
                if (i >= size)
                {
                    i = 0;
                }
            }

            return foundSlept ? Stack.SLEEP_SET_BLOCKED : -1;
        }

        /// <summary>
        /// Gets all threads in backtrack that are not slept and not selected.
        /// </summary>
        /// <param name="contract">IContract</param>
        public List<int> GetAllBacktrackNotSleptNotSelected(IContract contract)
        {
            List<int> res = new List<int>();
            for (int i = 0; i < List.Count; ++i)
            {
                if (List[i].Backtrack &&
                    !List[i].Sleep &&
                    List[i].Id != SelectedEntry)
                {
                    contract.Assert(List[i].Enabled);
                    res.Add(i);
                }
            }

            return res;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if some threads are: in backtrack and not slept and not selected.</returns>
        public bool HasBacktrackNotSleptNotSelected()
        {
            foreach (TidEntry t in List)
            {
                if (t.Backtrack &&
                    !t.Sleep &&
                    t.Id != SelectedEntry)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the selected thread to be slept.
        /// </summary>
        /// <param name="contract">IContract</param>
        public void SetSelectedToSleep(IContract contract)
        {
            List[GetSelected(contract)].Sleep = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if all threads are done or slept.</returns>
        public bool AllDoneOrSlept()
        {
            return GetFirstBacktrackNotSlept(0) < 0;
        }

        /// <summary>
        /// Tries to get the single selected thread.
        /// </summary>
        /// <param name="contract">IContract</param>
        /// <returns>The selected thread index or -1 if no thread is selected.</returns>
        public int TryGetSelected(IContract contract)
        {
            return SelectedEntry;
        }

        /// <summary>
        /// Are no threads selected?
        /// </summary>
        /// <param name="contract">IContract</param>
        public bool IsNoneSelected(IContract contract)
        {
            return TryGetSelected(contract) < 0;
        }

        /// <summary>
        /// Gets the selected thread.
        /// Asserts that there is a selected thread.
        /// </summary>
        /// <param name="contract">IContract</param>
        public int GetSelected(IContract contract)
        {
            int res = TryGetSelected(contract);
            contract.Assert(res != -1, "DFS Strategy: No selected tid entry!");
            return res;
        }

        /// <summary>
        /// Deselect the selected thread.
        /// </summary>
        /// <param name="contract">IContract</param>
        public void ClearSelected(IContract contract)
        {
            SelectedEntry = -1;
        }

        /// <summary>
        /// Add the first enabled and not slept thread to the backtrack set.
        /// </summary>
        /// <param name="startingFrom">a thread id to start from</param>
        /// <param name="contract">IContract</param>
        public void AddFirstEnabledNotSleptToBacktrack(int startingFrom, IContract contract)
        {
            int size = List.Count;
            int i = startingFrom;
            for (int count = 0; count < size; ++count)
            {
                if (List[i].Enabled &&
                    !List[i].Sleep)
                {
                    List[i].Backtrack = true;
                    // TODO: Remove?
                    contract.Assert(List[i].Enabled);
                    return;
                }
                ++i;
                if (i >= size)
                {
                    i = 0;
                }
            }
        }

        /// <summary>
        /// Add a random enabled and not slept thread to the backtrack set.
        /// </summary>
        /// <param name="rand"></param>
        public void AddRandomEnabledNotSleptToBacktrack(IRandomNumberGenerator rand)
        {
            var enabledNotSlept = List.Where(e => e.Enabled && !e.Sleep).ToList();
            if (enabledNotSlept.Count > 0)
            {
                int choice = rand.Next(enabledNotSlept.Count);
                enabledNotSlept[choice].Backtrack = true;
            }
        }

        /// <summary>
        /// Sets the selected thread id.
        /// There must not already be a selected thread id.
        /// </summary>
        /// <param name="tid">thread id to be set to selected</param>
        /// <param name="contract">IContract</param>
        public void SetSelected(int tid, IContract contract)
        {
            contract.Assert(SelectedEntry < 0);
            contract.Assert(tid >= 0 && tid < List.Count);
            SelectedEntry = tid;
        }
    }
}