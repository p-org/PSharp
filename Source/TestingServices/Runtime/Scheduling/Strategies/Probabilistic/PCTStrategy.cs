﻿// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.PSharp.IO;

namespace Microsoft.PSharp.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// A priority-based probabilistic scheduling strategy.
    ///
    /// This strategy is described in the following paper:
    /// https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/asplos277-pct.pdf
    /// </summary>
    public sealed class PCTStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Random number generator.
        /// </summary>
        private readonly IRandomNumberGenerator RandomNumberGenerator;

        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        private readonly int MaxScheduledSteps;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        private int ScheduledSteps;

        /// <summary>
        /// Max number of priority switch points.
        /// </summary>
        private readonly int MaxPrioritySwitchPoints;

        /// <summary>
        /// Approximate length of the schedule across all iterations.
        /// </summary>
        private int ScheduleLength;

        /// <summary>
        /// List of prioritized operations.
        /// </summary>
        private readonly List<IAsyncOperation> PrioritizedOperations;

        /// <summary>
        /// Set of priority change points.
        /// </summary>
        private readonly SortedSet<int> PriorityChangePoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="PCTStrategy"/> class. It uses
        /// the default random number generator (seed is based on current time).
        /// </summary>
        public PCTStrategy(int maxSteps, int maxPrioritySwitchPoints)
            : this(maxSteps, maxPrioritySwitchPoints, new DefaultRandomNumberGenerator(DateTime.Now.Millisecond))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PCTStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public PCTStrategy(int maxSteps, int maxPrioritySwitchPoints, IRandomNumberGenerator random)
        {
            this.RandomNumberGenerator = random;
            this.MaxScheduledSteps = maxSteps;
            this.ScheduledSteps = 0;
            this.ScheduleLength = 0;
            this.MaxPrioritySwitchPoints = maxPrioritySwitchPoints;
            this.PrioritizedOperations = new List<IAsyncOperation>();
            this.PriorityChangePoints = new SortedSet<int>();
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            next = null;
            return this.GetNextHelper(ref next, ops, current);
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
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
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            next = this.RandomNumberGenerator.Next(maxValue);
            this.ScheduledSteps++;
            return true;
        }

        /// <summary>
        /// Forces the next asynchronous operation to be scheduled.
        /// </summary>
        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            this.GetNextHelper(ref next, ops, current);
        }

        /// <summary>
        /// Returns or forces the next asynchronous operation to schedule.
        /// </summary>
        private bool GetNextHelper(ref IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            var enabledOperations = ops.Where(op => op.IsEnabled).ToList();
            if (enabledOperations.Count == 0)
            {
                return false;
            }

            IAsyncOperation highestEnabledOp = this.GetPrioritizedOperation(enabledOperations, current);
            if (next is null)
            {
                next = highestEnabledOp;
            }

            this.ScheduledSteps++;

            return true;
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
        public bool PrepareForNextIteration()
        {
            this.ScheduleLength = Math.Max(this.ScheduleLength, this.ScheduledSteps);
            this.ScheduledSteps = 0;

            this.PrioritizedOperations.Clear();
            this.PriorityChangePoints.Clear();

            var range = new List<int>();
            for (int idx = 0; idx < this.ScheduleLength; idx++)
            {
                range.Add(idx);
            }

            foreach (int point in this.Shuffle(range).Take(this.MaxPrioritySwitchPoints))
            {
                this.PriorityChangePoints.Add(point);
            }

            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            this.ScheduleLength = 0;
            this.ScheduledSteps = 0;
            this.PrioritizedOperations.Clear();
            this.PriorityChangePoints.Clear();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        public int GetScheduledSteps() => this.ScheduledSteps;

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
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
        public bool IsFair() => false;

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public string GetDescription()
        {
            var text = $"PCT[priority change points '{this.MaxPrioritySwitchPoints}' [";

            int idx = 0;
            foreach (var points in this.PriorityChangePoints)
            {
                text += points;
                if (idx < this.PriorityChangePoints.Count - 1)
                {
                    text += ", ";
                }

                idx++;
            }

            text += "], seed '" + this.RandomNumberGenerator.Seed + "']";
            return text;
        }

        /// <summary>
        /// Returns the prioritized operation.
        /// </summary>
        private IAsyncOperation GetPrioritizedOperation(List<IAsyncOperation> ops, IAsyncOperation current)
        {
            if (this.PrioritizedOperations.Count == 0)
            {
                this.PrioritizedOperations.Add(current);
            }

            foreach (var op in ops.Where(op => !this.PrioritizedOperations.Contains(op)))
            {
                var mIndex = this.RandomNumberGenerator.Next(this.PrioritizedOperations.Count) + 1;
                this.PrioritizedOperations.Insert(mIndex, op);
                Debug.WriteLine($"<PCTLog> Detected new operation from '{op.SourceName}' at index '{mIndex}'.");
            }

            if (this.PriorityChangePoints.Contains(this.ScheduledSteps))
            {
                if (ops.Count == 1)
                {
                    this.MovePriorityChangePointForward();
                }
                else
                {
                    var priority = this.GetHighestPriorityEnabledOperation(ops);
                    this.PrioritizedOperations.Remove(priority);
                    this.PrioritizedOperations.Add(priority);
                    Debug.WriteLine($"<PCTLog> Operation '{priority}' changes to lowest priority.");
                }
            }

            var prioritizedSchedulable = this.GetHighestPriorityEnabledOperation(ops);
            Debug.WriteLine($"<PCTLog> Prioritized schedulable '{prioritizedSchedulable}'.");
            Debug.Write("<PCTLog> Priority list: ");
            for (int idx = 0; idx < this.PrioritizedOperations.Count; idx++)
            {
                if (idx < this.PrioritizedOperations.Count - 1)
                {
                    Debug.Write($"'{this.PrioritizedOperations[idx]}', ");
                }
                else
                {
                    Debug.WriteLine($"'{this.PrioritizedOperations[idx]}({1})'.");
                }
            }

            return ops.First(op => op.Equals(prioritizedSchedulable));
        }

        /// <summary>
        /// Returns the highest-priority enabled operation.
        /// </summary>
        private IAsyncOperation GetHighestPriorityEnabledOperation(IEnumerable<IAsyncOperation> choices)
        {
            IAsyncOperation prioritizedOp = null;
            foreach (var entity in this.PrioritizedOperations)
            {
                if (choices.Any(m => m == entity))
                {
                    prioritizedOp = entity;
                    break;
                }
            }

            return prioritizedOp;
        }

        /// <summary>
        /// Shuffles the specified list using the Fisher-Yates algorithm.
        /// </summary>
        private IList<int> Shuffle(IList<int> list)
        {
            var result = new List<int>(list);
            for (int idx = result.Count - 1; idx >= 1; idx--)
            {
                int point = this.RandomNumberGenerator.Next(this.ScheduleLength);
                int temp = result[idx];
                result[idx] = result[point];
                result[point] = temp;
            }

            return result;
        }

        /// <summary>
        /// Moves the current priority change point forward. This is a useful
        /// optimization when a priority change point is assigned in either a
        /// sequential execution or a nondeterministic choice.
        /// </summary>
        private void MovePriorityChangePointForward()
        {
            this.PriorityChangePoints.Remove(this.ScheduledSteps);
            var newPriorityChangePoint = this.ScheduledSteps + 1;
            while (this.PriorityChangePoints.Contains(newPriorityChangePoint))
            {
                newPriorityChangePoint++;
            }

            this.PriorityChangePoints.Add(newPriorityChangePoint);
            Debug.WriteLine($"<PCTLog> Moving priority change to '{newPriorityChangePoint}'.");
        }
    }
}
