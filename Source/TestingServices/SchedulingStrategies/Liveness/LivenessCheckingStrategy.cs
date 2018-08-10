//-----------------------------------------------------------------------
// <copyright file="LivenessCheckingStrategy.cs">
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
using Microsoft.PSharp.TestingServices.SchedulingStrategies;

namespace Microsoft.PSharp.TestingServices.Scheduling
{
    /// <summary>
    /// Abstract strategy for detecting liveness property violations. It
    /// contains a nested <see cref="ISchedulingStrategy"/> that is used
    /// for scheduling decisions.
    /// </summary>
    internal abstract class LivenessCheckingStrategy : ISchedulingStrategy
    {
        #region fields

        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        protected List<Monitor> Monitors;

        /// <summary>
        /// Strategy used for scheduling decisions.
        /// </summary>
        protected ISchedulingStrategy SchedulingStrategy;

        #endregion

        #region public API

        /// <summary>
        /// Creates a liveness strategy that checks the specific monitors
        /// for liveness property violations, and uses the specified
        /// strategy for scheduling decisions.
        /// </summary>
        /// <param name="configuration">Configuration</param>
        /// <param name="monitors">List of monitors</param>
        /// <param name="strategy">ISchedulingStrategy</param>
        internal LivenessCheckingStrategy(Configuration configuration, List<Monitor> monitors, ISchedulingStrategy strategy)
        {
            Configuration = configuration;
            Monitors = monitors;
            SchedulingStrategy = strategy;
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public abstract bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current);

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public abstract bool GetNextBooleanChoice(int maxValue, out bool next);

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public abstract bool GetNextIntegerChoice(int maxValue, out int next);

        /// <summary>
        /// Forces the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public void ForceNext(ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration</returns>
        public virtual bool PrepareForNextIteration()
        {
            return SchedulingStrategy.PrepareForNextIteration();
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public virtual void Reset()
        {
            SchedulingStrategy.Reset();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        /// <returns>Scheduled steps</returns>
        public virtual int GetScheduledSteps()
        {
            return SchedulingStrategy.GetScheduledSteps();
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public virtual bool HasReachedMaxSchedulingSteps()
        {
            return SchedulingStrategy.HasReachedMaxSchedulingSteps();
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public virtual bool IsFair()
        {
            return SchedulingStrategy.IsFair();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public virtual string GetDescription()
        {
            return SchedulingStrategy.GetDescription();
        }

        #endregion
    }
}
