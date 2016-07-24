//-----------------------------------------------------------------------
// <copyright file="ProbabilisticRandomStrategy.cs">
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
    /// Class representing a probabilistic random-walk
    /// scheduling strategy.
    /// </summary>
    public class ProbabilisticRandomStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// Nondeterminitic seed.
        /// </summary>
        private int Seed;

        /// <summary>
        /// Randomizer.
        /// </summary>
        private Random Random;

        /// <summary>
        /// Number of coin flips.
        /// </summary>
        private int NumberOfCoinFlips;

        /// <summary>
        /// The number of explored steps.
        /// </summary>
        private int ExploredSteps;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="numberOfCoinFlips">Number of coin flips</param>
        public ProbabilisticRandomStrategy(Configuration configuration, int numberOfCoinFlips)
        {
            this.Configuration = configuration;
            this.Seed = this.Configuration.RandomSchedulingSeed ?? DateTime.Now.Millisecond;
            this.Random = new Random(this.Seed);
            this.NumberOfCoinFlips = numberOfCoinFlips;
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Flip the coin a specified number of times.
        /// </summary>
        /// <returns>Boolean</returns>
        private bool ShouldCurrentMachineChange()
        {
            for (int idx = 0; idx < this.NumberOfCoinFlips; idx++)
            {
                if (this.Random.Next(2) == 1)
                {
                    return false;
                }
            }

            return true;
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
                m => m.IsEnabled && !m.IsBlocked && !m.IsWaitingToReceive).ToList();
            if (availableMachines.Count == 0)
            {
                availableMachines = choices.Where(m => m.IsWaitingToReceive).ToList();
                if (availableMachines.Count == 0)
                {
                    next = null;
                    return false;
                }
            }

            this.ExploredSteps++;

            if (availableMachines.Count > 1)
            {
                if (!this.ShouldCurrentMachineChange() &&
                    current.IsEnabled && !current.IsBlocked && !current.IsWaitingToReceive)
                {
                    next = current;
                    return true;
                }
            }

            int idx = this.Random.Next(availableMachines.Count);
            next = availableMachines[idx];
            
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
            if (this.Configuration.MaxSchedulingSteps == 0)
            {
                return false;
            }
            
            return this.ExploredSteps == this.Configuration.MaxSchedulingSteps;
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
        /// Configures the next scheduling iteration.
        /// </summary>
        public void ConfigureNextIteration()
        {
            this.ExploredSteps = 0;
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.ExploredSteps = 0;
            this.Random = new Random(this.Seed);
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return "Random seed '" + this.Seed + "'.";
        }

        /// <summary>
        /// Is this a fair scheduler?
        /// </summary>
        public bool IsFair()
        {
            return true;
        }

        #endregion
    }
}
