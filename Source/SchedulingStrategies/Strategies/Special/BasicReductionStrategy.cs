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
    /// This strategy uses basic partial-order reduction to reduce
    /// the choice-space for a provided child strategy.
    /// </summary>
    public sealed class BasicReductionStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Type of reduction strategy.
        /// </summary>
        public enum ReductionStrategy
        {
            /// <summary>
            /// No reduction.
            /// </summary>
            None,

            /// <summary>
            /// Reduction strategy that omits scheduling points.
            /// </summary>
            OmitSchedulingPoints,

            /// <summary>
            /// Reduction strategy that forces scheduling points.
            /// </summary>
            ForceSchedule
        }

        /// <summary>
        /// The child strategy.
        /// </summary>
        private readonly ISchedulingStrategy ChildStrategy;

        /// <summary>
        /// The reduction strategy.
        /// </summary>
        private readonly ReductionStrategy Reduction;

        private int ScheduledSteps;
        private readonly int StepLimit;
        private readonly bool ReportActualScheduledSteps;

        /// <summary>
        /// Initializes a new instance of the <see cref="BasicReductionStrategy"/> class.
        /// </summary>
        public BasicReductionStrategy(
            ISchedulingStrategy childStrategy,
            ReductionStrategy reductionStrategy,
            int stepLimit = 0)
        {
            this.ChildStrategy = childStrategy;
            this.Reduction = reductionStrategy;
            this.ScheduledSteps = 0;
            this.StepLimit = stepLimit;
            this.ReportActualScheduledSteps = this.StepLimit != 0;
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        public bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            next = null;
            return this.GetNextHelper(ref next, choices, current);
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            ++this.ScheduledSteps;
            return this.ChildStrategy.GetNextBooleanChoice(maxValue, out next);
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            ++this.ScheduledSteps;
            return this.ChildStrategy.GetNextIntegerChoice(maxValue, out next);
        }

        /// <summary>
        /// Forces the next entity to be scheduled.
        /// </summary>
        public void ForceNext(ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            this.GetNextHelper(ref next, choices, current);
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            ++this.ScheduledSteps;
            this.ChildStrategy.ForceNextBooleanChoice(maxValue, next);
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            ++this.ScheduledSteps;
            this.ChildStrategy.ForceNextIntegerChoice(maxValue, next);
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        public bool PrepareForNextIteration()
        {
            this.ScheduledSteps = 0;
            return this.ChildStrategy.PrepareForNextIteration();
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            this.ScheduledSteps = 0;
            this.ChildStrategy.Reset();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        public int GetScheduledSteps() => this.ReportActualScheduledSteps ? this.ScheduledSteps : this.ChildStrategy.GetScheduledSteps();

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        public bool HasReachedMaxSchedulingSteps() => this.ReportActualScheduledSteps ?
            (this.StepLimit > 0 && this.ScheduledSteps >= this.StepLimit) : this.ChildStrategy.HasReachedMaxSchedulingSteps();

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        public bool IsFair() => this.ChildStrategy.IsFair();

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public string GetDescription() => $"{this.ChildStrategy.GetDescription()}  w/ {this.Reduction}";

        /// <summary>
        /// Returns or forces the next choice to schedule.
        /// </summary>
        private bool GetNextHelper(ref ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            ++this.ScheduledSteps;
            switch (this.Reduction)
            {
                case ReductionStrategy.ForceSchedule:
                    {
                        var partialOrderChoices =
                            choices
                                .Where(choice => choice.IsEnabled && IsPartialOrderOperation(choice.NextOperationType))
                                .ToList();

                        // If we are being forced:
                        if (next != null)
                        {
                            if (!partialOrderChoices.Contains(next))
                            {
                                // Tell child strategy that we were forced (to do a particular send).
                                this.ChildStrategy.ForceNext(next, choices, current);
                                return true;
                            }

                            // We would have forced this choice anyway so don't tell ChildStrategy.
                            return true;
                        }

                        // Not being forced:
                        if (partialOrderChoices.Count > 0)
                        {
                            // Force this non-send but don't tell ChildStrategy.
                            next = partialOrderChoices[0];
                            return true;
                        }

                        // Normal schedule:
                        return this.ChildStrategy.GetNext(out next, choices, current);
                    }

                case ReductionStrategy.OmitSchedulingPoints:
                    {
                        // Otherwise, don't schedule before non-Send.
                        bool continueWithCurrent =
                            current.IsEnabled &&
                            IsPartialOrderOperation(current.NextOperationType);

                        // We are being forced:
                        if (next != null)
                        {
                            // ...to do something different than we would have:
                            if (continueWithCurrent && current != next)
                            {
                                // ...so tell child.
                                this.ChildStrategy.ForceNext(next, choices, current);
                                return true;
                            }

                            // Otherwise, don't tell child.
                            return true;
                        }

                        // Not being forced:
                        if (continueWithCurrent)
                        {
                            next = current;
                            return true;
                        }

                        // Normal schedule:
                        return this.ChildStrategy.GetNext(out next, choices, current);
                    }

                case ReductionStrategy.None:
                    {
                        // Normal schedule:
                        if (next != null)
                        {
                            this.ChildStrategy.ForceNext(next, choices, current);
                            return true;
                        }

                        return this.ChildStrategy.GetNext(out next, choices, current);
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static bool IsPartialOrderOperation(OperationType operationType)
        {
            return operationType != OperationType.Send;
        }
    }
}
