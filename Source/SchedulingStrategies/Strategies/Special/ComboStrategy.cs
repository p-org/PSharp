// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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
        private readonly ISchedulingStrategy PrefixStrategy;

        /// <summary>
        /// The suffix strategy.
        /// </summary>
        private readonly ISchedulingStrategy SuffixStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComboStrategy"/> class.
        /// </summary>
        public ComboStrategy(ISchedulingStrategy prefixStrategy, ISchedulingStrategy suffixStrategy)
        {
            this.PrefixStrategy = prefixStrategy;
            this.SuffixStrategy = suffixStrategy;
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
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
        /// Forces the next entity to be scheduled.
        /// </summary>
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
        public bool HasReachedMaxSchedulingSteps() => this.SuffixStrategy.HasReachedMaxSchedulingSteps();

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        public bool IsFair() => this.SuffixStrategy.IsFair();

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public string GetDescription() =>
            string.Format("Combo[{0},{1}]", this.PrefixStrategy.GetDescription(), this.SuffixStrategy.GetDescription());
    }
}
