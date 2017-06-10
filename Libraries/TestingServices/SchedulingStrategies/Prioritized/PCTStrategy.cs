//-----------------------------------------------------------------------
// <copyright file="PCTStrategy.cs">
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

using Microsoft.PSharp.IO;
using Microsoft.PSharp.Scheduling;
using Microsoft.PSharp.Utilities;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Class representing a probabilistic concurrency testing (PCT)
    /// scheduling strategy.
    /// </summary>
    public class PCTStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// The maximum number of explored steps.
        /// </summary>
        private int MaxExploredSteps;

        /// <summary>
        /// The number of explored steps.
        /// </summary>
        private int ExploredSteps;

        /// <summary>
        /// The bug depth.
        /// </summary>
        private int BugDepth;

        /// <summary>
        /// Nondeterminitic seed.
        /// </summary>
        private int Seed;

        /// <summary>
        /// Randomizer.
        /// </summary>
        private IRandomNumberGenerator Random;

        /// <summary>
        /// List of prioritized entities.
        /// </summary>
        private List<ISchedulable> PrioritizedEntities;

        /// <summary>
        /// Set of priority change points.
        /// </summary>
        private SortedSet<int> PriorityChangePoints;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="depth">Bug depth</param>
        public PCTStrategy(Configuration configuration, int depth)
        {
            this.Configuration = configuration;
            this.MaxExploredSteps = 0;
            this.ExploredSteps = 0;
            this.BugDepth = depth;
            this.Seed = this.Configuration.RandomSchedulingSeed ?? DateTime.Now.Millisecond;
            this.Random = new DefaultRandomNumberGenerator(this.Seed);
            this.PrioritizedEntities = new List<ISchedulable>();
            this.PriorityChangePoints = new SortedSet<int>();
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool TryGetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            var enabledChoices = choices.Where(choice => choice.IsEnabled).ToList();
            if (enabledChoices.Count == 0)
            {
                next = null;
                return false;
            }

            next = this.GetPrioritizedChoice(enabledChoices, current);
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
            next = false;
            if (this.Random.Next(maxValue) == 0)
            {
                next = true;
            }

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
            next = this.Random.Next(maxValue);
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
            return false;
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
            this.MaxExploredSteps = Math.Max(this.MaxExploredSteps, this.ExploredSteps);
            this.ExploredSteps = 0;

            this.PrioritizedEntities.Clear();
            this.PriorityChangePoints.Clear();

            var range = new List<int>();
            for (int idx = 0; idx < this.MaxExploredSteps; idx++)
            {
                range.Add(idx);
            }

            foreach (int point in this.Shuffle(range).Take(this.BugDepth))
            {
                this.PriorityChangePoints.Add(point);
            }
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.MaxExploredSteps = 0;
            this.ExploredSteps = 0;
            this.Random = new DefaultRandomNumberGenerator(this.Seed);
            this.PrioritizedEntities.Clear();
            this.PriorityChangePoints.Clear();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            var text = this.BugDepth + "' bug depth, priority change points '[";

            int idx = 0;
            foreach (var points in this.PriorityChangePoints)
            {
                text += points;
                if (idx < this.PriorityChangePoints.Count - 1)
                {
                    text += ", ";
                }

                idx++;
            }

            text += "]'.";
            return text;
        }

        #endregion

        #region private methods

        /// <summary>
        /// Returns the prioritized choice.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>ISchedulable</returns>
        private ISchedulable GetPrioritizedChoice(List<ISchedulable> choices, ISchedulable current)
        {
            if (this.PrioritizedEntities.Count == 0)
            {
                this.PrioritizedEntities.Add(current);
            }

            foreach (var choice in choices.Where(choice => !this.PrioritizedEntities.Contains(choice)))
            {
                var mIndex = this.Random.Next(this.PrioritizedEntities.Count) + 1;
                this.PrioritizedEntities.Insert(mIndex, choice);
                Debug.WriteLine($"<PCTLog> Detected new schedulable choice '{choice.Name}' at index '{mIndex}'.");
            }

            if (this.PriorityChangePoints.Contains(this.ExploredSteps))
            {
                if (choices.Count == 1)
                {
                    this.MovePriorityChangePointForward();
                }
                else
                {
                    var priority = this.GetHighestPriorityEnabledChoice(choices);
                    this.PrioritizedEntities.Remove(priority);
                    this.PrioritizedEntities.Add(priority);
                    Debug.WriteLine($"<PCTLog> Schedulable '{priority}' changes to lowest priority.");
                }
            }

            var prioritizedSchedulable = this.GetHighestPriorityEnabledChoice(choices);
            Debug.WriteLine($"<PCTLog> Prioritized schedulable '{prioritizedSchedulable}'.");
            Debug.Write("<PCTLog> Priority list: ");
            for (int idx = 0; idx < this.PrioritizedEntities.Count; idx++)
            {
                if (idx < this.PrioritizedEntities.Count - 1)
                {
                    Debug.Write($"'{this.PrioritizedEntities[idx]}', ");
                }
                else
                {
                    Debug.Write($"'{this.PrioritizedEntities[idx]}({1})'.\n");
                }
            }

            return choices.First(choice => choice.Equals(prioritizedSchedulable));
        }

        /// <summary>
        /// Returns the highest-priority enabled choice.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <returns>ISchedulable</returns>
        private ISchedulable GetHighestPriorityEnabledChoice(IEnumerable<ISchedulable> choices)
        {
            ISchedulable prioritizedChoice = null;
            foreach (var entity in this.PrioritizedEntities)
            {
                if (choices.Any(m => m == entity))
                {
                    prioritizedChoice = entity;
                    break;
                }
            }

            return prioritizedChoice;
        }

        /// <summary>
        /// Shuffles the specified list using the
        /// Fisher-Yates algorithm.
        /// </summary>
        /// <param name="list">IList</param>
        /// <returns>IList</returns>
        private IList<int> Shuffle(IList<int> list)
        {
            var result = new List<int>(list);
            for (int idx = result.Count - 1; idx >= 1; idx--)
            {
                int point = this.Random.Next(this.MaxExploredSteps);
                int temp = result[idx];
                result[idx] = result[point];
                result[point] = temp;
            }

            return result;
        }

        /// <summary>
        /// Moves the current priority change point forward. This is a useful
        /// optimization when a priority change point is assigned in either a
        /// sequential execution or a nondeterministic choice.
        /// </summary>
        private void MovePriorityChangePointForward()
        {
            this.PriorityChangePoints.Remove(this.ExploredSteps);
            var newPriorityChangePoint = this.ExploredSteps + 1;
            while (this.PriorityChangePoints.Contains(newPriorityChangePoint))
            {
                newPriorityChangePoint++;
            }

            this.PriorityChangePoints.Add(newPriorityChangePoint);
            Debug.WriteLine($"<PCTLog> Moving priority change to '{newPriorityChangePoint}'.");
        }

        #endregion
    }
}
