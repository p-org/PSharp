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
    /// An abstract delay-bounding scheduling strategy.
    /// </summary>
    public abstract class DelayBoundingStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Logger used by the strategy.
        /// </summary>
        protected ILogger Logger;

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
        /// Length of the explored schedule across all iterations.
        /// </summary>
        protected int ScheduleLength;

        /// <summary>
        /// The maximum number of delays.
        /// </summary>
        protected int MaxDelays;

        /// <summary>
        /// The remaining delays.
        /// </summary>
        protected List<int> RemainingDelays;

        /// <summary>
        /// Creates a delay-bounding strategy that uses the default random
        /// number generator (seed is based on current time).
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="maxDelays">Max number of delays</param>
        /// <param name="logger">ILogger</param>
        public DelayBoundingStrategy(int maxSteps, int maxDelays, ILogger logger)
            : this(maxSteps, maxDelays, logger, new DefaultRandomNumberGenerator(DateTime.Now.Millisecond))
        { }

        /// <summary>
        /// Creates a delay-bounding strategy that uses the specified random number generator.
        /// </summary>
        /// <param name="maxSteps">Max scheduling steps</param>
        /// <param name="maxDelays">Max number of delays</param>
        /// <param name="logger">ILogger</param>
        /// <param name="random">IRandomNumberGenerator</param>
        public DelayBoundingStrategy(int maxSteps, int maxDelays, ILogger logger, IRandomNumberGenerator random)
        {
            Logger = logger;
            RandomNumberGenerator = random;
            MaxScheduledSteps = maxSteps;
            ScheduledSteps = 0;
            MaxDelays = maxDelays;
            ScheduleLength = 0;
            RemainingDelays = new List<int>();
        }

        /// <summary>
        /// Returns the next choice to schedule.
        /// </summary>
        /// <param name="next">Next</param>
        /// <param name="choices">Choices</param>
        /// <param name="current">Curent</param>
        /// <returns>Boolean</returns>
        public virtual bool GetNext(out ISchedulable next, List<ISchedulable> choices, ISchedulable current)
        {
            var currentMachineIdx = choices.IndexOf(current);
            var orderedMachines = choices.GetRange(currentMachineIdx, choices.Count - currentMachineIdx);
            if (currentMachineIdx != 0)
            {
                orderedMachines.AddRange(choices.GetRange(0, currentMachineIdx));
            }

            var enabledChoices = orderedMachines.Where(choice => choice.IsEnabled).ToList();
            if (enabledChoices.Count == 0)
            {
                next = null;
                return false;
            }

            int idx = 0;
            while (RemainingDelays.Count > 0 && ScheduledSteps == RemainingDelays[0])
            {
                idx = (idx + 1) % enabledChoices.Count;
                RemainingDelays.RemoveAt(0);
                Logger.WriteLine("<DelayLog> Inserted delay, '{0}' remaining.", RemainingDelays.Count);
            }

            next = enabledChoices[idx];

            ScheduledSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public virtual bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            next = false;
            if (RemainingDelays.Count > 0 && ScheduledSteps == RemainingDelays[0])
            {
                next = true;
                RemainingDelays.RemoveAt(0);
                Logger.WriteLine("<DelayLog> Inserted delay, '{0}' remaining.", RemainingDelays.Count);
            }

            ScheduledSteps++;

            return true;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public virtual bool GetNextIntegerChoice(int maxValue, out int next)
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
            ScheduledSteps++;
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
        /// <param name="next">Next</param>
        /// <returns>Boolean</returns>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            ScheduledSteps++;
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        /// <param name="maxValue">Max value</param>
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
        public abstract bool PrepareForNextIteration();

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public virtual void Reset()
        {
            ScheduleLength = 0;
            ScheduledSteps = 0;
            RemainingDelays.Clear();
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
        public abstract string GetDescription();
    }
}
