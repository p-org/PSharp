//-----------------------------------------------------------------------
// <copyright file="ExhaustiveDelayBoundingStrategy.cs">
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

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// An exhaustive delay-bounding scheduling strategy.
    /// </summary>
    public sealed class ExhaustiveDelayBoundingStrategy : DelayBoundingStrategy, ISchedulingStrategy
    {
        /// <summary>
        /// Cache of delays across iterations.
        /// </summary>
        private List<int> DelaysCache;

        /// <summary>
        /// Creates an exhaustive delay-bounding strategy that uses the default
        /// random number generator (seed is based on current time).
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="maxDelays">Max number of delays</param>
        /// <param name="logger">ILogger</param>
        public ExhaustiveDelayBoundingStrategy(int maxSteps, int maxDelays, ILogger logger)
            : this(maxSteps, maxDelays, logger, new DefaultRandomNumberGenerator(DateTime.Now.Millisecond))
        { }

        /// <summary>
        /// Creates an exhaustive delay-bounding strategy that uses
        /// the specified random number generator.
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="maxDelays">Max number of delays</param>
        /// <param name="logger">ILogger</param>
        /// <param name="random">IRandomNumberGenerator</param>
        public ExhaustiveDelayBoundingStrategy(int maxSteps, int maxDelays, ILogger logger, IRandomNumberGenerator random)
            : base(maxSteps, maxDelays, logger, random)
        {
            DelaysCache = Enumerable.Repeat(0, MaxDelays).ToList();
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

            var bound = Math.Min(MaxScheduledSteps, ScheduleLength);
            for (var idx = 0; idx < MaxDelays; idx++)
            {
                if (DelaysCache[idx] < bound)
                {
                    DelaysCache[idx] = DelaysCache[idx] + 1;
                    break;
                }

                DelaysCache[idx] = 0;
            }

            RemainingDelays.Clear();
            RemainingDelays.AddRange(DelaysCache);
            RemainingDelays.Sort();

            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public override void Reset()
        {
            DelaysCache = Enumerable.Repeat(0, MaxDelays).ToList();
            base.Reset();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public override string GetDescription()
        {
            var text = MaxDelays + "' delays, delays '[";
            for (int idx = 0; idx < DelaysCache.Count; idx++)
            {
                text += DelaysCache[idx];
                if (idx < DelaysCache.Count - 1)
                {
                    text += ", ";
                }
            }

            text += "]'";
            return text;
        }
    }
}
