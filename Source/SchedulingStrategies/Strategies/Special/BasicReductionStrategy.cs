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

        /// <summary>
        /// Creates a reduction strategy that reduces the choice-space for a child strategy.
        /// </summary>
        /// <param name="childStrategy">Child strategy.</param>
        /// <param name="reductionStrategy">The reduction strategy used.</param>
        public BasicReductionStrategy(
            ISchedulingStrategy childStrategy,
            ReductionStrategy reductionStrategy)
        {
            ChildStrategy = childStrategy;
            Reduction = reductionStrategy;
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
            switch (Reduction)
            {
                case ReductionStrategy.ForceSchedule:
                {
                    var partialOrderChoices =
                        choices
                            .Where(choice => choice.IsEnabled && IsPartialOrderOperation(choice.NextOperationType))
                            .ToList();

                    if (partialOrderChoices.Count > 0)
                    {
                        next = partialOrderChoices[0];
                        return true;
                    }

                    // Normal schedule:
                    return ChildStrategy.GetNext(out next, choices, current);
                }
                case ReductionStrategy.OmitSchedulingPoints:
                {
                    // Otherwise, don't schedule before non-Send.
                    if (current.IsEnabled &&
                        IsPartialOrderOperation(current.NextOperationType))
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
                    return ChildStrategy.GetNext(out next, choices, current);
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            return ChildStrategy.GetNextBooleanChoice(maxValue, out next);
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            return ChildStrategy.GetNextIntegerChoice(maxValue, out next);
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration</returns>
        public bool PrepareForNextIteration()
        {
            return ChildStrategy.PrepareForNextIteration();
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            ChildStrategy.Reset();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        /// <returns>Scheduled steps</returns>
        public int GetScheduledSteps()
        {
            return ChildStrategy.GetScheduledSteps();
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            return ChildStrategy.HasReachedMaxSchedulingSteps();
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

        private static bool IsPartialOrderOperation(OperationType operationType)
        {
            return operationType != OperationType.Send;
        }
    }
}
