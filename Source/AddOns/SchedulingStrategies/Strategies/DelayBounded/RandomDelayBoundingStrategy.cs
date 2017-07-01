//-----------------------------------------------------------------------
// <copyright file="RandomDelayBoundingStrategy.cs">
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

namespace Microsoft.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// A randomized delay-bounding scheduling strategy.
    /// </summary>
    public sealed class RandomDelayBoundingStrategy : DelayBoundingStrategy, ISchedulingStrategy
    {
        /// <summary>
        /// Delays during this iteration.
        /// </summary>
        private List<int> CurrentIterationDelays;

        /// <summary>
        /// Creates a randomized delay-bounding strategy that uses the default
        /// random number generator (seed is based on current time).
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="maxDelays">Max number of delays</param>
        /// <param name="logger">ILogger</param>
        public RandomDelayBoundingStrategy(int maxSteps, int maxDelays, ILogger logger)
            : this(maxSteps, maxDelays, logger, new DefaultRandomNumberGenerator(DateTime.Now.Millisecond))
        { }

        /// <summary>
        /// Creates a randomized delay-bounding strategy that uses
        /// the specified random number generator.
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="maxDelays">Max number of delays</param>
        /// <param name="logger">ILogger</param>
        /// <param name="random">IRandomNumberGenerator</param>
        public RandomDelayBoundingStrategy(int maxSteps, int maxDelays, ILogger logger, IRandomNumberGenerator random)
            : base(maxSteps, maxDelays, logger, random)
        {
            CurrentIterationDelays = new List<int>();
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration</returns>
        public override bool PrepareForNextIteration()
        {
            ScheduleLength = Math.Max(ScheduleLength, ScheduledSteps);
            ScheduledSteps = 0;

            RemainingDelays.Clear();
            for (int idx = 0; idx < MaxDelays; idx++)
            {
                RemainingDelays.Add(RandomNumberGenerator.Next(ScheduleLength));
            }

            RemainingDelays.Sort();

            CurrentIterationDelays.Clear();
            CurrentIterationDelays.AddRange(RemainingDelays);

            return true;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public override string GetDescription()
        {
            var text = "Random seed '" + RandomNumberGenerator.Seed + "', '" + MaxDelays + "' delays, delays '[";
            for (int idx = 0; idx < CurrentIterationDelays.Count; idx++)
            {
                text += CurrentIterationDelays[idx];
                if (idx < CurrentIterationDelays.Count - 1)
                {
                    text += ", ";
                }
            }

            text += "]'";
            return text;
        }
    }
}
