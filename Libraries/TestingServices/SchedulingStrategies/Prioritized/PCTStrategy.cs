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
        /// List of prioritized machines.
        /// </summary>
        private List<MachineId> PrioritizedMachines;

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
            this.PrioritizedMachines = new List<MachineId>();
            this.PriorityChangePoints = new SortedSet<int>();
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
                mi => mi.IsEnabled && !mi.IsBlocked && !mi.IsWaitingToReceive).ToList();
            if (availableMachines.Count == 0)
            {
                availableMachines = choices.Where(m => m.IsWaitingToReceive).ToList();
                if (availableMachines.Count == 0)
                {
                    next = null;
                    return false;
                }
            }

            next = this.GetPrioritizedMachine(availableMachines, current);
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

            this.PrioritizedMachines.Clear();
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
            this.PrioritizedMachines.Clear();
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
        /// Returns the prioritized machine.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>MachineInfo</returns>
        private MachineInfo GetPrioritizedMachine(List<MachineInfo> choices, MachineInfo current)
        {
            if (this.PrioritizedMachines.Count == 0)
            {
                this.PrioritizedMachines.Add(current.Machine.Id);
            }

            foreach (var mi in choices.Where(mi => !this.PrioritizedMachines.Contains(mi.Machine.Id)))
            {
                var mIndex = this.Random.Next(this.PrioritizedMachines.Count) + 1;
                this.PrioritizedMachines.Insert(mIndex, mi.Machine.Id);
                IO.Debug($"<PCTDebug> Detected new machine '{mi.Machine.Id}' at index '{mIndex}'.");
            }

            if (this.PriorityChangePoints.Contains(this.ExploredSteps))
            {
                if (choices.Count == 1)
                {
                    this.MovePriorityChangePointForward();
                }
                else
                {
                    var priority = this.GetHighestPriorityEnabledMachine(choices);
                    this.PrioritizedMachines.Remove(priority);
                    this.PrioritizedMachines.Add(priority);
                    IO.PrintLine($"<PCTLog> Machine '{priority}' changes to lowest priority.");
                }
            }

            var prioritizedMachine = this.GetHighestPriorityEnabledMachine(choices);
            IO.Debug($"<PCTDebug> Prioritized machine '{prioritizedMachine}'.");
            if (IO.Debugging)
            {
                IO.Print("<PCTDebug> Priority list: ");
                for (int idx = 0; idx < this.PrioritizedMachines.Count; idx++)
                {
                    if (idx < this.PrioritizedMachines.Count - 1)
                    {
                        IO.Print($"'{this.PrioritizedMachines[idx]}', ");
                    }
                    else
                    {
                        IO.Print($"'{this.PrioritizedMachines[idx]}({1})'.\n");
                    }
                }
            }

            var prioritizedMachineInfo = choices.First(mi => mi.Machine.Id.Equals(prioritizedMachine));
            return prioritizedMachineInfo;
        }

        /// <summary>
        /// Returns the highest-priority enabled machine.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <returns>MachineId</returns>
        private MachineId GetHighestPriorityEnabledMachine(IEnumerable<MachineInfo> choices)
        {
            MachineId prioritizedMachine = null;
            foreach (var mid in this.PrioritizedMachines)
            {
                if (choices.Any(m => m.Machine.Id == mid))
                {
                    prioritizedMachine = mid;
                    break;
                }
            }

            return prioritizedMachine;
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
            IO.Debug($"<PCTDebug> Moving priority change to '{newPriorityChangePoint}'.");
        }

        #endregion
    }
}
