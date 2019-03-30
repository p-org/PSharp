// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
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
        /// Initializes a new instance of the <see cref="RandomStrategy"/> class.
        /// It uses the default random number generator (seed is based on current time).
        /// </summary>
        public RandomStrategy(int maxSteps)
            : this(maxSteps, new DefaultRandomNumberGenerator(DateTime.Now.Millisecond))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public RandomStrategy(int maxSteps, IRandomNumberGenerator random)
        {
            this.RandomNumberGenerator = random;
            this.MaxScheduledSteps = maxSteps;
            this.ScheduledSteps = 0;
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        public virtual bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            var enabledChoices = choices.Where(choice => choice.IsEnabled).ToList();
            if (enabledChoices.Count == 0)
            {
                next = null;
                return false;
            }

            int idx = this.RandomNumberGenerator.Next(enabledChoices.Count);
            next = enabledChoices[idx];

            this.ScheduledSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public virtual bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            next = false;
            if (this.RandomNumberGenerator.Next(maxValue) == 0)
            {
                next = true;
            }

            this.ScheduledSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public virtual bool GetNextIntegerChoice(int maxValue, out int next)
        {
            next = this.RandomNumberGenerator.Next(maxValue);
            this.ScheduledSteps++;
            return true;
        }

        /// <summary>
        /// Forces the next entity to be scheduled.
        /// </summary>
        public void ForceNext(ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            this.ScheduledSteps++;
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            this.ScheduledSteps++;
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            this.ScheduledSteps++;
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        public virtual bool PrepareForNextIteration()
        {
            this.ScheduledSteps = 0;
            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public virtual void Reset()
        {
            this.ScheduledSteps = 0;
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        public int GetScheduledSteps() => this.ScheduledSteps;

        /// <summary>
        /// True if the scheduling strategy has reached the depth
        /// bound for the given scheduling iteration.
        /// </summary>
        public bool HasReachedMaxSchedulingSteps()
        {
            if (this.MaxScheduledSteps == 0)
            {
                return false;
            }

            return this.ScheduledSteps >= this.MaxScheduledSteps;
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        public bool IsFair() => true;

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public virtual string GetDescription() => $"Random[seed '{this.RandomNumberGenerator.Seed}']";
    }
}
