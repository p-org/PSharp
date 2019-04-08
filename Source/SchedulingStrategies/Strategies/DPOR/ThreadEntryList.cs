// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies.DPOR
{
    /// <summary>
    /// The elements of the <see cref="Stack"/> used by <see cref="DPORStrategy"/>.
    /// Stores a list of <see cref="ThreadEntry"/>; one for each <see cref="ISchedulable"/>.
    /// </summary>
    internal class ThreadEntryList
    {
        /// <summary>
        /// The actual list.
        /// </summary>
        public readonly List<ThreadEntry> List;

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
        /// Initializes a new instance of the <see cref="ThreadEntryList"/> class.
        /// </summary>
        public ThreadEntryList(List<ThreadEntry> list)
        {
            this.List = list;
            this.SelectedEntry = -1;
            this.NondetChoices = null;
            this.NextNondetChoiceIndex = 0;
        }

        /// <summary>
        /// Get a nondet choice.
        /// This may replay a nondet choice or make (and record) a new nondet choice.
        /// </summary>
        public int MakeOrReplayNondetChoice(bool isBoolChoice, IRandomNumberGenerator rand, IContract contract)
        {
            contract.Assert(
                rand != null || isBoolChoice,
                "A DFS DPOR exploration of int nondeterminstic choices " +
                "is not currently supported because this won't scale.");

            if (this.NondetChoices is null)
            {
                this.NondetChoices = new List<NonDetChoice>();
            }

            if (this.NextNondetChoiceIndex < this.NondetChoices.Count)
            {
                // Replay:
                NonDetChoice choice = this.NondetChoices[this.NextNondetChoiceIndex];
                ++this.NextNondetChoiceIndex;
                contract.Assert(choice.IsBoolChoice == isBoolChoice);
                return choice.Choice;
            }

            // Adding a choice.
            contract.Assert(this.NextNondetChoiceIndex == this.NondetChoices.Count);
            NonDetChoice ndc = new NonDetChoice
            {
                IsBoolChoice = isBoolChoice,
                Choice = rand is null ? 0 : (isBoolChoice ? rand.Next(2) : rand.Next())
            };
            this.NondetChoices.Add(ndc);
            ++this.NextNondetChoiceIndex;
            return ndc.Choice;
        }

        /// <summary>
        /// This method is used in a DFS exploration of nondet choice. It will pop off bool
        /// choices that are 1 until it reaches a 0 that will then be changed to a 1. The
        /// NextNondetChoiceIndex will be reset ready for replay.
        /// </summary>
        public bool BacktrackNondetChoices(IContract contract)
        {
            if (this.NondetChoices is null)
            {
                return false;
            }

            contract.Assert(this.NextNondetChoiceIndex == this.NondetChoices.Count);

            this.NextNondetChoiceIndex = 0;

            while (this.NondetChoices.Count > 0)
            {
                NonDetChoice choice = this.NondetChoices[this.NondetChoices.Count - 1];
                contract.Assert(choice.IsBoolChoice, "DFS DPOR only supports bool choices.");
                if (choice.Choice == 0)
                {
                    choice.Choice = 1;
                    this.NondetChoices[this.NondetChoices.Count - 1] = choice;
                    return true;
                }

                contract.Assert(choice.Choice == 1, "Unexpected choice value.");
                this.NondetChoices.RemoveAt(this.NondetChoices.Count - 1);
            }

            return false;
        }

        /// <summary>
        /// Prepares the list of nondet choices for replay. This is used by random DPOR, which does not need to
        /// backtrack individual nondet choices, but may need to replay all of them.
        /// </summary>
        public void RewindNondetChoicesForReplay()
        {
            this.NextNondetChoiceIndex = 0;
        }

        /// <summary>
        /// Clears the list of nondet choices for replay from the next nondet choice onwards.
        /// That is, nondet choices that have already been replayed remain in the list.
        /// </summary>
        public void ClearNondetChoicesFromNext()
        {
            if (this.NondetChoices != null && this.NextNondetChoiceIndex < this.NondetChoices.Count)
            {
                this.NondetChoices.RemoveRange(this.NextNondetChoiceIndex, this.NondetChoices.Count - this.NextNondetChoiceIndex);
            }
        }

        /// <summary>
        /// Add all enabled threads to the backtrack set.
        /// </summary>
        public void SetAllEnabledToBeBacktracked(IContract contract)
        {
            foreach (var tidEntry in this.List)
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
            foreach (var tidEntry in this.List)
            {
                if (!tidEntry.Enabled)
                {
                    continue;
                }

                if (tidEntry.Id == this.SelectedEntry)
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
        public string ShowSelected()
        {
            int selectedIndex = this.TryGetSelected();
            if (selectedIndex < 0)
            {
                return "-";
            }

            ThreadEntry selected = this.List[selectedIndex];
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
            foreach (var tidEntry in this.List)
            {
                if (!tidEntry.Backtrack)
                {
                    continue;
                }

                if (tidEntry.Id == this.SelectedEntry)
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
        public int GetFirstBacktrackNotSlept(int startingFrom)
        {
            int size = this.List.Count;
            int i = startingFrom;
            bool foundSlept = false;
            for (int count = 0; count < size; ++count)
            {
                if (this.List[i].Backtrack &&
                    !this.List[i].Sleep)
                {
                    return i;
                }

                if (this.List[i].Sleep)
                {
                    foundSlept = true;
                }

                ++i;
                if (i >= size)
                {
                    i = 0;
                }
            }

            return foundSlept ? Stack.SleepSetBlocked : -1;
        }

        /// <summary>
        /// Gets all threads in backtrack that are not slept and not selected.
        /// </summary>
        public List<int> GetAllBacktrackNotSleptNotSelected(IContract contract)
        {
            List<int> res = new List<int>();
            for (int i = 0; i < this.List.Count; ++i)
            {
                if (this.List[i].Backtrack &&
                    !this.List[i].Sleep &&
                    this.List[i].Id != this.SelectedEntry)
                {
                    contract.Assert(this.List[i].Enabled);
                    res.Add(i);
                }
            }

            return res;
        }

        /// <summary>
        /// Returns true if some threads are in backtrack, and are not slept nor selected.
        /// </summary>
        public bool HasBacktrackNotSleptNotSelected()
        {
            foreach (ThreadEntry t in this.List)
            {
                if (t.Backtrack &&
                    !t.Sleep &&
                    t.Id != this.SelectedEntry)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets the selected thread to be slept.
        /// </summary>
        public void SetSelectedToSleep(IContract contract)
        {
            this.List[this.GetSelected(contract)].Sleep = true;
        }

        /// <summary>
        /// Returns true if all threads are done or slept.
        /// </summary>
        public bool AllDoneOrSlept()
        {
            return this.GetFirstBacktrackNotSlept(0) < 0;
        }

        /// <summary>
        /// Tries to get the single selected thread. Returns -1 if no thread is selected.
        /// </summary>
        public int TryGetSelected()
        {
            return this.SelectedEntry;
        }

        /// <summary>
        /// Checks if no threads are selected.
        /// </summary>
        public bool IsNoneSelected()
        {
            return this.TryGetSelected() < 0;
        }

        /// <summary>
        /// Gets the selected thread. Asserts that there is a selected thread.
        /// </summary>
        public int GetSelected(IContract contract)
        {
            int res = this.TryGetSelected();
            contract.Assert(res != -1, "DFS Strategy: No selected tid entry!");
            return res;
        }

        /// <summary>
        /// Deselect the selected thread.
        /// </summary>
        public void ClearSelected()
        {
            this.SelectedEntry = -1;
        }

        /// <summary>
        /// Add the first enabled and not slept thread to the backtrack set.
        /// </summary>
        public void AddFirstEnabledNotSleptToBacktrack(int startingFrom, IContract contract)
        {
            int size = this.List.Count;
            int i = startingFrom;
            for (int count = 0; count < size; ++count)
            {
                if (this.List[i].Enabled &&
                    !this.List[i].Sleep)
                {
                    this.List[i].Backtrack = true;

                    // TODO: Remove?
                    contract.Assert(this.List[i].Enabled);
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
        /// Add a thread to the backtrack set.
        /// </summary>
        public void AddToBacktrack(int tid, IContract contract)
        {
            this.List[tid].Backtrack = true;
            contract.Assert(this.List[tid].Enabled);
        }

        /// <summary>
        /// Add a random enabled and not slept thread to the backtrack set.
        /// </summary>
        public void AddRandomEnabledNotSleptToBacktrack(IRandomNumberGenerator rand)
        {
            var enabledNotSlept = this.List.Where(e => e.Enabled && !e.Sleep).ToList();
            if (enabledNotSlept.Count > 0)
            {
                int choice = rand.Next(enabledNotSlept.Count);
                enabledNotSlept[choice].Backtrack = true;
            }
        }

        /// <summary>
        /// Sets the selected thread id. There must not already be a selected thread id.
        /// </summary>
        public void SetSelected(int tid, IContract contract)
        {
            contract.Assert(this.SelectedEntry < 0);
            contract.Assert(tid >= 0 && tid < this.List.Count);
            this.SelectedEntry = tid;
        }
    }
}
