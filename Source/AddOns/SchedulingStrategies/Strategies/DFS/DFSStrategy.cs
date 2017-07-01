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

namespace Microsoft.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// A depth-first search scheduling strategy.
    /// </summary>
    public class DFSStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Logger used by the strategy.
        /// </summary>
        protected ILogger Logger;

        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        protected int MaxScheduledSteps;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        protected int ScheduledSteps;

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
        /// Creates a DFS strategy.
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="logger">ILogger</param>
        public DFSStrategy(int maxSteps, ILogger logger)
        {
            Logger = logger;
            MaxScheduledSteps = maxSteps;
            ScheduledSteps = 0;
            SchIndex = 0;
            NondetIndex = 0;
            ScheduleStack = new List<List<SChoice>>();
            BoolNondetStack = new List<List<NondetBooleanChoice>>();
            IntNondetStack = new List<List<NondetIntegerChoice>>();
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            var enabledChoices = choices.Where(choice => choice.IsEnabled).ToList();
            if (enabledChoices.Count == 0)
            {
                next = null;
                return false;
            }

            SChoice nextChoice = null;
            List<SChoice> scs = null;

            if (SchIndex < ScheduleStack.Count)
            {
                scs = ScheduleStack[SchIndex];
            }
            else
            {
                scs = new List<SChoice>();
                foreach (var task in enabledChoices)
                {
                    scs.Add(new SChoice(task.Id));
                }

                ScheduleStack.Add(scs);
            }

            nextChoice = scs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice == null)
            {
                next = null;
                return false;
            }

            if (SchIndex > 0)
            {
                var previousChoice = ScheduleStack[SchIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }
            
            next = enabledChoices.Find(task => task.Id == nextChoice.Id);
            nextChoice.IsDone = true;
            SchIndex++;

            if (next == null)
            {
                return false;
            }

            ScheduledSteps++;

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

            if (NondetIndex < BoolNondetStack.Count)
            {
                ncs = BoolNondetStack[NondetIndex];
            }
            else
            {
                ncs = new List<NondetBooleanChoice>();
                ncs.Add(new NondetBooleanChoice(false));
                ncs.Add(new NondetBooleanChoice(true));

                BoolNondetStack.Add(ncs);
            }

            nextChoice = ncs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice == null)
            {
                next = false;
                return false;
            }

            if (NondetIndex > 0)
            {
                var previousChoice = BoolNondetStack[NondetIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = nextChoice.Value;
            nextChoice.IsDone = true;
            NondetIndex++;

            ScheduledSteps++;

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

            if (NondetIndex < IntNondetStack.Count)
            {
                ncs = IntNondetStack[NondetIndex];
            }
            else
            {
                ncs = new List<NondetIntegerChoice>();
                for (int value = 0; value < maxValue; value++)
                {
                    ncs.Add(new NondetIntegerChoice(value));
                }

                IntNondetStack.Add(ncs);
            }

            nextChoice = ncs.FirstOrDefault(val => !val.IsDone);
            if (nextChoice == null)
            {
                next = 0;
                return false;
            }

            if (NondetIndex > 0)
            {
                var previousChoice = IntNondetStack[NondetIndex - 1].Last(val => val.IsDone);
                previousChoice.IsDone = false;
            }

            next = nextChoice.Value;
            nextChoice.IsDone = true;
            NondetIndex++;

            ScheduledSteps++;

            return true;
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration</returns>
        public virtual bool PrepareForNextIteration()
        {
            if (ScheduleStack.All(scs => scs.All(val => val.IsDone)))
            {
                return false;
            }

            //PrintSchedule();

            ScheduledSteps = 0;

            SchIndex = 0;
            NondetIndex = 0;

            for (int idx = BoolNondetStack.Count - 1; idx > 0; idx--)
            {
                if (!BoolNondetStack[idx].All(val => val.IsDone))
                {
                    break;
                }

                var previousChoice = BoolNondetStack[idx - 1].First(val => !val.IsDone);
                previousChoice.IsDone = true;

                BoolNondetStack.RemoveAt(idx);
            }

            for (int idx = IntNondetStack.Count - 1; idx > 0; idx--)
            {
                if (!IntNondetStack[idx].All(val => val.IsDone))
                {
                    break;
                }

                var previousChoice = IntNondetStack[idx - 1].First(val => !val.IsDone);
                previousChoice.IsDone = true;

                IntNondetStack.RemoveAt(idx);
            }

            if (BoolNondetStack.Count > 0 &&
                BoolNondetStack.All(ns => ns.All(nsc => nsc.IsDone)))
            {
                BoolNondetStack.Clear();
            }

            if (IntNondetStack.Count > 0 &&
                IntNondetStack.All(ns => ns.All(nsc => nsc.IsDone)))
            {
                IntNondetStack.Clear();
            }

            if (BoolNondetStack.Count == 0 &&
                IntNondetStack.Count == 0)
            {
                for (int idx = ScheduleStack.Count - 1; idx > 0; idx--)
                {
                    if (!ScheduleStack[idx].All(val => val.IsDone))
                    {
                        break;
                    }

                    var previousChoice = ScheduleStack[idx - 1].First(val => !val.IsDone);
                    previousChoice.IsDone = true;

                    ScheduleStack.RemoveAt(idx);
                }
            }
            else
            {
                var previousChoice = ScheduleStack.Last().LastOrDefault(val => val.IsDone);
                if (previousChoice != null)
                {
                    previousChoice.IsDone = false;
                }
            }

            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            ScheduleStack.Clear();
            BoolNondetStack.Clear();
            IntNondetStack.Clear();
            SchIndex = 0;
            NondetIndex = 0;
            ScheduledSteps = 0;
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        /// <returns>Scheduled steps</returns>
        public int GetScheduledSteps()
        {
            return ScheduledSteps;
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            if (MaxScheduledSteps == 0)
            {
                return false;
            }

            return ScheduledSteps >= MaxScheduledSteps;
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            return false;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return "DFS";
        }

        /// <summary>
        /// Prints the schedule.
        /// </summary>
        private void PrintSchedule()
        {
            Logger.WriteLine("*******************");
            Logger.WriteLine("Schedule stack size: " + ScheduleStack.Count);
            for (int idx = 0; idx < ScheduleStack.Count; idx++)
            {
                Logger.WriteLine("Index: " + idx);
                foreach (var sc in ScheduleStack[idx])
                {
                    Logger.Write(sc.Id + " [" + sc.IsDone + "], ");
                }
                Logger.WriteLine("");
            }

            Logger.WriteLine("*******************");
            Logger.WriteLine("Random bool stack size: " + BoolNondetStack.Count);
            for (int idx = 0; idx < BoolNondetStack.Count; idx++)
            {
                Logger.WriteLine("Index: " + idx);
                foreach (var nc in BoolNondetStack[idx])
                {
                    Logger.Write(nc.Value + " [" + nc.IsDone + "], ");
                }
                Logger.WriteLine("");
            }

            Logger.WriteLine("*******************");
            Logger.WriteLine("Random int stack size: " + IntNondetStack.Count);
            for (int idx = 0; idx < IntNondetStack.Count; idx++)
            {
                Logger.WriteLine("Index: " + idx);
                foreach (var nc in IntNondetStack[idx])
                {
                    Logger.Write(nc.Value + " [" + nc.IsDone + "], ");
                }
                Logger.WriteLine("");
            }

            Logger.WriteLine("*******************");
        }

        /// <summary>
        /// A scheduling choice. Contains an id and a boolean that is
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
                Id = id;
                IsDone = false;
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
                Value = value;
                IsDone = false;
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
                Value = value;
                IsDone = false;
            }
        }
    }
}
