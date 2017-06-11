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

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Class representing a random delay-bounding scheduling strategy.
    /// </summary>
    public sealed class RandomDelayBoundingStrategy : DelayBoundingStrategy, ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// Delays during this iteration.
        /// </summary>
        private List<int> CurrentIterationDelays;

        #endregion

        #region public API

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="delays">Max number of delays</param>
        public RandomDelayBoundingStrategy(Configuration configuration, int delays)
            : base(configuration, delays)
        {
            this.CurrentIterationDelays = new List<int>();
        }

        /// <summary>
        /// Prepares the next scheduling iteration.
        /// </summary>
        /// <returns>False if all schedules have been explored</returns>
        public override bool PrepareForNextIteration()
        {
            base.MaxExploredSteps = Math.Max(base.MaxExploredSteps, base.ExploredSteps);
            base.ExploredSteps = 0;

            base.RemainingDelays.Clear();
            for (int idx = 0; idx < base.MaxDelays; idx++)
            {
                base.RemainingDelays.Add(base.Random.Next(base.MaxExploredSteps));
            }

            base.RemainingDelays.Sort();

            this.CurrentIterationDelays.Clear();
            this.CurrentIterationDelays.AddRange(base.RemainingDelays);

            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public override string GetDescription()
        {
            var text = "Random seed '" + base.Seed + "', '" + base.MaxDelays + "' delays, delays '[";
            for (int idx = 0; idx < this.CurrentIterationDelays.Count; idx++)
            {
                text += this.CurrentIterationDelays[idx];
                if (idx < this.CurrentIterationDelays.Count - 1)
                {
                    text += ", ";
                }
            }

            text += "]'.";
            return text;
        }

        #endregion
    }
}
