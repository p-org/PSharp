﻿// ------------------------------------------------------------------------------------------------
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
    public sealed class PCTCPStrategy : ISchedulingStrategy
    {

        private ChainPartitioner chainPartitioner;
        /// <summary>
        /// Logger used by the strategy.
        /// </summary>
        private ILogger Logger;

        /// <summary>
        /// Random number generator.
        /// </summary>
        private IRandomNumberGenerator RandomNumberGenerator;

        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        private int MaxScheduledSteps;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        private int ScheduledSteps;

        /// <summary>
        /// Max number of priority switch points.
        /// </summary>
        private int MaxPrioritySwitchPoints;

        /// <summary>
        /// Approximate length of the schedule across all iterations.
        /// </summary>
        private int ScheduleLength;

        /// <summary>
        /// List of prioritized schedulable choices.
        /// </summary>
        private List<ISchedulable> PrioritizedSchedulableChoices;

        /// <summary>
        /// List of priority change points, because we have a target priority.
        /// </summary>
         private List<int> PriorityChangePoints;

 
         /// <summary>
         /// Number of changepoints encountered so far
         /// </summary>
         private List<int> PriorityChangePointsEncountered;

        private ulong lastActionSchedulingStep;

        /// <summary>
        /// Creates a PCT strategy that uses the default random
        /// number generator (seed is based on current time).
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="maxPrioritySwitchPoints">Max number of priority switch points</param>
        /// <param name="logger">ILogger</param>
        public PCTCPStrategy(int maxSteps, int maxPrioritySwitchPoints, ILogger logger)
            : this(maxSteps, maxPrioritySwitchPoints, logger, new DefaultRandomNumberGenerator(DateTime.Now.Millisecond))
        { }

        /// <summary>
        /// Creates a PCT strategy that uses the specified random number generator.
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="maxPrioritySwitchPoints">Max number of priority switch points</param>
        /// <param name="logger">ILogger</param>
        /// <param name="random">IRandomNumberGenerator</param>
        public PCTCPStrategy(int maxSteps, int maxPrioritySwitchPoints, ILogger logger, IRandomNumberGenerator random)
        {
            Logger = logger;
            RandomNumberGenerator = random;
            MaxScheduledSteps = maxSteps;
            ScheduledSteps = 0;
            ScheduleLength = 0;
            lastActionSchedulingStep = 0;
            MaxPrioritySwitchPoints = maxPrioritySwitchPoints;
            PrioritizedSchedulableChoices = new List<ISchedulable>();
            PriorityChangePoints = new List<int>();
            PriorityChangePointsEncountered = new List<int>();
            chainPartitioner = new ChainPartitioner(logger, random);
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
            next = false;
            if (RandomNumberGenerator.Next(maxValue) == 0)
            {
                next = true;
            }

            ScheduledSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            next = RandomNumberGenerator.Next(maxValue);
            ScheduledSteps++;
            return true;
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
        /// Returns or forces the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        private bool GetNextHelper(ref ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            var enabledChoicesDict = choices.Where(choice => choice.IsEnabled).ToDictionary(x => x.Id);
            chainPartitioner.recordChoiceEffect(current, enabledChoicesDict, lastActionSchedulingStep);
            if (enabledChoicesDict.Count == 0)
            {
                return false;
            }

            // See if PCTCP can decide on an event to schedule.
            ChainEvent highestEnabledChainEvent = null ;
            ISchedulable highestEnabled = null;
            
            if (next == null)
            {
                if (!chainPartitioner.GetNext(out highestEnabled, out highestEnabledChainEvent, enabledChoicesDict, current))
                {
                    // Our chain partitioner could not decide what to schedule.
                    return false;
                }
            }

            next = highestEnabled;
            ScheduledSteps++;
            lastActionSchedulingStep = (ulong)GetScheduledSteps();

            //chainPartitioner.PrintChains(Logger);

            return true;
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            ScheduledSteps++;
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            ScheduledSteps++;
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration</returns>
        public bool PrepareForNextIteration()
        {
            ScheduleLength = Math.Max(ScheduleLength, ScheduledSteps);
            ScheduledSteps = 0;

            PrioritizedSchedulableChoices.Clear();
            PriorityChangePoints.Clear();
            PriorityChangePointsEncountered.Clear();

            var range = new List<int>();
            for (int idx = 0; idx < ScheduleLength; idx++)
            {
                range.Add(idx);
            }
            
            // TODO: [t-krgov] I'm sure we can make this more efficient
            foreach (int point in Shuffle(range).Take(MaxPrioritySwitchPoints))
            {
                PriorityChangePoints.Add(point);
            }


            chainPartitioner.Reset();
            lastActionSchedulingStep = 0;
            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            ScheduleLength = 0;
            ScheduledSteps = 0;
            lastActionSchedulingStep = 0;
            PrioritizedSchedulableChoices.Clear();
            PriorityChangePoints.Clear();
            chainPartitioner.Reset();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        /// <returns>Scheduled steps</returns>
        public int GetScheduledSteps()
        {
            return ScheduledSteps;
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool HasReachedMaxSchedulingSteps()
        {
            if (MaxScheduledSteps == 0)
            {
                return false;
            }

            return ScheduledSteps >= MaxScheduledSteps;
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        /// <returns>Boolean</returns>
        public bool IsFair()
        {
            return false;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        /// <returns>String</returns>
        public string GetDescription()
        {
            var text = $"PCT[priority change points '{MaxPrioritySwitchPoints}' [";

            int idx = 0;
            foreach (var points in PriorityChangePoints)
            {
                text += points;
                if (idx < PriorityChangePoints.Count - 1)
                {
                    text += ", ";
                }

                idx++;
            }

            text += "], seed '" + RandomNumberGenerator.Seed + "']";
            return text;
        }

        /// <summary>
        /// Returns the prioritized choice.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>ISchedulable</returns>
        private ISchedulable GetPrioritizedChoice(List<ISchedulable> choices, ISchedulable current)
        {
            if (PrioritizedSchedulableChoices.Count == 0)
            {
                PrioritizedSchedulableChoices.Add(current);
            }

            foreach (var choice in choices.Where(choice => !PrioritizedSchedulableChoices.Contains(choice)))
            {
                var mIndex = RandomNumberGenerator.Next(PrioritizedSchedulableChoices.Count - PriorityChangePointsEncountered.Count + 1);
                PrioritizedSchedulableChoices.Insert(mIndex, choice);
                Logger.WriteLine($"<PCTLog> Detected new schedulable choice '{choice.Name}' at index '{mIndex}'.");
            }

            if (PriorityChangePoints.Contains(ScheduledSteps))
            {
                if (choices.Count == 1)
                {
                    MovePriorityChangePointForward();
                }
                else
                {
                    // [t-krgov] Start : A priority change point has a target priority associated with it.
                    ISchedulable priority = GetHighestPriorityEnabledChoice(choices);
                    int targetPriority = PriorityChangePoints.IndexOf(ScheduledSteps);
                    int currentPriority = PrioritizedSchedulableChoices.IndexOf(priority);

                    // Update PriorityChangePointsEncountered
                    // If this ISchedulable has already had a priority change, we need to update the PriorityChangePointsEncountered accordingly.
                    if (currentPriority >= (PrioritizedSchedulableChoices.Count - PriorityChangePointsEncountered.Count))
                    {
                        int indexToDelete = currentPriority - (PrioritizedSchedulableChoices.Count - PriorityChangePointsEncountered.Count);
                        PriorityChangePointsEncountered.RemoveAt(indexToDelete);
                    }

                    // Insert at index
                    int insertAtIndex = 0;
                    foreach (int p in PriorityChangePointsEncountered)
                    {
                        if (p <= targetPriority)
                        {
                            insertAtIndex++;
                        }
                        else
                        {
                            break;
                        }
                    }

                    PrioritizedSchedulableChoices.Remove(priority);
                    PrioritizedSchedulableChoices.Insert(PrioritizedSchedulableChoices.Count - PriorityChangePointsEncountered.Count + insertAtIndex, priority);
                    PriorityChangePointsEncountered.Insert(insertAtIndex, targetPriority);
                    Logger.WriteLine($"<PCTLog> Schedulable '{priority}' assumes absolute priority='{targetPriority}'.");
                    // [t-krgov] Start : A priority change point has a target priority associated with it.
                }
            }

            ISchedulable prioritizedSchedulable = GetHighestPriorityEnabledChoice(choices);
            Logger.Write("<PCTLog> Priority list: ");
            for (int idx = 0; idx < PrioritizedSchedulableChoices.Count; idx++)
            {
                if (idx < PrioritizedSchedulableChoices.Count - 1)
                {
                    Logger.Write($"'{PrioritizedSchedulableChoices[idx]}', ");
                }
                else
                {
                    Logger.WriteLine($"'{PrioritizedSchedulableChoices[idx]}({1})'.");
                }
            }

            return choices.First(choice => choice.Equals(prioritizedSchedulable));
        }

        /// <summary>
        /// Returns the highest-priority enabled choice.
        /// </summary>
        /// <param name="choices">Choices</param>
        /// <returns>ISchedulable</returns>
        private ISchedulable GetHighestPriorityEnabledChoice(IEnumerable<ISchedulable> choices)
        {
            ISchedulable prioritizedChoice = null;
            foreach (var entity in PrioritizedSchedulableChoices)
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
        /// <param name="list">IList</param>
        /// <returns>IList</returns>
        private IList<int> Shuffle(IList<int> list)
        {
            var result = new List<int>(list);
            for (int idx = result.Count - 1; idx >= 1; idx--)
            {
                int point = RandomNumberGenerator.Next(ScheduleLength);
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
            int updateIndex = PriorityChangePoints.IndexOf(ScheduledSteps);
            var newPriorityChangePoint = ScheduledSteps + 1;
            while (PriorityChangePoints.Contains(newPriorityChangePoint))
            {
                newPriorityChangePoint++;
            }

            PriorityChangePoints[updateIndex] = newPriorityChangePoint;
            Logger.WriteLine($"<PCTLog> Moving priority change to '{newPriorityChangePoint}'.");
        }
    }
}