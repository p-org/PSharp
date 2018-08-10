//-----------------------------------------------------------------------
// <copyright file="BasicReductionStrategy.cs">
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
        /// Creates a reduction strategy that reduces the choice-space for a child strategy.
        /// </summary>
        /// <param name="childStrategy">Child strategy.</param>
        /// <param name="reductionStrategy">The reduction strategy used.</param>
        /// <param name="stepLimit">The step limit.</param>
        public BasicReductionStrategy(
            ISchedulingStrategy childStrategy,
            ReductionStrategy reductionStrategy,
            int stepLimit = 0)
        {
            ChildStrategy = childStrategy;
            Reduction = reductionStrategy;
            ScheduledSteps = 0;
            StepLimit = stepLimit;
            ReportActualScheduledSteps = StepLimit != 0;
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
            next = null;
            return GetNextHelper(ref next, choices, current);
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            ++ScheduledSteps;
            return ChildStrategy.GetNextBooleanChoice(maxValue, out next);
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            ++ScheduledSteps;
            return ChildStrategy.GetNextIntegerChoice(maxValue, out next);
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
            GetNextHelper(ref next, choices, current);
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            ++ScheduledSteps;
            ChildStrategy.ForceNextBooleanChoice(maxValue, next);
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            ++ScheduledSteps;
            ChildStrategy.ForceNextIntegerChoice(maxValue, next);
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration</returns>
        public bool PrepareForNextIteration()
        {
            ScheduledSteps = 0;
            return ChildStrategy.PrepareForNextIteration();
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            ScheduledSteps = 0;
            ChildStrategy.Reset();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        /// <returns>Scheduled steps</returns>
        public int GetScheduledSteps()
        {
            return ReportActualScheduledSteps
                ? ScheduledSteps
                : ChildStrategy.GetScheduledSteps();
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            return ReportActualScheduledSteps
                ? (StepLimit > 0 && ScheduledSteps >= StepLimit)
                : ChildStrategy.HasReachedMaxSchedulingSteps();
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            return ChildStrategy.IsFair();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            return $"{ChildStrategy.GetDescription()}  w/ {Reduction}";
        }

        /// <summary>
        /// Returns or forces the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        private bool GetNextHelper(ref ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            ++ScheduledSteps;
            switch (Reduction)
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
                                ChildStrategy.ForceNext(next, choices, current);
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
                        return ChildStrategy.GetNext(out next, choices, current);
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
                                ChildStrategy.ForceNext(next, choices, current);
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
                        return ChildStrategy.GetNext(out next, choices, current);
                    }

                case ReductionStrategy.None:
                    {
                        // Normal schedule:
                        if (next != null)
                        {
                            ChildStrategy.ForceNext(next, choices, current);
                            return true;
                        }
                        return ChildStrategy.GetNext(out next, choices, current);
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
