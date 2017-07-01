//-----------------------------------------------------------------------
// <copyright file="RandomStrategy.cs">
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

namespace Microsoft.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// A simple (but effective) randomized scheduling strategy.
    /// </summary>
    public class RandomStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Random number generator.
        /// </summary>
        protected IRandomNumberGenerator RandomNumberGenerator;

        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        protected int MaxScheduledSteps;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        protected int ScheduledSteps;

        /// <summary>
        /// Creates a random strategy that uses the default random
        /// number generator (seed is based on current time).
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        public RandomStrategy(int maxSteps)
            : this(maxSteps, new DefaultRandomNumberGenerator(DateTime.Now.Millisecond))
        { }

        /// <summary>
        /// Creates a random strategy that uses the specified random number generator.
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="random">IRandomNumberGenerator</param>
        public RandomStrategy(int maxSteps, IRandomNumberGenerator random)
        {
            RandomNumberGenerator = random;
            MaxScheduledSteps = maxSteps;
            ScheduledSteps = 0;
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public virtual bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            var enabledChoices = choices.Where(choice => choice.IsEnabled).ToList();
            if (enabledChoices.Count == 0)
            {
                next = null;
                return false;
            }

            int idx = RandomNumberGenerator.Next(enabledChoices.Count);
            next = enabledChoices[idx];

            ScheduledSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public virtual bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            next = false;
            if (RandomNumberGenerator.Next(maxValue) == 0)
            {
                next = true;
            }

            ScheduledSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public virtual bool GetNextIntegerChoice(int maxValue, out int next)
        {
            next = RandomNumberGenerator.Next(maxValue);
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
            ScheduledSteps = 0;
            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public virtual void Reset()
        {
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
        /// True if the scheduling strategy has reached the depth
        /// bound for the given scheduling iteration.
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
            return true;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public virtual string GetDescription()
        {
            return $"Random[seed '{RandomNumberGenerator.Seed}']";
        }
    }
}
