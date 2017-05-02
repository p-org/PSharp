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
    public sealed class ProbabilisticRandomStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        private Configuration Configuration;

        /// <summary>
        /// Nondeterminitic seed.
        /// </summary>
        private int Seed;

        /// <summary>
        /// Randomizer.
        /// </summary>
        private IRandomNumberGenerator Random;

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
            this.Random = new DefaultRandomNumberGenerator(this.Seed);
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

            this.ExploredSteps++;

            if (enabledChoices.Count > 1)
            {
                if (!this.ShouldCurrentMachineChange() && current.IsEnabled)
                {
                    next = current;
                    return true;
                }
            }

            int idx = this.Random.Next(enabledChoices.Count);
            next = enabledChoices[idx];
            
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
        /// Checks if this a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            return true;
        }

        /// <summary>
        /// Prepares the next scheduling iteration.
        /// </summary>
        /// <returns>False if all schedules have been explored</returns>
        public bool PrepareForNextIteration()
        {
            this.ExploredSteps = 0;
            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public void Reset()
        {
            this.ExploredSteps = 0;
            this.Random = new DefaultRandomNumberGenerator(this.Seed);
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return "Random seed '" + this.Seed + "'.";
        }

        #endregion
    }
}
