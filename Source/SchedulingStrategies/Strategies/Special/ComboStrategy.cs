﻿// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.PSharp.TestingServices.SchedulingStrategies
{
    /// <summary>
    /// This strategy combines two given strategies, using them to schedule
    /// the prefix and suffix of an execution.
    /// </summary>
    public sealed class ComboStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The prefix strategy.
        /// </summary>
        private ISchedulingStrategy PrefixStrategy;

        /// <summary>
        /// The suffix strategy.
        /// </summary>
        private ISchedulingStrategy SuffixStrategy;

        /// <summary>
        /// Creates a combo strategy that uses the two specified strategies.
        /// </summary>
        /// <param name="prefixStrategy">Prefix strategy </param>
        /// <param name="suffixStrategy">Suffix strategy</param>
        public ComboStrategy(ISchedulingStrategy prefixStrategy, ISchedulingStrategy suffixStrategy)
        {
            this.PrefixStrategy = prefixStrategy;
            this.SuffixStrategy = suffixStrategy;
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return this.SuffixStrategy.GetNext(out next, choices, current);
            }
            else
            {
                return this.PrefixStrategy.GetNext(out next, choices, current);
            }
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return this.SuffixStrategy.GetNextBooleanChoice(maxValue, out next);
            }
            else
            {
                return this.PrefixStrategy.GetNextBooleanChoice(maxValue, out next);
            }
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return this.SuffixStrategy.GetNextIntegerChoice(maxValue, out next);
            }
            else
            {
                return this.PrefixStrategy.GetNextIntegerChoice(maxValue, out next);
            }
        }

        /// <summary>
        /// Forces the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public void ForceNext(ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                this.SuffixStrategy.ForceNext(next, choices, current);
            }
            else
            {
                this.PrefixStrategy.ForceNext(next, choices, current);
            }
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                this.SuffixStrategy.ForceNextBooleanChoice(maxValue, next);
            }
            else
            {
                this.PrefixStrategy.ForceNextBooleanChoice(maxValue, next);
            }
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                this.SuffixStrategy.ForceNextIntegerChoice(maxValue, next);
            }
            else
            {
                this.PrefixStrategy.ForceNextIntegerChoice(maxValue, next);
            }
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration</returns>
        public bool PrepareForNextIteration()
        {
            bool doNext = this.PrefixStrategy.PrepareForNextIteration();
            doNext = doNext | this.SuffixStrategy.PrepareForNextIteration();
            return doNext;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            this.PrefixStrategy.Reset();
            this.SuffixStrategy.Reset();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        /// <returns>Scheduled steps</returns>
        public int GetScheduledSteps()
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return this.SuffixStrategy.GetScheduledSteps() + this.PrefixStrategy.GetScheduledSteps();
            }
            else
            {
                return this.PrefixStrategy.GetScheduledSteps();
            }
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            return this.SuffixStrategy.HasReachedMaxSchedulingSteps();
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            return this.SuffixStrategy.IsFair();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return string.Format("Combo[{0},{1}]", PrefixStrategy.GetDescription(), SuffixStrategy.GetDescription());
        }
    }
}
