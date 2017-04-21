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

using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.TestingServices.Scheduling
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
        private List<List<NondetBooleanChoice>> BoolNondetStack;

        /// <summary>
        /// Stack of nondeterministic choices.
        /// </summary>
        private List<List<NondetIntegerChoice>> IntNondetStack;

        /// <summary>
        /// Current schedule index.
        /// </summary>
        private int SchIndex;

        /// <summary>
        /// Current nondeterministic index.
        /// </summary>
        private int NondetIndex;

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
            this.BoolNondetStack = new List<List<NondetBooleanChoice>>();
            this.IntNondetStack = new List<List<NondetIntegerChoice>>();
            this.SchIndex = 0;
            this.NondetIndex = 0;
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Returns the next machine to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool TryGetNext(out MachineInfo next, IEnumerable<MachineInfo> choices, MachineInfo current)
        {
            var availableMachines = choices.Where(
                m => m.IsEnabled && !m.IsWaitingToReceive).ToList();
            if (availableMachines.Count == 0)
            {
                availableMachines = choices.Where(m => m.IsWaitingToReceive).ToList();
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
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            NondetBooleanChoice nextChoice = null;
            List<NondetBooleanChoice> ncs = null;

            if (this.NondetIndex < this.BoolNondetStack.Count)
            {
                ncs = this.BoolNondetStack[this.NondetIndex];
            }
            else
            {
                ncs = new List<NondetBooleanChoice>();
                ncs.Add(new NondetBooleanChoice(false));
                ncs.Add(new NondetBooleanChoice(true));

                this.BoolNondetStack.Add(ncs);
            }

            nextChoice = ncs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice == null)
            {
                next = false;
                return false;
            }

            if (this.NondetIndex > 0)
            {
                var previousChoice = this.BoolNondetStack[this.NondetIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = nextChoice.Value;
            nextChoice.IsDone = true;
            this.NondetIndex++;

            this.ExploredSteps++;

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
            NondetIntegerChoice nextChoice = null;
            List<NondetIntegerChoice> ncs = null;

            if (this.NondetIndex < this.IntNondetStack.Count)
            {
                ncs = this.IntNondetStack[this.NondetIndex];
            }
            else
            {
                ncs = new List<NondetIntegerChoice>();
                for (int value = 0; value < maxValue; value++)
                {
                    ncs.Add(new NondetIntegerChoice(value));
                }

                this.IntNondetStack.Add(ncs);
            }

            nextChoice = ncs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice == null)
            {
                next = 0;
                return false;
            }

            if (this.NondetIndex > 0)
            {
                var previousChoice = this.IntNondetStack[this.NondetIndex - 1].Last(val => val.IsDone);
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
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            var bound = (this.IsFair() ? this.Configuration.MaxFairSchedulingSteps :
                this.Configuration.MaxUnfairSchedulingSteps);

            if (bound == 0)
            {
                return false;
            }

            return this.ExploredSteps >= bound;
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
        /// Checks if this a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            return false;
        }

        /// <summary>
        /// Configures the next scheduling iteration.
        /// </summary>
        public void ConfigureNextIteration()
        {
            //this.PrintSchedule();

            this.ExploredSteps = 0;

            this.SchIndex = 0;
            this.NondetIndex = 0;

            for (int idx = this.BoolNondetStack.Count - 1; idx > 0; idx--)
            {
                if (!this.BoolNondetStack[idx].All(val => val.IsDone))
                {
                    break;
                }

                var previousChoice = this.BoolNondetStack[idx - 1].First(val => !val.IsDone);
                previousChoice.IsDone = true;

                this.BoolNondetStack.RemoveAt(idx);
            }

            for (int idx = this.IntNondetStack.Count - 1; idx > 0; idx--)
            {
                if (!this.IntNondetStack[idx].All(val => val.IsDone))
                {
                    break;
                }

                var previousChoice = this.IntNondetStack[idx - 1].First(val => !val.IsDone);
                previousChoice.IsDone = true;

                this.IntNondetStack.RemoveAt(idx);
            }

            if (this.BoolNondetStack.Count > 0 &&
                this.BoolNondetStack.All(ns => ns.All(nsc => nsc.IsDone)))
            {
                this.BoolNondetStack.Clear();
            }

            if (this.IntNondetStack.Count > 0 &&
                this.IntNondetStack.All(ns => ns.All(nsc => nsc.IsDone)))
            {
                this.IntNondetStack.Clear();
            }

            if (this.BoolNondetStack.Count == 0 &&
                this.IntNondetStack.Count == 0)
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
            this.BoolNondetStack.Clear();
            this.IntNondetStack.Clear();
            this.SchIndex = 0;
            this.NondetIndex = 0;
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
            Debug.WriteLine("*******************");
            Debug.WriteLine("Schedule stack size: " + this.ScheduleStack.Count);
            for (int idx = 0; idx < this.ScheduleStack.Count; idx++)
            {
                Debug.WriteLine("Index: " + idx);
                foreach (var sc in this.ScheduleStack[idx])
                {
                    Debug.Write(sc.Id + " [" + sc.IsDone + "], ");
                }
                Debug.WriteLine("");
            }

            Debug.WriteLine("*******************");
            Debug.WriteLine("Random bool stack size: " + this.BoolNondetStack.Count);
            for (int idx = 0; idx < this.BoolNondetStack.Count; idx++)
            {
                Debug.WriteLine("Index: " + idx);
                foreach (var nc in this.BoolNondetStack[idx])
                {
                    Debug.Write(nc.Value + " [" + nc.IsDone + "], ");
                }
                Debug.WriteLine("");
            }
            Debug.WriteLine("*******************");

            Debug.WriteLine("*******************");
            Debug.WriteLine("Random int stack size: " + this.IntNondetStack.Count);
            for (int idx = 0; idx < this.IntNondetStack.Count; idx++)
            {
                Debug.WriteLine("Index: " + idx);
                foreach (var nc in this.IntNondetStack[idx])
                {
                    Debug.Write(nc.Value + " [" + nc.IsDone + "], ");
                }
                Debug.WriteLine("");
            }
            Debug.WriteLine("*******************");
        }

        /// <summary>
        /// A scheduling choice. Contains a machine id and a boolean that is
        /// true if the choice has been previously explored.
        /// </summary>
        private class SChoice
        {
            internal ulong Id;
            internal bool IsDone;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="id">Id</param>
            internal SChoice(ulong id)
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
        private class NondetBooleanChoice
        {
            internal bool Value;
            internal bool IsDone;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="value">Value</param>
            internal NondetBooleanChoice(bool value)
            {
                this.Value = value;
                this.IsDone = false;
            }
        }

        /// <summary>
        /// A nondeterministic choice. Contains an integer value that
        /// corresponds to the choice and a boolean that is true if
        /// the choice has been previously explored.
        /// </summary>
        private class NondetIntegerChoice
        {
            internal int Value;
            internal bool IsDone;

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="value">Value</param>
            internal NondetIntegerChoice(int value)
            {
                this.Value = value;
                this.IsDone = false;
            }
        }

        #endregion
    }
}
