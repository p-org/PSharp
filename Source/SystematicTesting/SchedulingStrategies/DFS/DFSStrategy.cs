//-----------------------------------------------------------------------
// <copyright file="DFSStrategy.cs">
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

using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.SystematicTesting.Scheduling
{
    /// <summary>
    /// Class representing a depth-first search scheduling strategy.
    /// </summary>
    public class DFSStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// Stack of scheduling choices.
        /// </summary>
        private List<List<SChoice>> ScheduleStack;

        /// <summary>
        /// Stack of nondeterministic choices.
        /// </summary>
        private List<List<NondetChoice>> NondetStack;

        /// <summary>
        /// Current schedule index.
        /// </summary>
        private int SchIndex;

        /// <summary>
        /// Current nondeterministic index.
        /// </summary>
        private int NondetIndex;

        /// <summary>
        /// The maximum number of explored steps.
        /// </summary>
        private int MaxExploredSteps;

        /// <summary>
        /// The number of explored steps.
        /// </summary>
        protected int ExploredSteps;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        public DFSStrategy(Configuration configuration)
        {
            this.Configuration = configuration;
            this.ScheduleStack = new List<List<SChoice>>();
            this.NondetStack = new List<List<NondetChoice>>();
            this.SchIndex = 0;
            this.NondetIndex = 0;
            this.MaxExploredSteps = 0;
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool TryGetNext(out MachineInfo next, IList<MachineInfo> choices, MachineInfo current)
        {
            var availableMachines = choices.Where(
                m => m.IsEnabled && !m.IsBlocked && !m.IsWaiting).ToList();
            if (availableMachines.Count == 0)
            {
                availableMachines = choices.Where(m => m.IsWaiting).ToList();
                if (availableMachines.Count == 0)
                {
                    next = null;
                    return false;
                }
            }

            SChoice nextChoice = null;
            List<SChoice> scs = null;

            if (this.SchIndex < this.ScheduleStack.Count)
            {
                scs = this.ScheduleStack[this.SchIndex];
            }
            else
            {
                scs = new List<SChoice>();
                foreach (var task in availableMachines)
                {
                    scs.Add(new SChoice(task.Machine.Id.Value));
                }

                this.ScheduleStack.Add(scs);
            }

            nextChoice = scs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice == null)
            {
                next = null;
                return false;
            }

            if (this.SchIndex > 0)
            {
                var previousChoice = this.ScheduleStack[this.SchIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }
            
            next = availableMachines.Find(task => task.Machine.Id.Value == nextChoice.Id);
            nextChoice.IsDone = true;
            this.SchIndex++;

            if (next == null)
            {
                return false;
            }

            this.ExploredSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextChoice(int maxValue, out bool next)
        {
            NondetChoice nextChoice = null;
            List<NondetChoice> ncs = null;

            if (this.NondetIndex < this.NondetStack.Count)
            {
                ncs = this.NondetStack[this.NondetIndex];
            }
            else
            {
                ncs = new List<NondetChoice>();
                ncs.Add(new NondetChoice(false));
                ncs.Add(new NondetChoice(true));

                this.NondetStack.Add(ncs);
            }

            nextChoice = ncs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice == null)
            {
                next = false;
                return false;
            }

            if (this.NondetIndex > 0)
            {
                var previousChoice = this.NondetStack[this.NondetIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = nextChoice.Value;
            nextChoice.IsDone = true;
            this.NondetIndex++;

            this.ExploredSteps++;

            return true;
        }

        /// <summary>
        /// Returns the explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetExploredSteps()
        {
            return this.ExploredSteps;
        }

        /// <summary>
        /// Returns the maximum explored steps.
        /// </summary>
        /// <returns>Explored steps</returns>
        public int GetMaxExploredSteps()
        {
            return this.MaxExploredSteps;
        }

        /// <summary>  
        /// Returns the depth bound.
        /// </summary> 
        /// <returns>Depth bound</returns>  
        public int GetDepthBound()
        {
            return this.Configuration.DepthBound;
        }

        /// <summary>
        /// True if the scheduling strategy reached the depth bound
        /// for the given scheduling iteration.
        /// </summary>
        /// <returns>Depth bound</returns>
        public bool HasReachedDepthBound()
        {
            if (this.GetDepthBound() == 0)
            {
                return false;
            }

            return this.ExploredSteps == this.GetDepthBound();
        }

        /// <summary>
        /// Returns true if the scheduling has finished.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasFinished()
        {
            return this.ScheduleStack.All(scs => scs.All(val => val.IsDone));
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public void ConfigureNextIteration()
        {
            //this.PrintSchedule();

            this.MaxExploredSteps = Math.Max(this.MaxExploredSteps, this.ExploredSteps);
            this.ExploredSteps = 0;

            this.SchIndex = 0;
            this.NondetIndex = 0;

            for (int idx = this.NondetStack.Count - 1; idx > 0; idx--)
            {
                if (!this.NondetStack[idx].All(val => val.IsDone))
                {
                    break;
                }

                var previousChoice = this.NondetStack[idx - 1].First(val => !val.IsDone);
                previousChoice.IsDone = true;

                this.NondetStack.RemoveAt(idx);
            }

            if (this.NondetStack.Count > 0 &&
                this.NondetStack.All(ns => ns.All(nsc => nsc.IsDone)))
            {
                this.NondetStack.Clear();
            }

            if (this.NondetStack.Count == 0)
            {
                for (int idx = this.ScheduleStack.Count - 1; idx > 0; idx--)
                {
                    if (!this.ScheduleStack[idx].All(val => val.IsDone))
                    {
                        break;
                    }

                    var previousChoice = this.ScheduleStack[idx - 1].First(val => !val.IsDone);
                    previousChoice.IsDone = true;

                    this.ScheduleStack.RemoveAt(idx);
                }
            }
            else
            {
                var previousChoice = this.ScheduleStack.Last().LastOrDefault(val => val.IsDone);
                if (previousChoice != null)
                {
                    previousChoice.IsDone = false;
                }
            }
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.ScheduleStack.Clear();
            this.NondetStack.Clear();
            this.SchIndex = 0;
            this.NondetIndex = 0;
            this.MaxExploredSteps = 0;
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return "";
        }

        #endregion

        #region private methods

        /// <summary>
        /// Prints the schedule.
        /// </summary>
        private void PrintSchedule()
        {
            IO.PrintLine("*******************");
            IO.PrintLine("Schedule stack size: " + this.ScheduleStack.Count);
            for (int idx = 0; idx < this.ScheduleStack.Count; idx++)
            {
                IO.PrintLine("Index: " + idx);
                foreach (var sc in this.ScheduleStack[idx])
                {
                    IO.Print(sc.Id + " [" + sc.IsDone + "], ");
                }
                IO.PrintLine("");
            }

            IO.PrintLine("*******************");
            IO.PrintLine("Random stack size: " + this.NondetStack.Count);
            for (int idx = 0; idx < this.NondetStack.Count; idx++)
            {
                IO.PrintLine("Index: " + idx);
                foreach (var nc in this.NondetStack[idx])
                {
                    IO.Print(nc.Value + " [" + nc.IsDone + "], ");
                }
                IO.PrintLine("");
            }
            IO.PrintLine("*******************");
        }

        /// <summary>
        /// A scheduling choice. Contains a machine id and a boolean that is
        /// true if the choice has been previously explored.
        /// </summary>
        private class SChoice
        {
            internal int Id;
            internal bool IsDone;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="id">Id</param>
            internal SChoice(int id)
            {
                this.Id = id;
                this.IsDone = false;
            }
        }

        /// <summary>
        /// A nondeterministic choice. Contains a boolean value that
        /// corresponds to the choice and a boolean that is true if
        /// the choice has been previously explored.
        /// </summary>
        private class NondetChoice
        {
            internal bool Value;
            internal bool IsDone;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="value">Value</param>
            internal NondetChoice(bool value)
            {
                this.Value = value;
                this.IsDone = false;
            }
        }

        #endregion
    }
}
