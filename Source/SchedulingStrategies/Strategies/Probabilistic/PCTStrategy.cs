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
    /// A priority-based probabilistic scheduling strategy.
    ///
    /// This strategy is described in the following paper:
    /// https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/asplos277-pct.pdf
    /// </summary>
    public sealed class PCTStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Logger used by the strategy.
        /// </summary>
        private readonly ILogger Logger;

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
        /// List of prioritized schedulable choices.
        /// </summary>
        private readonly List<ISchedulable> PrioritizedSchedulableChoices;

        /// <summary>
        /// Set of priority change points.
        /// </summary>
        private readonly SortedSet<int> PriorityChangePoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="PCTStrategy"/> class. It uses
        /// the default random number generator (seed is based on current time).
        /// </summary>
        public PCTStrategy(int maxSteps, int maxPrioritySwitchPoints, ILogger logger)
            : this(maxSteps, maxPrioritySwitchPoints, logger, new DefaultRandomNumberGenerator(DateTime.Now.Millisecond))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PCTStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public PCTStrategy(int maxSteps, int maxPrioritySwitchPoints, ILogger logger, IRandomNumberGenerator random)
        {
            this.Logger = logger;
            this.RandomNumberGenerator = random;
            this.MaxScheduledSteps = maxSteps;
            this.ScheduledSteps = 0;
            this.ScheduleLength = 0;
            this.MaxPrioritySwitchPoints = maxPrioritySwitchPoints;
            this.PrioritizedSchedulableChoices = new List<ISchedulable>();
            this.PriorityChangePoints = new SortedSet<int>();
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
        /// Forces the next entity to be scheduled.
        /// </summary>
        public void ForceNext(ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            this.GetNextHelper(ref next, choices, current);
        }

        /// <summary>
        /// Returns or forces the next choice to schedule.
        /// </summary>
        private bool GetNextHelper(ref ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            var enabledChoices = choices.Where(choice => choice.IsEnabled).ToList();
            if (enabledChoices.Count == 0)
            {
                return false;
            }

            ISchedulable highestEnabled = this.GetPrioritizedChoice(enabledChoices, current);
            if (next is null)
            {
                next = highestEnabled;
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

            this.PrioritizedSchedulableChoices.Clear();
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
            this.PrioritizedSchedulableChoices.Clear();
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
        /// Returns the prioritized choice.
        /// </summary>
        private ISchedulable GetPrioritizedChoice(List<ISchedulable> choices, ISchedulable current)
        {
            if (this.PrioritizedSchedulableChoices.Count == 0)
            {
                this.PrioritizedSchedulableChoices.Add(current);
            }

            foreach (var choice in choices.Where(choice => !this.PrioritizedSchedulableChoices.Contains(choice)))
            {
                var mIndex = this.RandomNumberGenerator.Next(this.PrioritizedSchedulableChoices.Count) + 1;
                this.PrioritizedSchedulableChoices.Insert(mIndex, choice);
                this.Logger.WriteLine($"<PCTLog> Detected new schedulable choice '{choice.Name}' at index '{mIndex}'.");
            }

            if (this.PriorityChangePoints.Contains(this.ScheduledSteps))
            {
                if (choices.Count == 1)
                {
                    this.MovePriorityChangePointForward();
                }
                else
                {
                    var priority = this.GetHighestPriorityEnabledChoice(choices);
                    this.PrioritizedSchedulableChoices.Remove(priority);
                    this.PrioritizedSchedulableChoices.Add(priority);
                    this.Logger.WriteLine($"<PCTLog> Schedulable '{priority}' changes to lowest priority.");
                }
            }

            var prioritizedSchedulable = this.GetHighestPriorityEnabledChoice(choices);
            this.Logger.WriteLine($"<PCTLog> Prioritized schedulable '{prioritizedSchedulable}'.");
            this.Logger.Write("<PCTLog> Priority list: ");
            for (int idx = 0; idx < this.PrioritizedSchedulableChoices.Count; idx++)
            {
                if (idx < this.PrioritizedSchedulableChoices.Count - 1)
                {
                    this.Logger.Write($"'{this.PrioritizedSchedulableChoices[idx]}', ");
                }
                else
                {
                    this.Logger.WriteLine($"'{this.PrioritizedSchedulableChoices[idx]}({1})'.");
                }
            }

            return choices.First(choice => choice.Equals(prioritizedSchedulable));
        }

        /// <summary>
        /// Returns the highest-priority enabled choice.
        /// </summary>
        private ISchedulable GetHighestPriorityEnabledChoice(IEnumerable<ISchedulable> choices)
        {
            ISchedulable prioritizedChoice = null;
            foreach (var entity in this.PrioritizedSchedulableChoices)
            {
                if (choices.Any(m => m == entity))
                {
                    prioritizedChoice = entity;
                    break;
                }
            }

            return prioritizedChoice;
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
            this.Logger.WriteLine($"<PCTLog> Moving priority change to '{newPriorityChangePoint}'.");
        }
    }
}
