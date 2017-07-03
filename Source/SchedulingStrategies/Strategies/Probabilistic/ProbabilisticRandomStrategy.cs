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

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// A randomized scheduling strategy with increased probability
    /// to remain in the same scheduling choice.
    /// </summary>
    public sealed class ProbabilisticRandomStrategy : RandomStrategy
    {
        /// <summary>
        /// Number of coin flips.
        /// </summary>
        private int NumberOfCoinFlips;

        /// <summary>
        /// Creates a random strategy that uses the default random
        /// number generator (seed is based on current time).
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="numberOfCoinFlips">Number of coin flips</param>
        public ProbabilisticRandomStrategy(int maxSteps, int numberOfCoinFlips)
            : base(maxSteps)
        {
            NumberOfCoinFlips = numberOfCoinFlips;
        }

        /// <summary>
        /// Creates a random strategy that uses the specified random number generator.
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="numberOfCoinFlips">Number of coin flips</param>
        /// <param name="random">IRandomNumberGenerator</param>
        public ProbabilisticRandomStrategy(int maxSteps, int numberOfCoinFlips, IRandomNumberGenerator random)
            : base(maxSteps, random)
        {
            NumberOfCoinFlips = numberOfCoinFlips;
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public override bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            var enabledChoices = choices.Where(choice => choice.IsEnabled).ToList();
            if (enabledChoices.Count == 0)
            {
                next = null;
                return false;
            }

            ScheduledSteps++;

            if (enabledChoices.Count > 1)
            {
                if (!ShouldCurrentMachineChange() && current.IsEnabled)
                {
                    next = current;
                    return true;
                }
            }

            int idx = RandomNumberGenerator.Next(enabledChoices.Count);
            next = enabledChoices[idx];
            
            return true;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public override string GetDescription()
        {
            return $"ProbabilisticRandom[seed '{RandomNumberGenerator.Seed}', coin flips '{NumberOfCoinFlips}']";
        }

        /// <summary>
        /// Flip the coin a specified number of times.
        /// </summary>
        /// <returns>Boolean</returns>
        private bool ShouldCurrentMachineChange()
        {
            for (int idx = 0; idx < NumberOfCoinFlips; idx++)
            {
                if (RandomNumberGenerator.Next(2) == 1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
