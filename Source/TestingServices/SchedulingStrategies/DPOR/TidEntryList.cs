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

using System.Collections.Generic;
using System.Text;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// The elements of the <see cref="Stack"/> 
    /// used by <see cref="DPORStrategy"/>.
    /// Stores a list of <see cref="TidEntry"/>;
    /// one for each <see cref="ISchedulable"/>.
    /// </summary>
    public class TidEntryList
    {
        /// <summary>
        /// The actual list.
        /// </summary>
        public readonly List<TidEntry> List;

        /// <summary>
        /// Construct a TidEntryList.
        /// </summary>
        /// <param name="list"></param>
        public TidEntryList(List<TidEntry> list)
        {
            List = list;
        }

        /// <summary>
        /// Add all enabled threads to the backtrack set.
        /// </summary>
        public void SetAllEnabledToBeBacktracked(IAsserter asserter)
        {
            foreach (var tidEntry in List)
            {
                if (tidEntry.Enabled)
                {
                    tidEntry.Backtrack = true;
                    // TODO: Remove?
                    asserter.Assert(tidEntry.Enabled);
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
                if (tidEntry.Selected)
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
        /// <returns>string</returns>
        public string ShowSelected(IAsserter asserter)
        {
            int selectedIndex = TryGetSelected(asserter);
            if (selectedIndex < 0)
            {
                return "-";
            }
            TidEntry selected = List[selectedIndex];
            return $"({selected.Id}, {selected.OpType}, {selected.TargetType}, {selected.TargetId})";

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
                if (tidEntry.Selected)
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
        /// <returns></returns>
        public List<int> GetAllBacktrackNotSleptNotSelected(IAsserter asserter)
        {
            List<int> res = new List<int>();
            for (int i = 0; i < List.Count; ++i)
            {
                if (List[i].Backtrack &&
                    !List[i].Sleep &&
                    !List[i].Selected)
                {
                    asserter.Assert(List[i].Enabled);
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
                    !t.Selected)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Sets the selected thread to be slept.
        /// </summary>
        public void SetSelectedToSleep(IAsserter asserter)
        {
            List[GetSelected(asserter)].Sleep = true;
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
        /// <returns>The selected thread index or -1 if no thread is selected.</returns>
        public int TryGetSelected(IAsserter asserter)
        {
            int res = -1;
            for (int i = 0; i < List.Count; ++i)
            {
                if (List[i].Selected)
                {
                    asserter.Assert(res == -1, "DFS Strategy: More than one selected tid entry!");
                    res = i;
                }
            }
            

            return res;
        }

        /// <summary>
        /// Are no threads selected?
        /// </summary>
        /// <returns>bool</returns>
        public bool IsNoneSelected(IAsserter asserter)
        {
            return TryGetSelected(asserter) < 0;
        }

        /// <summary>
        /// Gets the selected thread.
        /// Asserts that there is a selected thread.
        /// </summary>
        /// <returns></returns>
        public int GetSelected(IAsserter asserter)
        {
            int res = TryGetSelected(asserter);
            asserter.Assert(res != -1, "DFS Strategy: No selected tid entry!");
            return res;
        }

        /// <summary>
        /// Deselect the selected thread.
        /// </summary>
        public void ClearSelected(IAsserter asserter)
        {
            List[GetSelected(asserter)].Selected = false;
        }

        /// <summary>
        /// Add the first enabled and not slept thread to the backtrack set.
        /// </summary>
        /// <param name="startingFrom">a thread id to start from</param>
        /// <param name="asserter">IAsserter</param>
        public void AddFirstEnabledNotSleptToBacktrack(int startingFrom, IAsserter asserter)
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
                    asserter.Assert(List[i].Enabled);
                    return;
                }
                ++i;
                if (i >= size)
                {
                    i = 0;
                }
            }
        }
    }
}